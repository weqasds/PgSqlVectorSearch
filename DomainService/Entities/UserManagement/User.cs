using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using DomainService.Entities.Core;
using DomainService.Entities.Chat;
using DomainService.Entities.KnowledgeGraph;
using DomainService.Entities.KnowledgeManagement;

namespace DomainService.Entities.UserManagement;

public class User : Entity<Guid>
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    /// <summary>
    /// 创建的知识关系
    /// </summary>
    public virtual ICollection<KnowledgeRelation>? CreatedRelations { get; set; }

    /// <summary>
    /// 用户可访问的知识库权限关系
    /// </summary>
    public virtual ICollection<UserKnowledgeBase>? AccessibleKnowledgeBases { get; set; }
}
