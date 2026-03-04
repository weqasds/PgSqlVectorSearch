using System;
using System.Collections.Generic;
using System.Text;

namespace DomainService.Entities;

public class RagRetrievalLog : Entity<Guid>
{
    public Guid RagGenerationId { get; set; }
    public RagGeneration RagGeneration { get; set; } = null!;

    public Guid ChunkId { get; set; }
    public KnowledgeChunk Chunk { get; set; } = null!;

    public float Score { get; set; }
}
