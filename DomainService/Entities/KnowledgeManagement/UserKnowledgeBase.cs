using System;
using System.Text;
using DomainService.Entities.Core;
using DomainService.Entities.UserManagement;

namespace DomainService.Entities.KnowledgeManagement;

/// <summary>
/// 用户与知识库的访问权限关联实体
/// 实现用户与知识库的多对多关系
/// </summary>
public class UserKnowledgeBase : Entity<Guid>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 知识库ID
    /// </summary>
    public Guid KnowledgeBaseId { get; set; }

    /// <summary>
    /// 访问权限级别
    /// </summary>
    public KnowledgeBaseAccessLevel AccessLevel { get; set; }

    /// <summary>
    /// 用户实体
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// 知识库实体
    /// </summary>
    public KnowledgeBase KnowledgeBase { get; set; } = null!;
}

/// <summary>
/// 知识库访问权限级别枚举
/// </summary>
public enum KnowledgeBaseAccessLevel
{
    /// <summary>
    /// 只读权限
    /// </summary>
    ReadOnly = 0,

    /// <summary>
    /// 读写权限
    /// </summary>
    ReadWrite = 1,

    /// <summary>
    /// 管理权限（可以管理用户权限）
    /// </summary>
    Admin = 2
}