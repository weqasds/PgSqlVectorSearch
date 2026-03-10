using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;

namespace DomainService.ServiceContract.Services;

public interface IChatService
{
    /// <summary>
    /// 创建新的对话会话
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="title"></param>
    /// <returns></returns>
    Task<ChatSession> CreateSessionAsync(Guid userId, string title);
    /// <summary>
    /// 或者创建新的对话会话并关联一个知识库
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="title"></param>
    /// <param name="knowledgeBaseId"></param>
    /// <returns></returns>
    Task<ChatSession> CreateSessionWithKnowledgeBaseAsync(Guid userId, string title, Guid? knowledgeBaseId);
    /// <summary>
    /// 添加消息到对话会话
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="content"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    Task<ChatMessage> AddMessageAsync(Guid sessionId, string content, ChatRole role);
    /// <summary>
    /// 获取用户的所有对话会话
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<IEnumerable<ChatSession>> GetUserSessionsAsync(Guid userId);
    /// <summary>
    /// 获取对话会话中的所有消息
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
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