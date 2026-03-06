using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace Application.QueryServices;

/// <summary>
/// 知识库查询服务接口
/// 负责知识库相关的复杂查询操作
/// </summary>
public interface IKnowledgeBaseQueryService : IQueryService<KnowledgeBase>
{
    /// <summary>
    /// 根据名称获取知识库
    /// </summary>
    Task<KnowledgeBase?> GetByNameAsync(string name);

    /// <summary>
    /// 检查名称是否存在
    /// </summary>
    Task<bool> ExistsByNameAsync(string name);

    /// <summary>
    /// 获取知识库中的知识块数量
    /// </summary>
    Task<int> GetChunkCountAsync(Guid knowledgeBaseId);

    /// <summary>
    /// 获取活跃的知识库列表（最近30天有访问）
    /// </summary>
    Task<IEnumerable<KnowledgeBase>> GetActiveKnowledgeBasesAsync(int days = 30);

    /// <summary>
    /// 分页获取知识库列表，支持按名称搜索
    /// </summary>
    Task<(IEnumerable<KnowledgeBase> Items, int TotalCount)> GetPagedByNameAsync(
        string? searchTerm,
        int pageIndex,
        int pageSize,
        bool asNoTracking = false);
}