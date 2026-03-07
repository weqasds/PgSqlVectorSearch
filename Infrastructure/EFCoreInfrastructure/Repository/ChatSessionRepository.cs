using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.Repository;

public class ChatSessionRepository : RepositoryBase<ChatSession>, IChatSessionRepository
{
    public ChatSessionRepository(RagDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ChatSession>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet.Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ModifiedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatSession>> GetRecentSessionsAsync(int count)
    {
        return await _dbSet.OrderByDescending(s => s.ModifiedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesBySessionIdAsync(Guid sessionId)
    {
        return await _context.ChatMessages
            .Where(m => m.ChatSessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        var message = await _context.ChatMessages.FindAsync(messageId);
        if (message != null)
        {
            _context.ChatMessages.Remove(message);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ChatSession>> GetSessionsOlderThanAsync(DateTime cutoffDate)
    {
        return await _dbSet.Where(s => s.CreatedAt < cutoffDate)
            .ToListAsync();
    }
}