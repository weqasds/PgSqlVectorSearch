using System;
using System.Collections.Generic;
using System.Text;

namespace DomainService.Entities;

public class KnowledgeDocument : Entity<Guid>
{
    public Guid KnowledgeBaseId { get; set; }
    public KnowledgeBase KnowledgeBase { get; set; } = null!;

    public string FileName { get; set; } = null!;
}