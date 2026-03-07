using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using Pgvector;

namespace DomainService.ServiceContract.Repositories;

public interface IEmbeddingRepository : IRepository<Embedding>
{
    Task<Embedding?> GetByChunkIdAsync(Guid chunkId);
    Task<IEnumerable<Embedding>> SearchSimilarAsync(Vector queryVector, int limit);
}