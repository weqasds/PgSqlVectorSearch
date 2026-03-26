using System.ComponentModel.DataAnnotations;

namespace Infrastructure.InfrastructureShared.DocumentParser;

/// <summary>
/// MinerU 精准解析 API 配置。
/// </summary>
public sealed class MinerUDocumentParserOptions
{
    public const string SectionName = "DocumentParser:MinerU";

    /// <summary>
    /// 是否启用 MinerU 解析服务。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// MinerU API 基地址，例如 https://mineru.net/api/v4/ 。
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = "https://mineru.net/api/v4/";

    /// <summary>
    /// MinerU API Bearer Token。
    /// </summary>
    [Required]
    public string BearerToken { get; set; } = string.Empty;

    /// <summary>
    /// HTTP 超时时间（秒）。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 轮询任务状态的最大重试次数。
    /// </summary>
    public int MaxPollingRetries { get; set; } = 60;

    /// <summary>
    /// 轮询间隔（秒）。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// 是否启用表格提取。
    /// </summary>
    public bool ExtractTables { get; set; } = true;

    /// <summary>
    /// 是否启用公式提取。
    /// </summary>
    public bool ExtractFormulas { get; set; } = true;

    /// <summary>
    /// 是否启用图片提取。
    /// </summary>
    public bool ExtractImages { get; set; } = false;

    /// <summary>
    /// 默认模型版本："pipeline"、"vlm" 或 "MinerU-HTML"。
    /// </summary>
    public string DefaultModelVersion { get; set; } = "pipeline";

    /// <summary>
    /// 默认语言（例如 "zh"、"en"）。
    /// </summary>
    public string DefaultLanguage { get; set; } = "zh";

    /// <summary>
    /// 是否使用缓存。
    /// </summary>
    public bool NoCache { get; set; } = false;

    /// <summary>
    /// 缓存容忍度（秒）。
    /// </summary>
    public int CacheTolerance { get; set; } = 86400;
}