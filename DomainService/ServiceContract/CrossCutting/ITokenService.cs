using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DomainService.Entities.UserManagement;

namespace DomainService.ServiceContract.CrossCutting;

/// <summary>
/// 令牌服务接口，负责JWT令牌的生成、验证和刷新
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// 为用户生成访问令牌
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>JWT访问令牌字符串</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// 为用户生成刷新令牌
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>刷新令牌字符串</returns>
    string GenerateRefreshToken(User user);

    /// <summary>
    /// 验证访问令牌
    /// </summary>
    /// <param name="token">访问令牌</param>
    /// <returns>验证结果，包含是否有效和声明主体</returns>
    (bool IsValid, ClaimsPrincipal? Principal) ValidateAccessToken(string token);

    /// <summary>
    /// 验证刷新令牌
    /// </summary>
    /// <param name="token">刷新令牌</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否有效</returns>
    bool ValidateRefreshToken(string token, Guid userId);

    /// <summary>
    /// 从令牌中提取用户ID
    /// </summary>
    /// <param name="token">访问令牌</param>
    /// <returns>用户ID，如果无效则返回null</returns>
    Guid? ExtractUserIdFromToken(string token);

    /// <summary>
    /// 从声明主体中提取用户ID
    /// </summary>
    /// <param name="principal">声明主体</param>
    /// <returns>用户ID，如果无效则返回null</returns>
    Guid? GetUserIdFromPrincipal(ClaimsPrincipal principal);

    /// <summary>
    /// 生成新的访问令牌（使用刷新令牌）
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="user">用户实体</param>
    /// <returns>新的访问令牌，如果刷新令牌无效则返回null</returns>
    string? RefreshAccessToken(string refreshToken, User user);
}