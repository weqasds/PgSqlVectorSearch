using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Models.DocumentParsing;
using Microsoft.Extensions.Options;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// MinerU 精准解析 API HTTP 客户端。
/// </summary>
public sealed class MinerUDocumentParserClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly MinerUDocumentParserOptions _options;

    public MinerUDocumentParserClient(
        HttpClient httpClient,
        IOptions<MinerUDocumentParserOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <summary>
    /// 检查服务是否启用。
    /// </summary>
    public bool IsEnabled =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.BaseUrl) &&
        !string.IsNullOrWhiteSpace(_options.BearerToken);

    /// <summary>
    /// 解析文档文件流。
    /// </summary>
    /// <param name="stream">文件流。</param>
    /// <param name="fileName">文件名。</param>
    /// <param name="parseOptions">解析选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解析后的内容列表。</returns>
    public async Task<MinerUContentList> ParseAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? parseOptions,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("MinerU 文档解析服务未启用或缺少配置。");
        }

        // 1. 获取预签名上传 URL
        var batchResponse = await RequestUploadUrlAsync(fileName, cancellationToken).ConfigureAwait(false);

        // 2. 上传文件
        var fileUrl = batchResponse.FileUrls.FirstOrDefault()?.Url
            ?? throw new InvalidOperationException("未获取到文件上传 URL。");
        await UploadFileAsync(stream, fileUrl, cancellationToken).ConfigureAwait(false);

        // 3. 轮询任务结果
        var zipStream = await PollForResultAsync(batchResponse.BatchId, cancellationToken).ConfigureAwait(false);

        // 4. 从 ZIP 包中提取 content_list.json
        return await ExtractContentListFromZipAsync(zipStream, cancellationToken).ConfigureAwait(false);
    }

    private async Task<MinerUBatchFileResponse> RequestUploadUrlAsync(
        string fileName,
        CancellationToken cancellationToken)
    {
        var request = new MinerUBatchFileRequest
        {
            Files = new List<MinerUBatchFileRequestItem>
            {
                new MinerUBatchFileRequestItem
                {
                    Name = fileName,
                    DataId = Guid.NewGuid().ToString("N")
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            CombineUrl(_options.BaseUrl, "file-urls/batch"))
        {
            Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };

        AddAuthorizationHeader(httpRequest);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var batchResponse = JsonSerializer.Deserialize<MinerUBatchFileResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("无法解析批量文件上传响应。");

        return batchResponse;
    }

    private static async Task UploadFileAsync(
        Stream stream,
        string uploadUrl,
        CancellationToken cancellationToken)
    {
        if (stream.CanSeek && stream.Position != 0)
        {
            stream.Position = 0;
        }

        using var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = new StreamContent(new NonDisposingStream(stream))
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await new HttpClient().SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"文件上传失败，HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)}");
        }
    }

    private async Task<Stream> PollForResultAsync(
        string batchId,
        CancellationToken cancellationToken)
    {
        var pollUrl = CombineUrl(_options.BaseUrl, $"extract-results/batch/{batchId}");

        for (var attempt = 0; attempt < _options.MaxPollingRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, pollUrl);
            AddAuthorizationHeader(request);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<MinerUExtractResultBatchResponse>(content, JsonOptions)
                ?? throw new InvalidOperationException("无法解析批量提取结果响应。");

            var item = result.ExtractResult.FirstOrDefault()
                ?? throw new InvalidOperationException("提取结果为空。");

            switch (item.State)
            {
                case "done":
                    if (string.IsNullOrEmpty(item.FullZipUrl))
                    {
                        throw new InvalidOperationException("任务已完成但缺少 ZIP 包下载 URL。");
                    }
                    return await DownloadZipAsync(item.FullZipUrl, cancellationToken).ConfigureAwait(false);

                case "failed":
                    throw new InvalidOperationException($"解析任务失败: {item.Error ?? "未知错误"}");

                case "running":
                    // 继续轮询
                    break;

                default:
                    throw new InvalidOperationException($"未知任务状态: {item.State}");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), cancellationToken)
                .ConfigureAwait(false);
        }

        throw new TimeoutException($"轮询任务结果超时（{_options.MaxPollingRetries} 次尝试）。");
    }

    private async Task<Stream> DownloadZipAsync(
        string zipUrl,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, zipUrl);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"下载 ZIP 包失败，HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)}");
        }

        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        memoryStream.Position = 0;
        return memoryStream;
    }

    private static async Task<MinerUContentList> ExtractContentListFromZipAsync(
        Stream zipStream,
        CancellationToken cancellationToken)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        var contentListEntry = archive.Entries.FirstOrDefault(e =>
            e.Name.EndsWith("content_list.json", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("ZIP 包中未找到 content_list.json 文件。");

        await using var entryStream = contentListEntry.Open();
        using var reader = new StreamReader(entryStream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        var contentList = JsonSerializer.Deserialize<MinerUContentList>(json, JsonOptions)
            ?? throw new InvalidOperationException("无法解析 content_list.json。");

        return contentList;
    }

    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.BearerToken);
    }

    private static async Task EnsureSuccessStatusCodeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException(
            $"MinerU API 调用失败，HTTP {(int)response.StatusCode}: {errorBody}");
    }

    private static string CombineUrl(string baseUrl, string relative)
    {
        if (baseUrl.EndsWith("/"))
        {
            baseUrl = baseUrl[..^1];
        }

        if (relative.StartsWith("/"))
        {
            relative = relative[1..];
        }

        return $"{baseUrl}/{relative}";
    }

    /// <summary>
    /// 包装流以防止 HttpClient 在请求完成后将其释放。
    /// </summary>
    private sealed class NonDisposingStream : Stream
    {
        private readonly Stream _inner;

        public NonDisposingStream(Stream inner)
        {
            _inner = inner;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            _inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(buffer, cancellationToken);

        public override Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            _inner.WriteAsync(buffer, offset, count, cancellationToken);

        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            _inner.WriteAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            // 输入流生命周期由调用方负责。
        }
    }
}