using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

    // 私有构造函数用于工厂方法和反序列化
    private User()
    {
        UserName = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    /// <summary>
    /// 创建用户的工厂方法
    /// </summary>
    public static User Create(string userName, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("邮箱不能为空", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("密码哈希不能为空", nameof(passwordHash));

        if (!IsValidUserName(userName))
            throw new ArgumentException("用户名格式无效", nameof(userName));
        if (!IsValidEmail(email))
            throw new ArgumentException("邮箱格式无效", nameof(email));

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName.Trim(),
            Email = email.Trim().ToLower(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        return user;
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    public void UpdateInfo(string userName, string email)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("邮箱不能为空", nameof(email));

        if (!IsValidUserName(userName))
            throw new ArgumentException("用户名格式无效", nameof(userName));
        if (!IsValidEmail(email))
            throw new ArgumentException("邮箱格式无效", nameof(email));

        UserName = userName.Trim();
        Email = email.Trim().ToLower();
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新密码哈希
    /// </summary>
    public void UpdatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("密码哈希不能为空", nameof(passwordHash));

        PasswordHash = passwordHash;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加对话会话
    /// </summary>
    public ChatSession AddChatSession(string chatTitle, Guid? knowledgeBaseId = null)
    {
        if (string.IsNullOrWhiteSpace(chatTitle))
            throw new ArgumentException("对话标题不能为空", nameof(chatTitle));

        var session = ChatSession.Create(Id, chatTitle, knowledgeBaseId);

        ChatSessions.Add(session);
        ModifiedAt = DateTime.UtcNow;

        return session;
    }

    /// <summary>
    /// 移除对话会话
    /// </summary>
    public void RemoveChatSession(Guid sessionId)
    {
        var session = ChatSessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            ChatSessions.Remove(session);
            ModifiedAt = DateTime.UtcNow;
        }
    }



    /// <summary>
    /// 添加创建的知识关系
    /// </summary>
    public void AddCreatedRelation(KnowledgeRelation relation)
    {
        if (relation == null)
            throw new ArgumentNullException(nameof(relation));

        CreatedRelations ??= new List<KnowledgeRelation>();
        CreatedRelations.Add(relation);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 验证用户名格式
    /// </summary>
    public static bool IsValidUserName(string userName)
    {
        return !string.IsNullOrWhiteSpace(userName) && userName.Length >= 3 && userName.Length <= 50;
    }

    /// <summary>
    /// 验证邮箱格式
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new global::System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取用户的对话会话数量
    /// </summary>
    public int GetChatSessionCount()
    {
        return ChatSessions.Count;
    }

    /// <summary>
    /// 获取用户可访问的知识库数量
    /// </summary>
    public int GetAccessibleKnowledgeBaseCount()
    {
        return AccessibleKnowledgeBases?.Count ?? 0;
    }
}
