using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;
using Pgvector;

namespace DomainService.ServiceContract.Repositories;

public interface IEmbeddingRepository : IRepository<Embedding>
{
    Task<Embedding?> GetByChunkIdAsync(Guid chunkId);
    Task<IEnumerable<Embedding>> SearchSimilarAsync(Vector queryVector, int limit);
}