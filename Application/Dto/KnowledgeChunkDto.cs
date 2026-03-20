using System;

namespace Application.ApplicationServiceContract;

public class KnowledgeChunkDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = null!;
    public Guid DocumentId { get; set; }
    public int Index { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int VectorDimension { get; set; }
}