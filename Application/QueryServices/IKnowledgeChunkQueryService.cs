using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;


namespace Application.QueryServices;

/// <summary>
/// 知识块查询服务接口
/// 负责知识块相关的复杂查询操作
/// </summary>
public interface IKnowledgeChunkQueryService : IQueryService<KnowledgeChunk>
{
    /// <summary>
    /// 根据文档ID获取知识块列表
    /// </summary>
    Task<IEnumerable<KnowledgeChunk>> GetByDocumentIdAsync(Guid documentId);

    /// <summary>
    /// 获取文档中的知识块数量
    /// </summary>
    Task<int> GetChunkCountAsync(Guid documentId);

    /// <summary>
    /// 获取总知识块数量（用于统计）
    /// </summary>
    Task<int> GetTotalCountAsync();

    /// <summary>
    /// 根据知识库ID获取知识块列表
    /// </summary>
    Task<IEnumerable<KnowledgeChunk>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId);

    /// <summary>
    /// 获取知识库中的知识块数量
    /// </summary>
    Task<int> GetChunkCountByKnowledgeBaseIdAsync(Guid knowledgeBaseId);

    /// <summary>
    /// 分页获取文档中的知识块
    /// </summary>
    Task<(IEnumerable<KnowledgeChunk> Items, int TotalCount)> GetPagedByDocumentIdAsync(
        Guid documentId,
        int pageIndex,
        int pageSize,
        bool asNoTracking = false);
}