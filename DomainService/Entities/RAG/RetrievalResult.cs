using Pgvector;
using DomainService.Entities.KnowledgeManagement;

namespace DomainService.Entities.RAG;

public class RetrievalResult
{
    public KnowledgeChunk Chunk { get; set; } = null!;
    public double SimilarityScore { get; set; }
}