using System.Collections.Generic;

namespace Application.Dto;

public class BatchProcessResultDto
{
    public int TotalDocuments { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public IEnumerable<ProcessResult> Results { get; set; } = null!;
}

public class ProcessResult
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = null!;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ChunksGenerated { get; set; }
}