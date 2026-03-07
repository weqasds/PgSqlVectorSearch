using System;
using System.Collections.Generic;
using System.Text;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;

namespace DomainService.Entities.RAG;

public class RagRetrievalLog : Entity<Guid>
{
    public Guid RagGenerationId { get; set; }
    public RagGeneration RagGeneration { get; set; } = null!;

    public Guid ChunkId { get; set; }
    public KnowledgeChunk Chunk { get; set; } = null!;

    public float Score { get; set; }
}
