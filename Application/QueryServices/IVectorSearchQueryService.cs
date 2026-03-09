using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using Pgvector;

namespace Application.QueryServices;

/// <summary>
/// 向量搜索查询服务接口
/// 负责向量检索相关的复杂查询操作
/// </summary>
public interface IVectorSearchQueryService : IQueryService<Embedding>
{
    /// <summary>
    /// 根据知识块ID获取嵌入向量
    /// </summary>
    Task<Embedding?> GetByChunkIdAsync(Guid chunkId);

    /// <summary>
    /// 向量相似度搜索
    /// </summary>
    /// <param name="queryVector">查询向量</param>
    /// <param name="limit">返回结果数量</param>
    /// <returns>相似度最高的嵌入向量列表</returns>
    Task<IEnumerable<Embedding>> SearchSimilarAsync(Vector queryVector, int limit);

    /// <summary>
    /// 带过滤条件的向量相似度搜索
    /// </summary>
    /// <param name="queryVector">查询向量</param>
    /// <param name="limit">返回结果数量</param>
    /// <param name="knowledgeBaseId">知识库ID过滤</param>
    /// <param name="documentId">文档ID过滤</param>
    /// <returns>过滤后的相似度最高的嵌入向量列表</returns>
    Task<IEnumerable<Embedding>> SearchSimilarWithFilterAsync(
        Vector queryVector,
        int limit,
        Guid? knowledgeBaseId = null,
        Guid? documentId = null);
}