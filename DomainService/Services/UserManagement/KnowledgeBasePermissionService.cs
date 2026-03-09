using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;
using DomainService.Entities.UserManagement;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;

namespace DomainService.Services.UserManagement;

public class KnowledgeBasePermissionService : IKnowledgeBasePermissionService
{
    private readonly IUserRepository _userRepository;
    private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;

    public KnowledgeBasePermissionService(
        IUserRepository userRepository,
        IKnowledgeBaseRepository knowledgeBaseRepository)
    {
        _userRepository = userRepository;
        _knowledgeBaseRepository = knowledgeBaseRepository;
    }

    public async Task<UserKnowledgeBase> GrantAccessAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBaseAccessLevel accessLevel)
    {
        // 验证用户存在
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {userId} 的用户");

        // 验证知识库存在并加载权限关系
        var knowledgeBase = await _knowledgeBaseRepository.GetKnowledgeBaseWithUserAccessesAsync(knowledgeBaseId);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {knowledgeBaseId} 的知识库");

        // 使用KnowledgeBase聚合根的方法授予权限（作为权限管理的主体）
        knowledgeBase.GrantUserAccess(userId, accessLevel);

        // 保存知识库变更
        await _knowledgeBaseRepository.UpdateAsync(knowledgeBase);

        // 返回创建的权限记录
        var userAccess = knowledgeBase.UserAccesses?.FirstOrDefault(ua => ua.UserId == userId && ua.KnowledgeBaseId == knowledgeBaseId);
        if (userAccess == null)
        {
            throw new InvalidOperationException("授予权限失败，权限记录未创建");
        }

        return userAccess;
    }

    public async Task RevokeAccessAsync(Guid userId, Guid knowledgeBaseId)
    {
        // 验证用户存在
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {userId} 的用户");

        // 验证知识库存在并加载权限关系
        var knowledgeBase = await _knowledgeBaseRepository.GetKnowledgeBaseWithUserAccessesAsync(knowledgeBaseId);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {knowledgeBaseId} 的知识库");

        // 检查用户是否有权限记录（通过知识库检查）
        var hasAccess = knowledgeBase.HasUserAccess(userId, KnowledgeBaseAccessLevel.ReadOnly);
        if (!hasAccess)
            throw new InvalidOperationException($"用户 {userId} 对知识库 {knowledgeBaseId} 没有权限记录");

        // 使用KnowledgeBase聚合根的方法撤销权限（作为权限管理的主体）
        knowledgeBase.RevokeUserAccess(userId);

        // 保存知识库变更
        await _knowledgeBaseRepository.UpdateAsync(knowledgeBase);
    }

    public async Task<UserKnowledgeBase> UpdateAccessLevelAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBaseAccessLevel newAccessLevel)
    {
        // 验证用户存在
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {userId} 的用户");

        // 验证知识库存在并加载权限关系
        var knowledgeBase = await _knowledgeBaseRepository.GetKnowledgeBaseWithUserAccessesAsync(knowledgeBaseId);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {knowledgeBaseId} 的知识库");

        // 使用KnowledgeBase聚合根的方法更新权限级别（作为权限管理的主体）
        knowledgeBase.UpdateUserAccessLevel(userId, newAccessLevel);

        // 保存知识库变更
        await _knowledgeBaseRepository.UpdateAsync(knowledgeBase);

        // 获取更新后的权限记录
        var userAccess = knowledgeBase.UserAccesses?.FirstOrDefault(ua => ua.UserId == userId && ua.KnowledgeBaseId == knowledgeBaseId);
        if (userAccess == null)
        {
            throw new InvalidOperationException($"用户 {userId} 对知识库 {knowledgeBaseId} 没有权限记录");
        }

        return userAccess;
    }

    public async Task<bool> HasAccessAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBaseAccessLevel requiredLevel)
    {
        // 验证知识库存在
        var knowledgeBase = await _knowledgeBaseRepository.GetKnowledgeBaseWithUserAccessesAsync(knowledgeBaseId);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            return false;

        // 使用KnowledgeBase聚合根的方法检查用户权限
        return knowledgeBase.HasUserAccess(userId, requiredLevel);
    }

    public async Task<KnowledgeBaseAccessLevel?> GetAccessLevelAsync(Guid userId, Guid knowledgeBaseId)
    {
        // 验证知识库存在
        var knowledgeBase = await _knowledgeBaseRepository.GetKnowledgeBaseWithUserAccessesAsync(knowledgeBaseId);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            return null;

        // 使用KnowledgeBase聚合根的方法获取用户权限级别
        return knowledgeBase.GetUserAccessLevel(userId);
    }

    public async Task<IEnumerable<KnowledgeBase>> GetAccessibleKnowledgeBasesAsync(Guid userId)
    {
        // 获取用户，加载权限关系
        //var user = await _userRepository.GetUserWithAccessibleKnowledgeBasesAsync(userId);
        var user= await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            throw new KeyNotFoundException($"未找到ID为 {userId} 的用户");
        }

        // 获取所有知识库
        var allKnowledgeBases = await _knowledgeBaseRepository.GetAllAsync();
        var accessibleBases = new List<KnowledgeBase>();

        // 获取用户显式授权的知识库ID集合
        var explicitAccessKnowledgeBaseIds = user.AccessibleKnowledgeBases?
            .Where(ua => !ua.IsDeleted)
            .Select(ua => ua.KnowledgeBaseId)
            .ToHashSet() ?? new HashSet<Guid>();

        foreach (var knowledgeBase in allKnowledgeBases)
        {
            if (knowledgeBase.IsDeleted)
                continue;

            // 检查是否为公共知识库
            if (knowledgeBase.IsPublic)
            {
                accessibleBases.Add(knowledgeBase);
                continue;
            }

            // 检查用户是否是所有者
            if (knowledgeBase.OwnerId == userId)
            {
                accessibleBases.Add(knowledgeBase);
                continue;
            }

            // 检查是否有显式授权
            if (explicitAccessKnowledgeBaseIds.Contains(knowledgeBase.Id))
            {
                accessibleBases.Add(knowledgeBase);
            }
        }

        return accessibleBases;
    }

    public async Task<IEnumerable<User>> GetUsersWithAccessAsync(Guid knowledgeBaseId, KnowledgeBaseAccessLevel? minimumLevel = null)
    {
        // 验证知识库存在并加载权限关系
        var knowledgeBase = await _knowledgeBaseRepository.GetKnowledgeBaseWithUserAccessesAsync(knowledgeBaseId);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {knowledgeBaseId} 的知识库");

        var usersWithAccess = new List<User>();

        // 添加所有者（如果有）
        if (knowledgeBase.OwnerId.HasValue)
        {
            var owner = await _userRepository.GetByIdAsync(knowledgeBase.OwnerId.Value);
            if (owner != null && !owner.IsDeleted)
            {
                usersWithAccess.Add(owner);
            }
        }

        // 获取知识库的所有权限记录
        var userAccesses = knowledgeBase.UserAccesses;
        if (userAccesses != null)
        {
            foreach (var access in userAccesses)
            {
                if (access.IsDeleted)
                    continue;

                // 如果指定了最低权限级别，检查是否满足
                if (minimumLevel.HasValue && access.AccessLevel < minimumLevel.Value)
                    continue;

                var user = await _userRepository.GetByIdAsync(access.UserId);
                if (user != null && !user.IsDeleted && !usersWithAccess.Any(u => u.Id == user.Id))
                {
                    usersWithAccess.Add(user);
                }
            }
        }

        return usersWithAccess;
    }
}