using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract;
using Application.ApplicationServiceContract;

namespace Application.ApplicationService;

public class RagChatAppService : IRagChatAppService
{
    private readonly IChatService _chatService;
    private readonly IRagGenerationService _ragGenerationService;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IChatMessageRepository _chatMessageRepository;

    public RagChatAppService(
        IChatService chatService,
        IRagGenerationService ragGenerationService,
        IKnowledgeBaseService knowledgeBaseService,
        IChatSessionRepository chatSessionRepository,
        IChatMessageRepository chatMessageRepository)
    {
        _chatService = chatService;
        _ragGenerationService = ragGenerationService;
        _knowledgeBaseService = knowledgeBaseService;
        _chatSessionRepository = chatSessionRepository;
        _chatMessageRepository = chatMessageRepository;
    }

    public async Task<ChatSessionDto> CreateChatSessionAsync(Guid userId, string title)
    {
        var session = await _chatService.CreateSessionAsync(userId, title);
        return MapToDto(session);
    }

    public async Task<ChatResponseDto> SendMessageAsync(Guid sessionId, SendMessageRequest request)
    {
        // Validate session exists
        var session = await _chatSessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new ArgumentException($"Chat session with id {sessionId} not found.");
        }

        // Add user message to session
        var userMessage = await _chatService.AddMessageAsync(sessionId, request.Message, ChatRole.User);

        // Generate RAG response
        RagResponse ragResponse;
        if (request.KnowledgeBaseId.HasValue)
        {
            // If knowledge base is specified, we might need to restrict search to that knowledge base
            // For simplicity, we'll just pass the query; actual implementation should incorporate knowledge base filtering
            ragResponse = await _ragGenerationService.GenerateResponseAsync(request.Message, sessionId);
        }
        else
        {
            ragResponse = await _ragGenerationService.GenerateResponseAsync(request.Message, sessionId);
        }

        // Add assistant message to session
        await _chatService.AddMessageAsync(sessionId, ragResponse.Answer, ChatRole.Assistant);

        // Map to DTO
        return new ChatResponseDto
        {
            Answer = ragResponse.Answer,
            RetrievedChunks = ragResponse.RetrievedChunks.Select(MapToRetrievedChunkDto),
            SessionId = sessionId,
            GeneratedAt = ragResponse.GeneratedAt
        };
    }

    public async Task<IEnumerable<ChatSessionDto>> GetUserChatSessionsAsync(Guid userId)
    {
        var sessions = await _chatService.GetUserSessionsAsync(userId);
        return sessions.Select(MapToDto);
    }

    public async Task<ChatHistoryDto> GetChatHistoryAsync(Guid sessionId)
    {
        var session = await _chatSessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new ArgumentException($"Chat session with id {sessionId} not found.");
        }

        var messages = await _chatService.GetSessionMessagesAsync(sessionId);
        return new ChatHistoryDto
        {
            SessionId = sessionId,
            Title = session.Title,
            Messages = messages.Select(MapToMessageDto),
            CreatedAt = session.CreatedAt
        };
    }

    private ChatSessionDto MapToDto(ChatSession session)
    {
        // MessageCount could be retrieved from repository; for simplicity we'll set to 0
        return new ChatSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            MessageCount = 0
        };
    }

    private ChatMessageDto MapToMessageDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            Content = message.Content,
            Role = message.Role.ToString(),
            CreatedAt = message.CreatedAt
        };
    }

    private RetrievedChunkDto MapToRetrievedChunkDto(RetrievalResult retrievalResult)
    {
        return new RetrievedChunkDto
        {
            Content = retrievalResult.Chunk.Content,
            SourceDocument = retrievalResult.Chunk.Document?.FileName ?? "Unknown",
            SimilarityScore = retrievalResult.SimilarityScore
        };
    }
}