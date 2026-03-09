using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;

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

    // Analytics methods
    Task<int> GetTotalCountAsync();
}