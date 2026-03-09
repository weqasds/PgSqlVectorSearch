using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeGraph;

namespace DomainService.Entities.KnowledgeManagement;

/// <summary>
/// RAG最小单位
/// </summary>
public class KnowledgeChunk : Entity<Guid>
{
    public Guid DocumentId { get; set; }
    /// <summary>
    /// 归属的文档
    /// </summary>
    public KnowledgeDocument Document { get; set; } = null!;

    public int ChunkIndex { get; set; }
    public string Content { get; set; } = null!;

    /// <summary>
    /// 作为源的知识关系
    /// </summary>
    public virtual ICollection<KnowledgeRelation>? SourceRelations { get; set; }

    /// <summary>
    /// 作为目标的知识关系
    /// </summary>
    public virtual ICollection<KnowledgeRelation>? TargetRelations { get; set; }
}
