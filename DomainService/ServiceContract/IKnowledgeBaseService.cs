using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract;

public interface IKnowledgeBaseService
{
    Task<KnowledgeBase> CreateKnowledgeBaseAsync(string name, string description);
    Task<KnowledgeBase> GetKnowledgeBaseAsync(Guid id);
    Task<IEnumerable<KnowledgeBase>> ListKnowledgeBasesAsync();
    Task DeleteKnowledgeBaseAsync(Guid id);
}