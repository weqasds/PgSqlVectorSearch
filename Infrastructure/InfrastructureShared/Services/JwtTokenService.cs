using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DomainService.ServiceContract.CrossCutting;
using DomainService.Entities.UserManagement;

namespace InfrastructureShared.Services;

/// <summary>
/// JWT令牌服务实现
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtTokenService(IOptions<JwtSettings> jwtSettingsOptions)
    {
        _jwtSettings = jwtSettingsOptions.Value;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken(User user)
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public (bool IsValid, ClaimsPrincipal? Principal) ValidateAccessToken(string token)
    {
        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _jwtSettings.ValidateIssuer,
                ValidateAudience = _jwtSettings.ValidateAudience,
                ValidateLifetime = _jwtSettings.ValidateLifetime,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.FromSeconds(_jwtSettings.ClockSkewSeconds)
            };

            var principal = _tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
            return (true, principal);
        }
        catch (SecurityTokenException)
        {
            return (false, null);
        }
        catch (ArgumentException)
        {
            return (false, null);
        }
    }

    public bool ValidateRefreshToken(string token, Guid userId)
    {
        // 刷新令牌验证逻辑
        // 在实际应用中，这里应该检查刷新令牌是否在数据库或缓存中有效且未过期
        // 当前实现中，我们只进行基本验证
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // 刷新令牌是随机生成的，需要存储并与用户关联进行验证
        // 这里简化处理，实际应该查询数据库或缓存
        return true;
    }

    public Guid? ExtractUserIdFromToken(string token)
    {
        var validationResult = ValidateAccessToken(token);
        if (!validationResult.IsValid || validationResult.Principal == null)
            return null;

        return GetUserIdFromPrincipal(validationResult.Principal);
    }

    public Guid? GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }

    public string? RefreshAccessToken(string refreshToken, User user)
    {
        if (!ValidateRefreshToken(refreshToken, user.Id))
            return null;

        // 刷新令牌有效，生成新的访问令牌
        return GenerateAccessToken(user);
    }
}