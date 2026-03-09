using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pgvector;


namespace Infrastructure.SemanticKernelAI.ServiceContract;

/// <summary>
/// 语义内核服务接口
/// 负责封装Microsoft Semantic Kernel核心功能
/// </summary>
public interface ISemanticKernelService
{
    /// <summary>
    /// 初始化语义内核实例
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 生成文本嵌入向量
    /// </summary>
    Task<Vector> GenerateEmbeddingAsync(string text);

    /// <summary>
    /// 批量生成文本嵌入向量
    /// </summary>
    Task<IEnumerable<Vector>> GenerateEmbeddingsAsync(IEnumerable<string> texts);

    /// <summary>
    /// 调用LLM生成响应
    /// </summary>
    Task<string> GenerateResponseAsync(string prompt, LLMOptions? options = null);

    /// <summary>
    /// 流式调用LLM生成响应
    /// </summary>
    IAsyncEnumerable<string> GenerateResponseStreamAsync(string prompt, LLMOptions? options = null);

    /// <summary>
    /// 获取支持的嵌入模型名称
    /// </summary>
    string GetEmbeddingModelName();

    /// <summary>
    /// 获取嵌入向量维度
    /// </summary>
    int GetEmbeddingDimension();

    /// <summary>
    /// 获取支持的LLM模型列表
    /// </summary>
    Task<IEnumerable<string>> GetSupportedModelsAsync();

    /// <summary>
    /// 计算文本的token数量
    /// </summary>
    Task<int> CountTokensAsync(string text);
}

/// <summary>
/// LLM选项配置
/// </summary>
public class LLMOptions
{
    /// <summary>
    /// 温度参数（0.0-2.0）
    /// </summary>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>
    /// 最大token数
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// 模型名称（可选，使用默认模型如果未指定）
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// 系统提示
    /// </summary>
    public string? SystemPrompt { get; set; }
}