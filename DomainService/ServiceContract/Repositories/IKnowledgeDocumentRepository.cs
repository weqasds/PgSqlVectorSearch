using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract.Repositories;

public interface IKnowledgeDocumentRepository : IRepository<KnowledgeDocument>
{
    Task<IEnumerable<KnowledgeDocument>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId);
    Task<int> GetDocumentCountAsync(Guid knowledgeBaseId);

    // Analytics methods
    Task<int> GetCountByDateRangeAsync(DateOnly start, DateOnly end);
    Task<int> GetCountByDateAsync(DateOnly date);
    Task<int> GetTotalCountAsync();
    Task<int> GetChunkCountAsync(Guid documentId);
}