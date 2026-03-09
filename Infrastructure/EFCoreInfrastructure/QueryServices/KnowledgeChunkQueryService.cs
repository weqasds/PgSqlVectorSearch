using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.QueryServices;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.QueryServices;

/// <summary>
/// 知识块查询服务实现
/// 负责知识块相关的复杂查询操作
/// </summary>
public class KnowledgeChunkQueryService : QueryServiceBase<KnowledgeChunk>,
                                          IKnowledgeChunkQueryService
{
    public KnowledgeChunkQueryService(RagDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<KnowledgeChunk>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _dbSet
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync();
    }

    public async Task<int> GetChunkCountAsync(Guid documentId)
    {
        return await _dbSet.CountAsync(c => c.DocumentId == documentId);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public async Task<IEnumerable<KnowledgeChunk>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId)
    {
        return await _dbSet
            .Include(c => c.Document)
            .Where(c => c.Document.KnowledgeBaseId == knowledgeBaseId)
            .OrderBy(c => c.DocumentId)
            .ThenBy(c => c.ChunkIndex)
            .ToListAsync();
    }

    public async Task<int> GetChunkCountByKnowledgeBaseIdAsync(Guid knowledgeBaseId)
    {
        return await _dbSet
            .Include(c => c.Document)
            .CountAsync(c => c.Document.KnowledgeBaseId == knowledgeBaseId);
    }

    public async Task<(IEnumerable<KnowledgeChunk> Items, int TotalCount)> GetPagedByDocumentIdAsync(
        Guid documentId,
        int pageIndex,
        int pageSize,
        bool asNoTracking = false)
    {
        IQueryable<KnowledgeChunk> query = _dbSet
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex);

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