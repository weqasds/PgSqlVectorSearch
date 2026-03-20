using Application.QueryServices;
using DomainService.Entities.KnowledgeManagement;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCoreInfrastructure.QueryServices;

/// <summary>
/// 知识库查询服务实现
/// 负责知识库相关的复杂查询操作
/// </summary>
public class KnowledgeBaseQueryService : QueryServiceBase<KnowledgeBase>, IKnowledgeBaseQueryService
{
    public KnowledgeBaseQueryService(RagDbContext context) : base(context)
    {
    }

    public async Task<KnowledgeBase?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(kb => kb.Name == name);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _dbSet.AnyAsync(kb => kb.Name == name);
    }

    public async Task<int> GetChunkCountAsync(Guid knowledgeBaseId)
    {
        // 通过Document关联查询Chunk数量
        return await _context.Set<KnowledgeDocument>()
            .Where(doc => doc.KnowledgeBaseId == knowledgeBaseId)
            .Join(_context.Set<KnowledgeChunk>(),
                doc => doc.Id,
                chunk => chunk.DocumentId,
                (doc, chunk) => chunk)
            .CountAsync();
    }

    public async Task<IEnumerable<KnowledgeBase>> GetActiveKnowledgeBasesAsync(int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        // 获取最近有访问记录的知识库
        // 这里需要根据实际业务逻辑调整，可能需要关联其他表（如访问日志）
        return await _dbSet
            .Where(kb => kb.ModifiedAt >= cutoffDate)
            .OrderByDescending(kb => kb.ModifiedAt)
            .ToListAsync();
    }

    public async Task<(IEnumerable<KnowledgeBase> Items, int TotalCount)> GetPagedByNameAsync(
        string? searchTerm,
        int pageIndex,
        int pageSize,
        bool asNoTracking = false)
    {
        IQueryable<KnowledgeBase> query = _dbSet;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(kb => kb.Name.Contains(searchTerm) ||
                                      (kb.Description != null && kb.Description.Contains(searchTerm)));
        }

        query = query.OrderBy(kb => kb.Name);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}