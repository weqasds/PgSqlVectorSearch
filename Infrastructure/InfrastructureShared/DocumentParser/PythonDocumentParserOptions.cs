namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// Python 文档解析服务配置。
/// </summary>
public sealed class PythonDocumentParserOptions
{
    public const string SectionName = "DocumentParser:Python";

    /// <summary>
    /// 是否启用 Python 解析服务。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Python 服务基地址，例如 http://127.0.0.1:8000/ 。
    /// </summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:8000/";

    /// <summary>
    /// Python 服务 Bearer Token。
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// HTTP 超时时间（秒）。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}
