using ApplicationService.Dto.Request;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.ApplicationServiceContract;

public interface IRagChatAppService
{
    /// <summary>
    /// 创建新的聊天会话
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="title"></param>
    /// <returns></returns>
    Task<ChatSessionDto> CreateChatSessionAsync(Guid userId, string title);
    /// <summary>
    /// 发送消息并获取回复
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<ChatResponseDto> SendMessageAsync(Guid sessionId, SendMessageRequest request);
    /// <summary>
    /// 获取用户的聊天会话列表
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<IEnumerable<ChatSessionDto>> GetUserChatSessionsAsync(Guid userId);
    /// <summary>
    /// 获取聊天会话的历史消息
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    Task<ChatHistoryDto> GetChatHistoryAsync(Guid sessionId);

    /// <summary>
    /// 根据ID获取聊天会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>聊天会话DTO</returns>
    Task<ChatSessionDto> GetChatSessionByIdAsync(Guid sessionId);

    /// <summary>
    /// 删除聊天会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns></returns>
    Task DeleteChatSessionAsync(Guid sessionId);

    /// <summary>
    /// 直接进行RAG检索生成响应（不关联聊天会话）
    /// </summary>
    /// <param name="query">用户查询</param>
    /// <param name="knowledgeBaseId">知识库ID（可选）</param>
    /// <returns>RAG响应</returns>
    Task<ChatResponseDto> DirectRagAsync(string query, Guid? knowledgeBaseId = null);
}