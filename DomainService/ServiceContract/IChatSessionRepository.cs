using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract;

public interface IChatSessionRepository : IRepository<ChatSession>
{
    Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<ChatSession>> GetRecentSessionsAsync(int count);
}