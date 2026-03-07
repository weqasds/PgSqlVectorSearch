using System;
using System.Collections.Generic;

namespace DomainService.Entities.RAG;

public class RagResponse
{
    public string Answer { get; set; } = null!;
    public IEnumerable<RetrievalResult> RetrievedChunks { get; set; } = null!;
    public Guid? SessionId { get; set; }
    public DateTime GeneratedAt { get; set; }
}