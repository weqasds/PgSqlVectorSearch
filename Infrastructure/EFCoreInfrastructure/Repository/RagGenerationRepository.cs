using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.Repository;

public class RagGenerationRepository : RepositoryBase<RagGeneration>, IRagGenerationRepository
{
    public RagGenerationRepository(RagDbContext context) : base(context)
    {
    }

    public async Task<int> GetCountByDateRangeAsync(DateOnly start, DateOnly end)
    {
        var startDateTime = start.ToDateTime(TimeOnly.MinValue);
        var endDateTime = end.ToDateTime(TimeOnly.MaxValue);
        return await _dbSet.CountAsync(r => r.CreatedAt >= startDateTime && r.CreatedAt <= endDateTime);
    }

    public async Task<int> GetCountByDateAsync(DateOnly date)
    {
        var startDateTime = date.ToDateTime(TimeOnly.MinValue);
        var endDateTime = date.ToDateTime(TimeOnly.MaxValue);
        return await _dbSet.CountAsync(r => r.CreatedAt >= startDateTime && r.CreatedAt <= endDateTime);
    }

    public async Task<int> GetCountByKnowledgeBaseIdAsync(Guid knowledgeBaseId)
    {
        // Join RagGeneration -> RagRetrievalLog -> KnowledgeChunk -> KnowledgeDocument -> KnowledgeBase
        return await _dbSet
            .Join(_context.RagRetrievalLogs,
                rg => rg.Id,
                rl => rl.RagGenerationId,
                (rg, rl) => new { rg, rl })
            .Join(_context.KnowledgeChunks,
                joined => joined.rl.ChunkId,
                kc => kc.Id,
                (joined, kc) => new { joined.rg, kc })
            .Join(_context.KnowledgeDocuments,
                joined => joined.kc.DocumentId,
                kd => kd.Id,
                (joined, kd) => new { joined.rg, kd })
            .Where(joined => joined.kd.KnowledgeBaseId == knowledgeBaseId)
            .Select(joined => joined.rg.Id)
            .Distinct()
            .CountAsync();
    }

    public async Task<DateTime?> GetLastQueryTimeByKnowledgeBaseIdAsync(Guid knowledgeBaseId)
    {
        var lastGeneration = await _dbSet
            .Join(_context.RagRetrievalLogs,
                rg => rg.Id,
                rl => rl.RagGenerationId,
                (rg, rl) => new { rg, rl })
            .Join(_context.KnowledgeChunks,
                joined => joined.rl.ChunkId,
                kc => kc.Id,
                (joined, kc) => new { joined.rg, kc })
            .Join(_context.KnowledgeDocuments,
                joined => joined.kc.DocumentId,
                kd => kd.Id,
                (joined, kd) => new { joined.rg, kd })
            .Where(joined => joined.kd.KnowledgeBaseId == knowledgeBaseId)
            .OrderByDescending(joined => joined.rg.CreatedAt)
            .Select(joined => joined.rg)
            .FirstOrDefaultAsync();
        return lastGeneration?.ModifiedAt ?? lastGeneration?.CreatedAt;
    }

    public async Task<IEnumerable<QueryStat>> GetTopQueriesByKnowledgeBaseIdAsync(Guid knowledgeBaseId, int topN)
    {
        var stats = await _dbSet
            .Join(_context.RagRetrievalLogs,
                rg => rg.Id,
                rl => rl.RagGenerationId,
                (rg, rl) => new { rg, rl })
            .Join(_context.KnowledgeChunks,
                joined => joined.rl.ChunkId,
                kc => kc.Id,
                (joined, kc) => new { joined.rg, kc })
            .Join(_context.KnowledgeDocuments,
                joined => joined.kc.DocumentId,
                kd => kd.Id,
                (joined, kd) => new { joined.rg, kd })
            .Where(joined => joined.kd.KnowledgeBaseId == knowledgeBaseId)
            .GroupBy(joined => joined.rg.Prompt)
            .Select(g => new QueryStat(
                g.Key,
                g.Count(),
                g.Max(joined => joined.rg.CreatedAt)
            ))
            .OrderByDescending(q => q.Count)
            .Take(topN)
            .ToListAsync();
        return stats;
    }

    public async Task<IEnumerable<QueryStat>> GetPopularQueriesAsync(int topN)
    {
        var stats = await _dbSet
            .GroupBy(r => r.Prompt)
            .Select(g => new QueryStat(
                g.Key,
                g.Count(),
                g.Max(r => r.CreatedAt)
            ))
            .OrderByDescending(q => q.Count)
            .Take(topN)
            .ToListAsync();
        return stats;
    }
}