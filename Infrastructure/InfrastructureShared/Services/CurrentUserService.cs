using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using DomainService.ServiceContract.CrossCutting;

namespace InfrastructureShared.Services;

/// <summary>
/// 当前用户服务实现
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Guid? GetCurrentUserId()
    {
        var userIdClaim = GetClaimValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }

    public string? GetCurrentUserName()
    {
        return GetClaimValue(ClaimTypes.Name);
    }

    public string? GetCurrentUserEmail()
    {
        return GetClaimValue(ClaimTypes.Email);
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }

    public bool IsInRole(string role)
    {
        if (string.IsNullOrEmpty(role))
            return false;

        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }

    public bool HasClaim(string claimType, string claimValue)
    {
        if (string.IsNullOrEmpty(claimType) || string.IsNullOrEmpty(claimValue))
            return false;

        return _httpContextAccessor.HttpContext?.User?
            .Claims
            .Any(c => c.Type == claimType && c.Value == claimValue) ?? false;
    }

    private string? GetClaimValue(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(claimType)?.Value;
    }
}