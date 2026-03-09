using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.KnowledgeManagement;
using DomainService.Entities.UserManagement;

namespace DomainService.ServiceContract.Services;

/// <summary>
/// 用户-知识库权限管理服务
/// 负责管理用户对知识库的访问权限
/// </summary>
public interface IKnowledgeBasePermissionService
{
    /// <summary>
    /// 授予用户对知识库的访问权限
    /// </summary>
    Task<UserKnowledgeBase> GrantAccessAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBaseAccessLevel accessLevel);

    /// <summary>
    /// 撤销用户对知识库的访问权限
    /// </summary>
    Task RevokeAccessAsync(Guid userId, Guid knowledgeBaseId);

    /// <summary>
    /// 更新用户对知识库的访问权限级别
    /// </summary>
    Task<UserKnowledgeBase> UpdateAccessLevelAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBaseAccessLevel newAccessLevel);

    /// <summary>
    /// 检查用户是否有权限访问知识库
    /// </summary>
    Task<bool> HasAccessAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBaseAccessLevel requiredLevel);

    /// <summary>
    /// 获取用户对知识库的访问权限级别
    /// </summary>
    Task<KnowledgeBaseAccessLevel?> GetAccessLevelAsync(Guid userId, Guid knowledgeBaseId);

    /// <summary>
    /// 获取用户有权限访问的所有知识库
    /// </summary>
    Task<IEnumerable<KnowledgeBase>> GetAccessibleKnowledgeBasesAsync(Guid userId);

    /// <summary>
    /// 获取有权限访问指定知识库的所有用户
    /// </summary>
    Task<IEnumerable<User>> GetUsersWithAccessAsync(Guid knowledgeBaseId, KnowledgeBaseAccessLevel? minimumLevel = null);
}