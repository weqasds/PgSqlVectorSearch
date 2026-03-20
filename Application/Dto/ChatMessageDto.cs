using System;

namespace Application.Dto;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}