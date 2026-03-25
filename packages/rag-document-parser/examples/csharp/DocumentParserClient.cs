using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RagDocumentParser.Examples;

public sealed class DocumentParserClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;

    public DocumentParserClient(HttpClient httpClient, string? bearerToken = null)
    {
        _httpClient = httpClient;
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    public async Task<ApiResponse<ParseResultDto>?> ParseAsync(
        string filePath,
        string strategy = "auto",
        bool extractTables = true,
        bool extractImages = false,
        CancellationToken cancellationToken = default)
    {
        using var content = BuildMultipart(filePath, strategy, extractTables, extractImages);
        using var response = await _httpClient.PostAsync("/parse", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await DeserializeAsync<ParseResultDto>(response, cancellationToken);
    }

    public async Task<ApiResponse<AsyncTaskDto>?> SubmitAsync(
        string filePath,
        string strategy = "auto",
        CancellationToken cancellationToken = default)
    {
        using var content = BuildMultipart(filePath, strategy, extractTables: true, extractImages: false);
        using var response = await _httpClient.PostAsync("/parse/async", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await DeserializeAsync<AsyncTaskDto>(response, cancellationToken);
    }

    public async Task<ApiResponse<TaskStatusDto>?> GetTaskStatusAsync(string taskId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"/tasks/{taskId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await DeserializeAsync<TaskStatusDto>(response, cancellationToken);
    }

    public async Task<ApiResponse<ParseResultDto>?> GetTaskResultAsync(string taskId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"/tasks/{taskId}/result", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await DeserializeAsync<ParseResultDto>(response, cancellationToken);
    }

    private static MultipartFormDataContent BuildMultipart(
        string filePath,
        string strategy,
        bool extractTables,
        bool extractImages)
    {
        var content = new MultipartFormDataContent();
        var fileStream = File.OpenRead(filePath);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        content.Add(fileContent, "file", Path.GetFileName(filePath));
        content.Add(new StringContent(strategy, Encoding.UTF8), "strategy");
        content.Add(new StringContent(extractTables.ToString().ToLowerInvariant(), Encoding.UTF8), "extract_tables");
        content.Add(new StringContent(extractImages.ToString().ToLowerInvariant(), Encoding.UTF8), "extract_images");
        return content;
    }

    private static async Task<ApiResponse<T>?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<ApiResponse<T>>(stream, JsonOptions, cancellationToken);
    }
}

public sealed record ApiResponse<T>(bool Success, T? Data, ErrorDto? Error, MetadataDto Metadata);
public sealed record ErrorDto(string Code, string Message, Dictionary<string, object?>? Details);
public sealed record MetadataDto(double ProcessingTime, string? FileType, int? PagesCount, bool? OcrTriggered, string? TaskId);
public sealed record AsyncTaskDto(string TaskId, string Status, string Message, string StatusUrl, string ResultUrl);
public sealed record TaskStatusDto(string TaskId, string Status, double Progress, string? FileName, string? FileType, double? ProcessingTime);
public sealed record ParseResultDto(List<PageDto> Content, DocumentMetadataDto DocumentMetadata, bool OcrTriggered);
public sealed record PageDto(int Page, string Text);
public sealed record DocumentMetadataDto(string? Title, int PageCount, string FileType);
