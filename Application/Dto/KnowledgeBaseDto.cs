using System;

namespace Application.ApplicationServiceContract;

public class KnowledgeBaseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public int DocumentCount { get; set; }
}