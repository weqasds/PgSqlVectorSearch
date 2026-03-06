using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract.Repositories;

public interface IChatSessionRepository : IRepository<ChatSession>
{
    Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<ChatSession>> GetRecentSessionsAsync(int count);

    // Methods for managing session messages
    Task<IEnumerable<ChatMessage>> GetMessagesBySessionIdAsync(Guid sessionId);
    Task DeleteMessageAsync(Guid messageId);

    // Cleanup methods
    Task<IEnumerable<ChatSession>> GetSessionsOlderThanAsync(DateTime cutoffDate);
}