using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace EFCoreInfrastructure.Repository;

public class EmbeddingRepository : RepositoryBase<Embedding>, IEmbeddingRepository
{
    public EmbeddingRepository(RagDbContext context) : base(context)
    {
    }

    public async Task<Embedding?> GetByChunkIdAsync(Guid chunkId)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.ChunkId == chunkId);
    }

    public async Task<IEnumerable<Embedding>> SearchSimilarAsync(Vector queryVector, int limit)
    {
        // Use pgvector similarity search via EF.Functions.L2Distance (Euclidean distance)
        // Note: Cosine distance may not be available; using L2 distance as fallback
        return await _dbSet
            .OrderBy(e => e.Vector.L2Distance(queryVector))
            .Take(limit)
            .ToListAsync();
    }
}