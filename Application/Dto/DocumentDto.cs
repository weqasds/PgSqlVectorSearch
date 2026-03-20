using System;
using Application.Extensions;
using DomainService.Entities.KnowledgeManagement;

namespace Application.Dto;

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
    public static DocumentDto ToDto(KnowledgeDocument document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            FileName = document.FileName,
            FilePath = document.FilePath,
            FileSize = document.FileSize,
            ContentType = document.ContentType.GetMimeType(),
            UploadedAt = document.CreatedAt,
            Status = document.Status.ToString(),
            ChunkCount = document.ChunkCount
        };
    }
}