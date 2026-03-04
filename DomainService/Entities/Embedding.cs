using Pgvector;
using System;
using System.Collections.Generic;
using System.Text;

namespace DomainService.Entities;

public class Embedding : Entity<Guid>
{
    public Guid ChunkId { get; set; }
    public KnowledgeChunk Chunk { get; set; } = null!;

    public Vector Vector { get; set; } = null!;
}