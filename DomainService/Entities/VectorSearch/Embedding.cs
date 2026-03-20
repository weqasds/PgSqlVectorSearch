using Pgvector;
using System;
using System.Collections.Generic;
using System.Text;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;

namespace DomainService.Entities.VectorSearch;

public class Embedding
{
    public Guid ChunkId { get; set; }
    public KnowledgeChunk Chunk { get; set; } = null!;

    public Vector Vector { get; set; } = null!;
}