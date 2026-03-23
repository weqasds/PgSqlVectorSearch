namespace DomainService.ServiceContract.CrossCutting;

/// <summary>
/// AI服务配置选项
/// </summary>
public class AIOptions
{
    /// <summary>
    /// DeepSeek API配置
    /// </summary>
    public DeepSeekOptions DeepSeek { get; set; } = new();

    /// <summary>
    /// OpenAI API配置
    /// </summary>
    public OpenAIOptions OpenAI { get; set; } = new();

    /// <summary>
    /// 本地嵌入模型配置
    /// </summary>
    public LocalEmbeddingOptions LocalEmbedding { get; set; } = new();

    /// <summary>
    /// 默认使用的AI服务类型
    /// </summary>
    public string DefaultService { get; set; } = "DeepSeek"; // DeepSeek, OpenAI, Local
}

/// <summary>
/// DeepSeek API配置
/// </summary>
public class DeepSeekOptions
{
    /// <summary>
    /// API密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API基础URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.deepseek.com";

    /// <summary>
    /// 聊天模型名称
    /// </summary>
    public string Model { get; set; } = "deepseek-chat";

    /// <summary>
    /// 嵌入模型名称
    /// </summary>
    public string EmbeddingModel { get; set; } = "deepseek-embedding";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// OpenAI API配置
/// </summary>
public class OpenAIOptions
{
    /// <summary>
    /// API密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API基础URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// 聊天模型名称
    /// </summary>
    public string Model { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// 嵌入模型名称
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = false;
}

/// <summary>
/// 本地嵌入模型配置
/// </summary>
public class LocalEmbeddingOptions
{
    /// <summary>
    /// API密钥（本地模型通常需要占位符）
    /// </summary>
    public string ApiKey { get; set; } = "sk-1233";

    /// <summary>
    /// API基础URL
    /// </summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:30000/v1";

    /// <summary>
    /// 嵌入模型名称
    /// </summary>
    public string EmbeddingModel { get; set; } = "Qwen/Qwen3-Embedding-0.6B";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
}