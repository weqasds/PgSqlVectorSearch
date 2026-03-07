using System;
using System.Collections.Generic;
using System.Text;
using DomainService.Entities.Core;
using DomainService.Entities.UserManagement;
using DomainService.Entities.KnowledgeManagement;

namespace DomainService.Entities.Chat;
/// <summary>
/// 用户的一次历史对话
/// </summary>
public class ChatSession : Entity<Guid>
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string ChatTitle { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    /// <summary>
    /// 主要使用的知识库ID（可为空）
    /// </summary>
    public Guid? KnowledgeBaseId { get; set; }

    /// <summary>
    /// 主要使用的知识库
    /// </summary>
    public KnowledgeBase? KnowledgeBase { get; set; }

    /// <summary>
    /// 引用的其他知识库ID列表（JSON数组格式）
    /// 用于记录对话中实际引用的所有知识库
    /// </summary>
    public string? ReferencedKnowledgeBaseIds { get; set; }
}
