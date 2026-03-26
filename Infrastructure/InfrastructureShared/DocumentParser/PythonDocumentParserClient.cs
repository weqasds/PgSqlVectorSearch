using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Models.DocumentParsing;
using Microsoft.Extensions.Options;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// Python 文档解析服务 HTTP 客户端。
/// </summary>
public sealed class PythonDocumentParserClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly PythonDocumentParserOptions _options;

    public PythonDocumentParserClient(
        HttpClient httpClient,
        IOptions<PythonDocumentParserOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public bool IsEnabled =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.BaseUrl);

    public async Task<PythonParseEnvelope> ParseAsync(
        Stream stream,
        string fileName,
        DocumentParseOptions? parseOptions,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Python 文档解析服务未启用或缺少 BaseUrl 配置。");
        }

        if (stream.CanSeek && stream.Position != 0)
        {
            stream.Position = 0;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "parse");
        request.Content = BuildMultipartContent(stream, fileName, parseOptions);

        if (!string.IsNullOrWhiteSpace(_options.BearerToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.BearerToken);
        }

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        PythonParseEnvelope? envelope = null;

        if (!string.IsNullOrWhiteSpace(payload))
        {
            envelope = JsonSerializer.Deserialize<PythonParseEnvelope>(payload, JsonOptions);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Python 文档解析服务调用失败，HTTP {(int)response.StatusCode}: {BuildErrorMessage(envelope, payload)}");
        }

        if (envelope?.Success != true || envelope.Data is null)
        {
            throw new InvalidOperationException(
                $"Python 文档解析服务返回失败: {BuildErrorMessage(envelope, payload)}");
        }

        return envelope;
    }

    private static MultipartFormDataContent BuildMultipartContent(
        Stream stream,
        string fileName,
        DocumentParseOptions? parseOptions)
    {
        var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(new NonDisposingStream(stream));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        content.Add(fileContent, "file", fileName);
        content.Add(
            new StringContent(BuildStrategy(parseOptions), Encoding.UTF8),
            "strategy");
        content.Add(
            new StringContent(((parseOptions?.ExtractTables) ?? true).ToString().ToLowerInvariant(), Encoding.UTF8),
            "extract_tables");
        content.Add(
            new StringContent(((parseOptions?.ExtractImages) ?? false).ToString().ToLowerInvariant(), Encoding.UTF8),
            "extract_images");

        return content;
    }

    private static string BuildStrategy(DocumentParseOptions? parseOptions)
    {
        if (parseOptions?.ExtractTables == false)
        {
            return "text_only";
        }

        return "auto";
    }

    private static string BuildErrorMessage(PythonParseEnvelope? envelope, string payload)
    {
        if (!string.IsNullOrWhiteSpace(envelope?.Error?.Message))
        {
            return envelope.Error.Message;
        }

        return string.IsNullOrWhiteSpace(payload)
            ? "未返回错误详情。"
            : payload;
    }

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

public sealed class PythonParseEnvelope
{
    public bool Success { get; set; }

    public PythonParseResult? Data { get; set; }

    public PythonErrorDetail? Error { get; set; }

    public PythonResponseMetadata? Metadata { get; set; }
}

public sealed class PythonErrorDetail
{
    public string? Code { get; set; }

    public string? Message { get; set; }
}

public sealed class PythonResponseMetadata
{
    [JsonPropertyName("processing_time")]
    public double? ProcessingTime { get; set; }

    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    [JsonPropertyName("pages_count")]
    public int? PagesCount { get; set; }

    [JsonPropertyName("ocr_triggered")]
    public bool? OcrTriggered { get; set; }

    [JsonPropertyName("strategy_used")]
    public string? StrategyUsed { get; set; }
}

public sealed class PythonParseResult
{
    public List<PythonPageContent> Content { get; set; } = new();

    [JsonPropertyName("document_metadata")]
    public PythonDocumentMetadata DocumentMetadata { get; set; } = new();

    public PythonDocumentStructure Structure { get; set; } = new();

    [JsonPropertyName("ocr_triggered")]
    public bool OcrTriggered { get; set; }
}

public sealed class PythonPageContent
{
    public int Page { get; set; }

    public string Text { get; set; } = string.Empty;

    public List<PythonTable> Tables { get; set; } = new();
}

public sealed class PythonTable
{
    [JsonPropertyName("table_index")]
    public int TableIndex { get; set; }

    public List<List<string>> Rows { get; set; } = new();

    public PythonBoundingBox? Bbox { get; set; }
}

public sealed class PythonDocumentMetadata
{
    public string? Title { get; set; }

    public string? Author { get; set; }

    public string? Subject { get; set; }

    public List<string> Keywords { get; set; } = new();

    [JsonPropertyName("created_date")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("modified_date")]
    public DateTime? ModifiedDate { get; set; }

    [JsonPropertyName("page_count")]
    public int PageCount { get; set; }

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }

    [JsonPropertyName("file_type")]
    public string FileType { get; set; } = string.Empty;
}

public sealed class PythonDocumentStructure
{
    public List<PythonHeading> Headings { get; set; } = new();

    public List<PythonParagraph> Paragraphs { get; set; } = new();
}

public sealed class PythonHeading
{
    public int Level { get; set; }

    public string Text { get; set; } = string.Empty;

    public int Page { get; set; }

    public PythonBoundingBox? Bbox { get; set; }
}

public sealed class PythonParagraph
{
    public string Text { get; set; } = string.Empty;

    public int Page { get; set; }

    public PythonBoundingBox? Bbox { get; set; }
}

public sealed class PythonBoundingBox
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
}
