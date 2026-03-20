using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;
using ApplicationService.Dto.Request;

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
    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authAppService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// 登出
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// 注册
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authAppService.RegisterAsync(request);
        return CreatedAtAction(nameof(GetCurrentUser), result);
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _authAppService.ChangePasswordAsync(request);
        return NoContent();
    }
    /// <summary>
    /// 从Authorization头中提取Bearer令牌
    /// </summary>
    /// <returns></returns>
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