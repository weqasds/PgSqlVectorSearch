using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DomainService.ServiceContract.CrossCutting;

/// <summary>
/// LLM选项
/// </summary>
public class LLMOptions
{
    public string? SystemPrompt { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public double TopP { get; set; } = 1.0;
    public double FrequencyPenalty { get; set; } = 0.0;
    public double PresencePenalty { get; set; } = 0.0;
    public string? ModelName { get; set; }
}
/// <summary>
/// 大型语言模型服务接口
/// 负责调用LLM生成文本响应
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// 生成LLM响应（非流式）
    /// </summary>
    Task<string> GenerateResponseAsync(string prompt, LLMOptions? options = null);

    /// <summary>
    /// 生成LLM响应（流式）
    /// </summary>
    IAsyncEnumerable<string> GenerateResponseStreamAsync(string prompt, LLMOptions? options = null);

    /// <summary>
    /// 计算文本的token数量
    /// </summary>
    Task<int> CountTokensAsync(string text);

    /// <summary>
    /// 获取支持的模型列表
    /// </summary>
    Task<IEnumerable<string>> GetSupportedModelsAsync();
}
