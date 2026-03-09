using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// 知识库中的文档集合
    /// </summary>
    public virtual ICollection<KnowledgeDocument>? Documents { get; set; }

    // 私有构造函数用于工厂方法和反序列化
    private KnowledgeBase() { }

    /// <summary>
    /// 创建知识库的工厂方法
    /// </summary>
    public static KnowledgeBase Create(string name, string description, Guid? ownerId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("知识库名称不能为空", nameof(name));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("知识库描述不能为空", nameof(description));

        var knowledgeBase = new KnowledgeBase
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description.Trim(),
            OwnerId = ownerId,
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        return knowledgeBase;
    }

    /// <summary>
    /// 添加文档到知识库
    /// </summary>
    public KnowledgeDocument AddDocument(string fileName, string filePath, long fileSize, string contentType, string? fileHash = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        if (fileSize <= 0)
            throw new ArgumentException("文件大小必须大于0", nameof(fileSize));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("内容类型不能为空", nameof(contentType));

        var document = KnowledgeDocument.Create(this.Id, fileName, filePath, fileSize, contentType, fileHash);

        Documents ??= new List<KnowledgeDocument>();
        Documents.Add(document);

        ModifiedAt = DateTime.UtcNow;

        return document;
    }

    /// <summary>
    /// 移除文档
    /// </summary>
    public void RemoveDocument(Guid documentId)
    {
        var document = Documents?.FirstOrDefault(d => d.Id == documentId);
        if (document == null)
            throw new InvalidOperationException($"文档 {documentId} 不存在于知识库中");

        Documents?.Remove(document);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置知识库为公共或私有
    /// </summary>
    public void SetPublic(bool isPublic)
    {
        IsPublic = isPublic;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新知识库信息
    /// </summary>
    public void UpdateInfo(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("知识库名称不能为空", nameof(name));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("知识库描述不能为空", nameof(description));

        Name = name.Trim();
        Description = description.Trim();
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 验证知识库名称是否有效
    /// </summary>
    public static bool IsValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Length <= 200;
    }

    /// <summary>
    /// 授予用户访问权限
    /// </summary>
    public void GrantUserAccess(Guid userId, KnowledgeBaseAccessLevel accessLevel = KnowledgeBaseAccessLevel.ReadOnly)
    {
        UserAccesses ??= new List<UserKnowledgeBase>();

        var existingAccess = UserAccesses.FirstOrDefault(ua => ua.UserId == userId);
        if (existingAccess == null)
        {
            var userAccess = new UserKnowledgeBase
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                KnowledgeBaseId = this.Id,
                AccessLevel = accessLevel,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            UserAccesses.Add(userAccess);
        }
        else
        {
            // 更新现有记录的权限级别
            existingAccess.AccessLevel = accessLevel;
            existingAccess.ModifiedAt = DateTime.UtcNow;
        }
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 撤销用户访问权限
    /// </summary>
    public void RevokeUserAccess(Guid userId)
    {
        var userAccess = UserAccesses?.FirstOrDefault(ua => ua.UserId == userId);
        if (userAccess != null)
        {
            UserAccesses?.Remove(userAccess);
            ModifiedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 检查用户是否有访问权限
    /// </summary>
    public bool HasUserAccess(Guid userId)
    {
        return IsPublic || OwnerId == userId || UserAccesses?.Any(ua => ua.UserId == userId) == true;
    }

    /// <summary>
    /// 检查用户是否有特定级别的访问权限
    /// </summary>
    public bool HasUserAccess(Guid userId, KnowledgeBaseAccessLevel requiredLevel)
    {
        // 如果是公共知识库且只需要只读权限
        if (IsPublic && requiredLevel == KnowledgeBaseAccessLevel.ReadOnly)
            return true;

        // 如果是所有者，拥有所有权限
        if (OwnerId == userId)
            return true;

        // 检查用户权限记录
        var userAccess = UserAccesses?.FirstOrDefault(ua => ua.UserId == userId);
        return userAccess != null && userAccess.AccessLevel >= requiredLevel;
    }

    /// <summary>
    /// 更新用户的访问权限级别
    /// </summary>
    public void UpdateUserAccessLevel(Guid userId, KnowledgeBaseAccessLevel accessLevel)
    {
        UserAccesses ??= new List<UserKnowledgeBase>();

        var existingAccess = UserAccesses.FirstOrDefault(ua => ua.UserId == userId);
        if (existingAccess == null)
        {
            throw new InvalidOperationException($"用户 {userId} 对知识库 {Id} 没有访问权限记录");
        }

        existingAccess.AccessLevel = accessLevel;
        existingAccess.ModifiedAt = DateTime.UtcNow;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取用户对知识库的访问权限级别
    /// </summary>
    public KnowledgeBaseAccessLevel? GetUserAccessLevel(Guid userId)
    {
        // 如果是所有者，拥有管理员权限
        if (OwnerId == userId)
            return KnowledgeBaseAccessLevel.Admin;

        // 如果是公共知识库，返回只读权限
        if (IsPublic)
            return KnowledgeBaseAccessLevel.ReadOnly;

        // 检查用户权限记录
        var userAccess = UserAccesses?.FirstOrDefault(ua => ua.UserId == userId);
        return userAccess?.AccessLevel;
    }

    /// <summary>
    /// 获取文档数量
    /// </summary>
    public int GetDocumentCount()
    {
        return Documents?.Count ?? 0;
    }

    /// <summary>
    /// 获取总块数（所有文档的块数之和）
    /// </summary>
    public int GetTotalChunkCount()
    {
        return Documents?.Sum(d => d.ChunkCount) ?? 0;
    }
}
