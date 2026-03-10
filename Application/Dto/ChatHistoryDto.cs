using System;
using System.Collections.Generic;

namespace Application.ApplicationServiceContract;

public class ChatHistoryDto
{
    public Guid SessionId { get; set; }
    public string Title { get; set; } = null!;
    public IEnumerable<ChatMessageDto> Messages { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}