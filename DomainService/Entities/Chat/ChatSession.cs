using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

    // 私有构造函数用于工厂方法和反序列化
    private ChatSession() { }

    /// <summary>
    /// 创建对话会话的工厂方法
    /// </summary>
    public static ChatSession Create(Guid userId, string chatTitle, Guid? knowledgeBaseId = null)
    {
        if (string.IsNullOrWhiteSpace(chatTitle))
            throw new ArgumentException("对话标题不能为空", nameof(chatTitle));

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ChatTitle = chatTitle.Trim(),
            KnowledgeBaseId = knowledgeBaseId,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Messages = new List<ChatMessage>()
        };

        return session;
    }

    /// <summary>
    /// 更新对话标题
    /// </summary>
    public void UpdateTitle(string chatTitle)
    {
        if (string.IsNullOrWhiteSpace(chatTitle))
            throw new ArgumentException("对话标题不能为空", nameof(chatTitle));

        ChatTitle = chatTitle.Trim();
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加消息到会话
    /// </summary>
    public ChatMessage AddMessage(string content, ChatRole role)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("消息内容不能为空", nameof(content));

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = this.Id,
            Content = content.Trim(),
            Role = role,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        Messages.Add(message);
        ModifiedAt = DateTime.UtcNow;

        return message;
    }

    /// <summary>
    /// 移除消息
    /// </summary>
    public void RemoveMessage(Guid messageId)
    {
        var message = Messages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            Messages.Remove(message);
            ModifiedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 设置主要使用的知识库
    /// </summary>
    public void SetKnowledgeBase(Guid? knowledgeBaseId)
    {
        KnowledgeBaseId = knowledgeBaseId;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加引用的知识库ID
    /// </summary>
    public void AddReferencedKnowledgeBase(Guid knowledgeBaseId)
    {
        var referencedIds = GetReferencedKnowledgeBaseIds();
        if (!referencedIds.Contains(knowledgeBaseId))
        {
            referencedIds.Add(knowledgeBaseId);
            ReferencedKnowledgeBaseIds = JsonSerializer.Serialize(referencedIds);
            ModifiedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 移除引用的知识库ID
    /// </summary>
    public void RemoveReferencedKnowledgeBase(Guid knowledgeBaseId)
    {
        var referencedIds = GetReferencedKnowledgeBaseIds();
        if (referencedIds.Contains(knowledgeBaseId))
        {
            referencedIds.Remove(knowledgeBaseId);
            ReferencedKnowledgeBaseIds = referencedIds.Count > 0 ? JsonSerializer.Serialize(referencedIds) : null;
            ModifiedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 获取引用的知识库ID列表
    /// </summary>
    public List<Guid> GetReferencedKnowledgeBaseIds()
    {
        if (string.IsNullOrEmpty(ReferencedKnowledgeBaseIds))
            return new List<Guid>();

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(ReferencedKnowledgeBaseIds) ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    /// <summary>
    /// 获取消息数量
    /// </summary>
    public int GetMessageCount()
    {
        return Messages.Count;
    }

    /// <summary>
    /// 获取最新消息时间
    /// </summary>
    public DateTime? GetLatestMessageTime()
    {
        return Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.CreatedAt;
    }

    /// <summary>
    /// 验证对话标题是否有效
    /// </summary>
    public static bool IsValidTitle(string title)
    {
        return !string.IsNullOrWhiteSpace(title) && title.Length <= 200;
    }

    /// <summary>
    /// 检查会话是否属于指定用户
    /// </summary>
    public bool BelongsToUser(Guid userId)
    {
        return UserId == userId;
    }

    /// <summary>
    /// 获取用户消息列表
    /// </summary>
    public IEnumerable<ChatMessage> GetUserMessages()
    {
        return Messages.Where(m => m.Role == ChatRole.User);
    }

    /// <summary>
    /// 获取助手消息列表
    /// </summary>
    public IEnumerable<ChatMessage> GetAssistantMessages()
    {
        return Messages.Where(m => m.Role == ChatRole.Assistant);
    }
}
