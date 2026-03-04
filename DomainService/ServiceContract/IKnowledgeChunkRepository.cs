using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract;

public interface IKnowledgeChunkRepository : IRepository<KnowledgeChunk>
{
    Task<IEnumerable<KnowledgeChunk>> GetByDocumentIdAsync(Guid documentId);
    Task<int> GetChunkCountAsync(Guid documentId);
}