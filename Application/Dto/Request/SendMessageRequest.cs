using System;

namespace ApplicationService.Dto.Request;

public class SendMessageRequest
{
    public string Message { get; set; } = null!;
    public Guid? KnowledgeBaseId { get; set; }
}