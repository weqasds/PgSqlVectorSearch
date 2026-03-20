using DomainService.Entities.RAG;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using Application.ApplicationServiceContract;
using DomainService.Entities.Chat;
using Application.ApplicationServiceContract.CrossCutting;
using ApplicationService.Dto.Request;
using Application.Dto;
namespace Application.ApplicationService;

public class RagChatAppService : IRagChatAppService
{
    private readonly IChatService _chatService;
    private readonly IRagGenerationService _ragGenerationService;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RagChatAppService(
        IChatService chatService,
        IRagGenerationService ragGenerationService,
        IKnowledgeBaseService knowledgeBaseService,
        IChatSessionRepository chatSessionRepository,
        IUnitOfWork unitOfWork)
    {
        _chatService = chatService;
        _ragGenerationService = ragGenerationService;
        _knowledgeBaseService = knowledgeBaseService;
        _chatSessionRepository = chatSessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChatSessionDto> CreateChatSessionAsync(Guid userId, string title)
    {
        var session = await _chatService.CreateSessionAsync(userId, title);
        await _unitOfWork.SaveChangesAsync();
        return await MapToDtoAsync(session);
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
            // If knowledge base is specified, restrict search to that knowledge base
            ragResponse = await _ragGenerationService.GenerateResponseAsync(
                request.Message,
                sessionId,
                request.KnowledgeBaseId.Value);
        }
        else
        {
            ragResponse = await _ragGenerationService.GenerateResponseAsync(request.Message, sessionId);
        }

        // Add assistant message to session
        await _chatService.AddMessageAsync(sessionId, ragResponse.Answer, ChatRole.Assistant);
        await _unitOfWork.SaveChangesAsync();
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

        // Convert each session to DTO asynchronously
        var sessionDtos = new List<ChatSessionDto>();
        foreach (var session in sessions)
        {
            var dto = await MapToDtoAsync(session);
            sessionDtos.Add(dto);
        }

        return sessionDtos;
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
            Title = session.ChatTitle,
            Messages = messages.Select(MapToMessageDto),
            CreatedAt = session.CreatedAt
        };
    }

    private async Task<ChatSessionDto> MapToDtoAsync(ChatSession session)
    {
        // Get actual message count from repository
        var messages = await _chatService.GetSessionMessagesAsync(session.Id);
        var messageCount = messages?.Count() ?? 0;

        return new ChatSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Title = session.ChatTitle,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.ModifiedAt,
            MessageCount = messageCount
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

    public async Task<ChatSessionDto> GetChatSessionByIdAsync(Guid sessionId)
    {
        var session = await _chatSessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new ArgumentException($"Chat session with id {sessionId} not found.");
        }

        return await MapToDtoAsync(session);
    }

    public async Task DeleteChatSessionAsync(Guid sessionId)
    {
        var session = await _chatSessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new ArgumentException($"Chat session with id {sessionId} not found.");
        }

        await _chatSessionRepository.DeleteAsync(session);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<ChatResponseDto> DirectRagAsync(string query, Guid? knowledgeBaseId = null)
    {
        // Generate RAG response without session
        RagResponse ragResponse;
        if (knowledgeBaseId.HasValue)
        {
            ragResponse = await _ragGenerationService.GenerateResponseAsync(
                query,
                null,  // No session ID for direct RAG
                knowledgeBaseId.Value);
        }
        else
        {
            ragResponse = await _ragGenerationService.GenerateResponseAsync(query, null);
        }
        await _unitOfWork.SaveChangesAsync();
        // Map to DTO
        return new ChatResponseDto
        {
            Answer = ragResponse.Answer,
            RetrievedChunks = ragResponse.RetrievedChunks.Select(MapToRetrievedChunkDto),
            SessionId = null,  // No session for direct RAG
            GeneratedAt = ragResponse.GeneratedAt
        };
    }
}