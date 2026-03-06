using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.Repository;

public class KnowledgeChunkRepository : RepositoryBase<KnowledgeChunk>, IKnowledgeChunkRepository
{
    public KnowledgeChunkRepository(RagDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<KnowledgeChunk>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _dbSet.Where(c => c.DocumentId == documentId)
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
}