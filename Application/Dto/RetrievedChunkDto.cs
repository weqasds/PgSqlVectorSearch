namespace Application.Dto;

public class RetrievedChunkDto
{
    public string Content { get; set; } = null!;
    public string SourceDocument { get; set; } = null!;
    public double SimilarityScore { get; set; }
}