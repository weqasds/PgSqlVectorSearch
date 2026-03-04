using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;

namespace PgSqlVectorSearch.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService _authAppService;

    public AuthController(IAuthAppService authAppService)
    {
        _authAppService = authAppService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authAppService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        // Extract token from Authorization header
        var token = ExtractTokenFromHeader();
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("No token provided");
        }

        await _authAppService.LogoutAsync(token);
        return NoContent();
    }

    [HttpGet("user")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var result = await _authAppService.GetCurrentUserAsync();
        if (result == null)
        {
            return Unauthorized();
        }
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authAppService.RegisterAsync(request);
        return CreatedAtAction(nameof(GetCurrentUser), result);
    }

    [HttpPut("password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _authAppService.ChangePasswordAsync(request);
        return NoContent();
    }

    private string? ExtractTokenFromHeader()
    {
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return headerValue.Substring("Bearer ".Length).Trim();
            }
        }
        return null;
    }
}