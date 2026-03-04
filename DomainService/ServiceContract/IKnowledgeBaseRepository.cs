using System;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract;

public interface IKnowledgeBaseRepository : IRepository<KnowledgeBase>
{
    Task<KnowledgeBase?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);
}