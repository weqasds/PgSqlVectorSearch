using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract;

public interface IKnowledgeDocumentRepository : IRepository<KnowledgeDocument>
{
    Task<IEnumerable<KnowledgeDocument>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId);
    Task<int> GetDocumentCountAsync(Guid knowledgeBaseId);
}