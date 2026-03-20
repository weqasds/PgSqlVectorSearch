using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;
using Application.ApplicationServiceContract.CrossCutting;
using ApplicationService.Dto.Request;

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

    /// <summary>
    /// 获取指定聊天会话的详细信息
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("conversations/{id}")]
    public async Task<ActionResult<ChatSessionDto>> GetConversation(Guid id)
    {
        // Get current user ID from authentication context
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }

        try
        {
            var session = await _ragChatAppService.GetChatSessionByIdAsync(id);

            // 验证会话是否属于当前用户
            if (session.UserId != currentUserId.Value)
            {
                return Forbid(); // 或者返回NotFound以隐藏存在性
            }

            return Ok(session);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
    }

    [HttpDelete("conversations/{id}")]
    public async Task<ActionResult> DeleteConversation(Guid id)
    {
        // Get current user ID from authentication context
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }

        try
        {
            // 首先获取会话以验证所有权
            var session = await _ragChatAppService.GetChatSessionByIdAsync(id);
            if (session.UserId != currentUserId.Value)
            {
                return Forbid(); // 或者返回NotFound以隐藏存在性
            }

            // 删除会话
            await _ragChatAppService.DeleteChatSessionAsync(id);
            return NoContent();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
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
        // 可以添加用户认证，但允许匿名访问可能更有用
        // 如果需要认证，可以取消注释下面的代码
        /*
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }
        */

        try
        {
            var result = await _ragChatAppService.DirectRagAsync(request.Query, request.KnowledgeBaseId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // 根据异常类型返回适当的HTTP状态码
            return StatusCode(500, $"Direct RAG processing failed: {ex.Message}");
        }
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