using System;

namespace Application.ApplicationServiceContract;

public class SendMessageRequest
{
    public string Message { get; set; } = null!;
    public Guid? KnowledgeBaseId { get; set; }
}