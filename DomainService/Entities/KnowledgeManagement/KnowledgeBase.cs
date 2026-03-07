using System;
using System.Collections.Generic;
using System.Text;
using DomainService.Entities.Core;
using DomainService.Entities.UserManagement;
using DomainService.Entities.Chat;

namespace DomainService.Entities.KnowledgeManagement;

public class KnowledgeBase : Entity<Guid>
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    /// <summary>
    /// 所有者用户ID，如果为null则表示系统知识库
    /// </summary>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// 所有者用户
    /// </summary>
    public User? Owner { get; set; }

    /// <summary>
    /// 是否为公共知识库，所有用户可见
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 用户访问权限关系
    /// </summary>
    public virtual ICollection<UserKnowledgeBase>? UserAccesses { get; set; }

    /// <summary>
    /// 使用该知识库的对话会话（一对多关系）
    /// </summary>
    public virtual ICollection<ChatSession>? ChatSessions { get; set; }
}
