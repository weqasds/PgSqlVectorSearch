using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract.Repositories;

public interface IKnowledgeChunkRepository : IRepository<KnowledgeChunk>
{
    Task<IEnumerable<KnowledgeChunk>> GetByDocumentIdAsync(Guid documentId);
    Task<int> GetChunkCountAsync(Guid documentId);

    // Analytics methods
    Task<int> GetTotalCountAsync();
}