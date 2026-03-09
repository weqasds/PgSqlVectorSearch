using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;

namespace DomainService.ServiceContract.Services;

public interface IChatService
{
    Task<ChatSession> CreateSessionAsync(Guid userId, string title);
    Task<ChatSession> CreateSessionWithKnowledgeBaseAsync(Guid userId, string title, Guid? knowledgeBaseId);
    Task<ChatMessage> AddMessageAsync(Guid sessionId, string content, ChatRole role);
    Task<IEnumerable<ChatSession>> GetUserSessionsAsync(Guid userId);
    Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(Guid sessionId);

    /// <summary>
    /// 设置对话的主要知识库
    /// </summary>
    Task SetSessionKnowledgeBaseAsync(Guid sessionId, Guid? knowledgeBaseId);

    /// <summary>
    /// 记录对话中引用的知识库
    /// </summary>
    Task RecordKnowledgeBaseReferenceAsync(Guid sessionId, Guid knowledgeBaseId);

    /// <summary>
    /// 获取对话引用的所有知识库（包括主要和引用的）
    /// </summary>
    Task<IEnumerable<KnowledgeBase>> GetSessionKnowledgeBasesAsync(Guid sessionId);
}