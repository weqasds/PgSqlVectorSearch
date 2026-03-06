using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.Repository;

public class KnowledgeDocumentRepository : RepositoryBase<KnowledgeDocument>, IKnowledgeDocumentRepository
{
    public KnowledgeDocumentRepository(RagDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<KnowledgeDocument>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId)
    {
        return await _dbSet.Where(d => d.KnowledgeBaseId == knowledgeBaseId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetDocumentCountAsync(Guid knowledgeBaseId)
    {
        return await _dbSet.CountAsync(d => d.KnowledgeBaseId == knowledgeBaseId);
    }

    public async Task<int> GetCountByDateRangeAsync(DateOnly start, DateOnly end)
    {
        var startDateTime = start.ToDateTime(TimeOnly.MinValue);
        var endDateTime = end.ToDateTime(TimeOnly.MaxValue);
        return await _dbSet.CountAsync(d => d.CreatedAt >= startDateTime && d.CreatedAt <= endDateTime);
    }

    public async Task<int> GetCountByDateAsync(DateOnly date)
    {
        var startDateTime = date.ToDateTime(TimeOnly.MinValue);
        var endDateTime = date.ToDateTime(TimeOnly.MaxValue);
        return await _dbSet.CountAsync(d => d.CreatedAt >= startDateTime && d.CreatedAt <= endDateTime);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public async Task<int> GetChunkCountAsync(Guid documentId)
    {
        return await _context.KnowledgeChunks.CountAsync(c => c.DocumentId == documentId);
    }
}