using System;

namespace DomainService.Entities;

/// <summary>
/// 图节点实体
/// </summary>
public class GraphNode : Entity<Guid>
{
    /// <summary>
    /// 关联的实体ID
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// 实体类型
    /// </summary>
    public EntityType EntityType { get; set; }

    /// <summary>
    /// 可视化位置X坐标
    /// </summary>
    public float PositionX { get; set; }

    /// <summary>
    /// 可视化位置Y坐标
    /// </summary>
    public float PositionY { get; set; }

    /// <summary>
    /// 节点标签（可选，默认使用实体名称）
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 扩展属性（JSON格式）
    /// </summary>
    public string Properties { get; set; } = string.Empty;
}