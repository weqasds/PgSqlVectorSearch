using System;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract.Repositories;

public interface IKnowledgeBaseRepository : IRepository<KnowledgeBase>
{
    Task<KnowledgeBase?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);

    // Analytics methods
    Task<int> GetChunkCountAsync(Guid knowledgeBaseId);
}