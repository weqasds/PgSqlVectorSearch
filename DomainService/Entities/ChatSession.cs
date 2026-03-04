using System;
using System.Collections.Generic;
using System.Text;

namespace DomainService.Entities;
/// <summary>
/// 用户的一次历史对话
/// </summary>
public class ChatSession : Entity<Guid>
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string ChatTitle { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

}
