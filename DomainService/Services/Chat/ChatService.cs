using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;

namespace DomainService.Services.Chat;

public class ChatService : IChatService
{
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IRepository<ChatMessage> _chatMessageRepository;
    private readonly IUserRepository _userRepository;

    public ChatService(
        IChatSessionRepository chatSessionRepository,
        IRepository<ChatMessage> chatMessageRepository,
        IUserRepository userRepository)
    {
        _chatSessionRepository = chatSessionRepository;
        _chatMessageRepository = chatMessageRepository;
        _userRepository = userRepository;
    }

    public async Task<ChatSession> CreateSessionAsync(Guid userId, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("会话标题不能为空", nameof(title));

        // 验证用户是否存在
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {userId} 的用户");

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ChatTitle = title,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _chatSessionRepository.AddAsync(session);
        return session;
    }

    public async Task<ChatMessage> AddMessageAsync(Guid sessionId, string content, ChatRole role)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("消息内容不能为空", nameof(content));

        // 验证会话是否存在
        var session = await _chatSessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {sessionId} 的会话");

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _chatMessageRepository.AddAsync(message);

        // 更新会话标题（如果是第一条消息）
        if (role == ChatRole.User && session.Messages.Count == 0)
        {
            // 使用消息内容的前50个字符作为标题
            var newTitle = content.Length > 50 ? content.Substring(0, 50) + "..." : content;
            session.ChatTitle = newTitle;
            await _chatSessionRepository.UpdateAsync(session);
        }

        return message;
    }

    public async Task<IEnumerable<ChatSession>> GetUserSessionsAsync(Guid userId)
    {
        // 验证用户是否存在
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {userId} 的用户");

        var sessions = await _chatSessionRepository.GetByUserIdAsync(userId);
        var activeSessions = new List<ChatSession>();

        foreach (var session in sessions)
        {
            if (!session.IsDeleted)
                activeSessions.Add(session);
        }

        return activeSessions.OrderByDescending(s => s.CreatedAt);
    }

    public async Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(Guid sessionId)
    {
        // 验证会话是否存在
        var session = await _chatSessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {sessionId} 的会话");

        // 使用通用Repository的Find方法查找消息
        var messages = await _chatMessageRepository.FindAsync(m => m.ChatSessionId == sessionId);
        var activeMessages = new List<ChatMessage>();

        foreach (var message in messages)
        {
            if (!message.IsDeleted)
                activeMessages.Add(message);
        }

        return activeMessages.OrderBy(m => m.CreatedAt);
    }
}