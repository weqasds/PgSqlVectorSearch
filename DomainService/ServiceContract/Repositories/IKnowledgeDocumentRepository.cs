using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;

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
    Task AddChunksAsync(IEnumerable<KnowledgeChunk> knowledgeChunks);
}