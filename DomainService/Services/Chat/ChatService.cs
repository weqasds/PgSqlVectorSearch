using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;
using DomainService.Entities.KnowledgeGraph;
using DomainService.Entities.Enums;
using DomainService.Entities.VectorSearch;
using DomainService.Entities.Chat;
using DomainService.Entities.UserManagement;
using DomainService.Entities.System;
using DomainService.Entities.RAG;
using DomainService.Entities.Analytics;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;


namespace DomainService.Services.Chat;

public class ChatService : IChatService
{
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;

    public ChatService(
        IChatSessionRepository chatSessionRepository,
        IUserRepository userRepository,
        IKnowledgeBaseRepository knowledgeBaseRepository)
    {
        _chatSessionRepository = chatSessionRepository;
        _userRepository = userRepository;
        _knowledgeBaseRepository = knowledgeBaseRepository;
    }

    public async Task<ChatSession> CreateSessionAsync(Guid userId, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("会话标题不能为空", nameof(title));

        // 验证用户是否存在
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {userId} 的用户");

        var session = ChatSession.Create(userId, title);

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

        // 使用聚合根的方法添加消息
        var message = session.AddMessage(content, role);

        // 更新会话标题（如果是第一条用户消息）
        if (role == ChatRole.User && session.GetMessageCount() == 1)
        {
            // 使用消息内容的前50个字符作为标题
            var newTitle = content.Length > 50 ? content.Substring(0, 50) + "..." : content;
            session.UpdateTitle(newTitle);
        }

        // 保存会话变更（包括新消息）
        await _chatSessionRepository.UpdateAsync(session);
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

        // 通过聚合根仓储获取消息
        var messages = await _chatSessionRepository.GetMessagesBySessionIdAsync(sessionId);
        var activeMessages = new List<ChatMessage>();

        foreach (var message in messages)
        {
            if (!message.IsDeleted)
                activeMessages.Add(message);
        }

        return activeMessages.OrderBy(m => m.CreatedAt);
    }

    public async Task<ChatSession> CreateSessionWithKnowledgeBaseAsync(Guid userId, string title, Guid? knowledgeBaseId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("会话标题不能为空", nameof(title));

        // 验证用户是否存在
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {userId} 的用户");

        // 如果指定了知识库ID，验证知识库是否存在
        if (knowledgeBaseId.HasValue)
        {
            var knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId.Value);
            if (knowledgeBase == null || knowledgeBase.IsDeleted)
                throw new KeyNotFoundException($"未找到ID为 {knowledgeBaseId.Value} 的知识库");
        }

        var session = ChatSession.Create(userId, title, knowledgeBaseId);

        await _chatSessionRepository.AddAsync(session);
        return session;
    }

    public async Task SetSessionKnowledgeBaseAsync(Guid sessionId, Guid? knowledgeBaseId)
    {
        var session = await _chatSessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {sessionId} 的会话");

        // 如果指定了知识库ID，验证知识库是否存在
        if (knowledgeBaseId.HasValue)
        {
            var knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId.Value);
            if (knowledgeBase == null || knowledgeBase.IsDeleted)
                throw new KeyNotFoundException($"未找到ID为 {knowledgeBaseId.Value} 的知识库");
        }

        session.KnowledgeBaseId = knowledgeBaseId;
        session.ModifiedAt = DateTime.UtcNow;
        await _chatSessionRepository.UpdateAsync(session);
    }

    public async Task RecordKnowledgeBaseReferenceAsync(Guid sessionId, Guid knowledgeBaseId)
    {
        var session = await _chatSessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {sessionId} 的会话");

        // 验证知识库存在
        var knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {knowledgeBaseId} 的知识库");

        // 解析现有的引用知识库ID列表
        var referencedIds = ParseReferencedKnowledgeBaseIds(session.ReferencedKnowledgeBaseIds);

        // 如果知识库ID不在列表中，添加它
        if (!referencedIds.Contains(knowledgeBaseId))
        {
            referencedIds.Add(knowledgeBaseId);
            session.ReferencedKnowledgeBaseIds = SerializeReferencedKnowledgeBaseIds(referencedIds);
            session.ModifiedAt = DateTime.UtcNow;
            await _chatSessionRepository.UpdateAsync(session);
        }
    }

    public async Task<IEnumerable<KnowledgeBase>> GetSessionKnowledgeBasesAsync(Guid sessionId)
    {
        var session = await _chatSessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {sessionId} 的会话");

        var knowledgeBases = new List<KnowledgeBase>();

        // 添加主要知识库（如果有）
        if (session.KnowledgeBaseId.HasValue)
        {
            var mainKnowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(session.KnowledgeBaseId.Value);
            if (mainKnowledgeBase != null && !mainKnowledgeBase.IsDeleted)
            {
                knowledgeBases.Add(mainKnowledgeBase);
            }
        }

        // 添加引用的知识库
        var referencedIds = ParseReferencedKnowledgeBaseIds(session.ReferencedKnowledgeBaseIds);
        foreach (var knowledgeBaseId in referencedIds)
        {
            // 避免重复添加主要知识库
            if (session.KnowledgeBaseId.HasValue && knowledgeBaseId == session.KnowledgeBaseId.Value)
                continue;

            var knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId);
            if (knowledgeBase != null && !knowledgeBase.IsDeleted)
            {
                knowledgeBases.Add(knowledgeBase);
            }
        }

        return knowledgeBases;
    }

    private List<Guid> ParseReferencedKnowledgeBaseIds(string? jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            return new List<Guid>();

        try
        {
            // 简单的JSON解析：假设格式为["guid1", "guid2", ...]
            // 实际实现应该使用System.Text.Json或Newtonsoft.Json
            if (jsonString.StartsWith("[") && jsonString.EndsWith("]"))
            {
                var content = jsonString.Trim('[', ']').Replace("\"", "").Replace("'", "");
                if (string.IsNullOrWhiteSpace(content))
                    return new List<Guid>();

                var idStrings = content.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var ids = new List<Guid>();

                foreach (var idString in idStrings)
                {
                    if (Guid.TryParse(idString.Trim(), out var id))
                    {
                        ids.Add(id);
                    }
                }

                return ids;
            }
        }
        catch
        {
            // 如果解析失败，返回空列表
        }

        return new List<Guid>();
    }

    private string SerializeReferencedKnowledgeBaseIds(List<Guid> ids)
    {
        if (ids == null || !ids.Any())
            return "[]";

        var idStrings = ids.Select(id => $"\"{id}\"");
        return $"[{string.Join(",", idStrings)}]";
    }
}