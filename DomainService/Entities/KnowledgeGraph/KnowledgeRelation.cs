using System;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;
using DomainService.Entities.Enums;
using DomainService.Entities.UserManagement;

namespace DomainService.Entities.KnowledgeGraph;

/// <summary>
/// 知识关系实体
/// </summary>
public class KnowledgeRelation : Entity<Guid>
{
    /// <summary>
    /// 源知识块ID
    /// </summary>
    public Guid SourceChunkId { get; set; }

    /// <summary>
    /// 源知识块
    /// </summary>
    public KnowledgeChunk SourceChunk { get; set; } = null!;

    /// <summary>
    /// 目标知识块ID
    /// </summary>
    public Guid TargetChunkId { get; set; }

    /// <summary>
    /// 目标知识块
    /// </summary>
    public KnowledgeChunk TargetChunk { get; set; } = null!;

    /// <summary>
    /// 关系类型
    /// </summary>
    public RelationType RelationType { get; set; }

    /// <summary>
    /// 关系强度 (0-1)
    /// </summary>
    public float Strength { get; set; }

    /// <summary>
    /// 关系描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 创建者用户ID（可选）
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 创建者用户
    /// </summary>
    public User? Creator { get; set; }
}