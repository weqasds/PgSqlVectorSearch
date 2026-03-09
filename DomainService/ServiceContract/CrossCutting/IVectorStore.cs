using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Enums;
using DomainService.Entities.VectorSearch;
using Pgvector;

namespace DomainService.ServiceContract.CrossCutting;

/// <summary>
/// 向量存储服务接口
/// 负责向量数据的存储和相似性搜索，属于基础设施层服务
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// 根据知识块ID获取嵌入向量
    /// </summary>
    Task<Embedding?> GetByChunkIdAsync(Guid chunkId);

    /// <summary>
    /// 搜索相似的嵌入向量
    /// </summary>
    /// <param name="queryVector">查询向量</param>
    /// <param name="limit">返回结果数量</param>
    /// <param name="metric">距离度量标准，默认为L2距离</param>
    Task<IEnumerable<Embedding>> SearchSimilarAsync(Vector queryVector, int limit, DistanceMetric metric = DistanceMetric.L2);

    /// <summary>
    /// 添加嵌入向量
    /// </summary>
    Task AddAsync(Embedding embedding);

    /// <summary>
    /// 批量添加嵌入向量
    /// </summary>
    Task AddRangeAsync(IEnumerable<Embedding> embeddings);

    /// <summary>
    /// 根据知识块ID删除嵌入向量
    /// </summary>
    Task DeleteByChunkIdAsync(Guid chunkId);

    /// <summary>
    /// 带过滤条件的向量相似度搜索
    /// </summary>
    /// <param name="queryVector">查询向量</param>
    /// <param name="limit">返回结果数量</param>
    /// <param name="metric">距离度量标准，默认为L2距离</param>
    /// <param name="knowledgeBaseId">知识库ID过滤</param>
    /// <param name="documentId">文档ID过滤</param>
    Task<IEnumerable<Embedding>> SearchSimilarWithFilterAsync(
        Vector queryVector,
        int limit,
        DistanceMetric metric = DistanceMetric.L2,
        Guid? knowledgeBaseId = null,
        Guid? documentId = null);
}