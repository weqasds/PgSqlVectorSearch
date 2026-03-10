using System;

namespace Application.ApplicationServiceContract;

public class ChatSessionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public int MessageCount { get; set; }
}