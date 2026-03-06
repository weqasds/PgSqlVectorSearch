using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract.Repositories;

public interface IChatMessageRepository : IRepository<ChatMessage>
{
    Task<IEnumerable<ChatMessage>> GetBySessionIdAsync(Guid sessionId);
    Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(int count);
}