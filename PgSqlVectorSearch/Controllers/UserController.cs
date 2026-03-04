using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;

namespace PgSqlVectorSearch.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserAppService _userAppService;

    public UserController(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        // Check if current user has permission to view this user
        if (!CanViewUser(id))
        {
            return Forbid();
        }

        var result = await _userAppService.GetUserAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpPut("{id}/profile")]
    public async Task<ActionResult<UserDto>> UpdateUserProfile(Guid id, [FromBody] UpdateUserProfileRequest request)
    {
        // Check if current user has permission to update this user
        if (!CanUpdateUser(id))
        {
            return Forbid();
        }

        var result = await _userAppService.UpdateUserProfileAsync(id, request);
        return Ok(result);
    }

    [HttpGet("{id}/sessions")]
    public async Task<ActionResult<IEnumerable<ChatSessionDto>>> GetUserSessions(Guid id)
    {
        // Check if current user has permission to view this user's sessions
        if (!CanViewUser(id))
        {
            return Forbid();
        }

        var result = await _userAppService.GetUserSessionsAsync(id);
        return Ok(result);
    }

    private bool CanViewUser(Guid userId)
    {
        // TODO: Implement authorization logic
        // For now, allow users to view their own profile only
        var currentUserId = GetCurrentUserId();
        return currentUserId.HasValue && currentUserId.Value == userId;
    }

    private bool CanUpdateUser(Guid userId)
    {
        // TODO: Implement authorization logic
        // For now, allow users to update their own profile only
        var currentUserId = GetCurrentUserId();
        return currentUserId.HasValue && currentUserId.Value == userId;
    }

    private Guid? GetCurrentUserId()
    {
        // TODO: Extract user ID from authentication token
        // For now, return a placeholder
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}