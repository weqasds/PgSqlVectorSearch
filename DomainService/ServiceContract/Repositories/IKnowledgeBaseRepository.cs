using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;

namespace DomainService.ServiceContract.Repositories;

public interface IKnowledgeBaseRepository : IRepository<KnowledgeBase>
{
    Task<KnowledgeBase?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);

    // Analytics methods
    Task<int> GetChunkCountAsync(Guid knowledgeBaseId);

    // User access permission methods
    Task<KnowledgeBase> GetKnowledgeBaseWithUserAccessesAsync(Guid knowledgeBaseId);
    Task<IEnumerable<UserKnowledgeBase>> GetKnowledgeBaseUserAccessesAsync(Guid knowledgeBaseId);
}