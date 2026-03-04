using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.ApplicationServiceContract;

public interface IRagChatAppService
{
    Task<ChatSessionDto> CreateChatSessionAsync(Guid userId, string title);
    Task<ChatResponseDto> SendMessageAsync(Guid sessionId, SendMessageRequest request);
    Task<IEnumerable<ChatSessionDto>> GetUserChatSessionsAsync(Guid userId);
    Task<ChatHistoryDto> GetChatHistoryAsync(Guid sessionId);
}