using System;

namespace DomainService.ServiceContract.CrossCutting;

/// <summary>
/// 当前用户服务接口，用于获取当前认证用户的信息
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>当前用户ID，如果未认证则返回null</returns>
    Guid? GetCurrentUserId();

    /// <summary>
    /// 获取当前用户名
    /// </summary>
    /// <returns>当前用户名，如果未认证则返回null</returns>
    string? GetCurrentUserName();

    /// <summary>
    /// 获取当前用户邮箱
    /// </summary>
    /// <returns>当前用户邮箱，如果未认证则返回null</returns>
    string? GetCurrentUserEmail();

    /// <summary>
    /// 检查当前用户是否已认证
    /// </summary>
    /// <returns>如果用户已认证则返回true</returns>
    bool IsAuthenticated();

    /// <summary>
    /// 检查当前用户是否具有指定角色
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <returns>如果用户具有指定角色则返回true</returns>
    bool IsInRole(string role);

    /// <summary>
    /// 检查当前用户是否具有指定声明
    /// </summary>
    /// <param name="claimType">声明类型</param>
    /// <param name="claimValue">声明值</param>
    /// <returns>如果用户具有指定声明则返回true</returns>
    bool HasClaim(string claimType, string claimValue);
}