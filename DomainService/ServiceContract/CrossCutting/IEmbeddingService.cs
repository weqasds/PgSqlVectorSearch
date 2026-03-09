using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pgvector;

namespace DomainService.ServiceContract.CrossCutting;

/// <summary>
/// 嵌入生成服务接口
/// 负责调用AI服务生成文本向量表示
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// 生成单个文本的嵌入向量
    /// </summary>
    Task<Vector> GenerateEmbeddingAsync(string text);

    /// <summary>
    /// 批量生成文本的嵌入向量
    /// </summary>
    Task<IEnumerable<Vector>> GenerateEmbeddingsAsync(IEnumerable<string> texts);

    /// <summary>
    /// 获取嵌入模型名称
    /// </summary>
    string GetEmbeddingModelName();

    /// <summary>
    /// 获取嵌入向量维度
    /// </summary>
    int GetEmbeddingDimension();
}