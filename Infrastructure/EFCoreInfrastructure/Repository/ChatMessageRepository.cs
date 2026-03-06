using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.Repository;

public class ChatMessageRepository : RepositoryBase<ChatMessage>, IChatMessageRepository
{
    public ChatMessageRepository(RagDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ChatMessage>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _dbSet.Where(m => m.ChatSessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(int count)
    {
        return await _dbSet.OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}