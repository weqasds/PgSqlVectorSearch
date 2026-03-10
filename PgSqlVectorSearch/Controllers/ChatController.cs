using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;
using Application.ApplicationServiceContract.CrossCutting;

namespace PgSqlVectorSearch.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IRagChatAppService _ragChatAppService;
    private readonly ICurrentUserService _currentUserService;

    public ChatController(IRagChatAppService ragChatAppService, ICurrentUserService currentUserService)
    {
        _ragChatAppService = ragChatAppService;
        _currentUserService = currentUserService;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<IEnumerable<ChatSessionDto>>> GetConversations()
    {
        // Get current user ID from authentication context
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var result = await _ragChatAppService.GetUserChatSessionsAsync(currentUserId.Value);
        return Ok(result);
    }
    
    /// <summary>
    /// 创建新的聊天会话
    /// </summary>
    /// <param name="request">请求体</param>
    /// <returns>创建结果</returns>
    [HttpPost("conversations")]
    public async Task<ActionResult<ChatSessionDto>> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var result = await _ragChatAppService.CreateChatSessionAsync(currentUserId.Value, request.Title);
        return CreatedAtAction(nameof(GetConversation), new { id = result.Id }, result);
    }

    [HttpGet("conversations/{id}")]
    public async Task<ActionResult<ChatSessionDto>> GetConversation(Guid id)
    {
        // Note: GetChatSessionById method not defined in interface
        // For now, return Not Implemented
        return StatusCode(501, "Get conversation by ID not implemented");
    }

    [HttpDelete("conversations/{id}")]
    public async Task<ActionResult> DeleteConversation(Guid id)
    {
        // Note: DeleteChatSession method not defined in interface
        return StatusCode(501, "Delete conversation not implemented");
    }

    [HttpGet("conversations/{id}/messages")]
    public async Task<ActionResult<ChatHistoryDto>> GetConversationMessages(Guid id)
    {
        var result = await _ragChatAppService.GetChatHistoryAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpPost("messages")]
    public async Task<ActionResult<ChatResponseDto>> SendMessage([FromBody] SendChatMessageRequest request)
    {
        if (request.ConversationId == null)
        {
            return BadRequest("ConversationId is required");
        }

        var sendMessageRequest = new SendMessageRequest
        {
            Message = request.Message,
            KnowledgeBaseId = request.KnowledgeBaseId
        };

        var result = await _ragChatAppService.SendMessageAsync(request.ConversationId.Value, sendMessageRequest);
        return Ok(result);
    }

    [HttpPost("rag")]
    public async Task<ActionResult<ChatResponseDto>> DirectRag([FromBody] DirectRagRequest request)
    {
        // Note: Direct RAG method not defined in interface
        // Could be implemented as a separate service method
        return StatusCode(501, "Direct RAG not implemented");
    }

    private Guid? GetCurrentUserId()
    {
        // TODO: Extract user ID from authentication token
        // For now, return a placeholder
        return _currentUserService.GetCurrentUserId();
    }
}

// Request DTOs for ChatController
public class CreateConversationRequest
{
    public string Title { get; set; } = string.Empty;
}

public class SendChatMessageRequest
{
    public Guid? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? KnowledgeBaseId { get; set; }
}

public class DirectRagRequest
{
    public string Query { get; set; } = string.Empty;
    public Guid? KnowledgeBaseId { get; set; }
}