using System;
using System.Collections.Generic;
using System.Text;
using DomainService.Entities.Core;

namespace DomainService.Entities.Chat;
/// <summary>
/// 对话消息，包含用户输入和系统回复
/// </summary>
public class ChatMessage : Entity<Guid>
{
    public Guid ChatSessionId { get; set; }
    public ChatSession ChatSession { get; set; } = null!;
    public ChatRole Role { get; set; }
    /// <summary>
    /// ai回复的内容或者用户输入的内容，取决于Role
    /// </summary>
    public string Content { get; set; } = null!;

}
public enum ChatRole
{
    System,
    User,
    Assistant
}
