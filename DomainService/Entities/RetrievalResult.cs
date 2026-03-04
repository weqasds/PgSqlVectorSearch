using Pgvector;

namespace DomainService.Entities;

public class RetrievalResult
{
    public KnowledgeChunk Chunk { get; set; } = null!;
    public double SimilarityScore { get; set; }
}