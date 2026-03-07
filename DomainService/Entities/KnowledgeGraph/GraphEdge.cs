using System;
using DomainService.Entities.Core;

namespace DomainService.Entities.KnowledgeGraph;

/// <summary>
/// 图边实体
/// </summary>
public class GraphEdge : Entity<Guid>
{
    /// <summary>
    /// 源节点ID
    /// </summary>
    public Guid SourceNodeId { get; set; }

    /// <summary>
    /// 源节点
    /// </summary>
    public GraphNode SourceNode { get; set; } = null!;

    /// <summary>
    /// 目标节点ID
    /// </summary>
    public Guid TargetNodeId { get; set; }

    /// <summary>
    /// 目标节点
    /// </summary>
    public GraphNode TargetNode { get; set; } = null!;

    /// <summary>
    /// 关联的知识关系ID（可选）
    /// </summary>
    public Guid? RelationId { get; set; }

    /// <summary>
    /// 关联的知识关系
    /// </summary>
    public KnowledgeRelation? Relation { get; set; }

    /// <summary>
    /// 可视化属性（JSON格式）
    /// </summary>
    public string VisualProperties { get; set; } = string.Empty;

    /// <summary>
    /// 边标签
    /// </summary>
    public string Label { get; set; } = string.Empty;
}