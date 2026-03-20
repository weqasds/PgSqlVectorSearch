using System;
using System.Collections.Generic;

namespace Application.Dto;

public class ChatResponseDto
{
    public string Answer { get; set; } = null!;
    public IEnumerable<RetrievedChunkDto> RetrievedChunks { get; set; } = null!;
    public Guid? SessionId { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}