using System;

namespace Application.ApplicationServiceContract;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}