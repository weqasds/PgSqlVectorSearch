using System;
using System.Collections.Generic;
using System.Text;

namespace DomainService.Entities;

public class KnowledgeBase : Entity<Guid>
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
}
