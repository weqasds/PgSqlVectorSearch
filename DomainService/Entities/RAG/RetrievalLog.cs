using System;

namespace DomainService.Entities.RAG;

public class RetrievalLog
{
    public Guid ChunkId { get; set; }
    public double Score { get; set; }
    public int Rank { get; set; }
    public DateTime RetrievedAt { get; set; }
}