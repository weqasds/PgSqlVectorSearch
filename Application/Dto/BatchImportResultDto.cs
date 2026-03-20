using System.Collections.Generic;

namespace Application.Dto;

public class BatchImportResultDto
{
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public IEnumerable<ImportResult> Results { get; set; } = null!;
}

public class ImportResult
{
    public string FileName { get; set; } = null!;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? DocumentId { get; set; }
}