using System;
using System.Collections.Generic;
using System.Text;

namespace DomainService.Entities;

public class RagGeneration : Entity<Guid>
{
    public Guid ChatSessionId { get; set; }
    public ChatSession ChatSession { get; set; } = null!;

    public string Prompt { get; set; } = null!;
    public string Answer { get; set; } = null!;
}
