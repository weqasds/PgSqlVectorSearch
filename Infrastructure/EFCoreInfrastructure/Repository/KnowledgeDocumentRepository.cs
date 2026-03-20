using DomainService.Entities.KnowledgeManagement;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.EFCoreInfrastructure.Repository;

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
        var startDateTime = start.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
        var endDateTime = end.ToDateTime(TimeOnly.MaxValue).ToUniversalTime();
        return await _dbSet.CountAsync(d => d.CreatedAt >= startDateTime && d.CreatedAt <= endDateTime);
    }

    public async Task<int> GetCountByDateAsync(DateOnly date)
    {
        var startDateTime = date.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
        var endDateTime = date.ToDateTime(TimeOnly.MaxValue).ToUniversalTime();
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
    public async Task AddChunksAsync(IEnumerable<KnowledgeChunk> knowledgeChunks)
    {
        await _context.Set<KnowledgeChunk>().AddRangeAsync(knowledgeChunks);
    }
}