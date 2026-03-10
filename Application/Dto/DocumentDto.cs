using System;

namespace Application.ApplicationServiceContract;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = null!;
    public DateTimeOffset? UploadedAt { get; set; }
    public string Status { get; set; } = null!; // e.g., "Pending", "Processing", "Processed", "Failed"
    public int ChunkCount { get; set; }
}