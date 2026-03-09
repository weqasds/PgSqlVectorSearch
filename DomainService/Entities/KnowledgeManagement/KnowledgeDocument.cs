using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DomainService.Entities.Core;

namespace DomainService.Entities.KnowledgeManagement;
/// <summary>
/// 知识文档实体，表示上传的文件元信息
/// </summary>
public class KnowledgeDocument : Entity<Guid>
{
    /// <summary>
    /// 外键，关联到知识库
    /// </summary>
    public Guid KnowledgeBaseId { get; set; }
    public KnowledgeBase KnowledgeBase { get; set; } = null!;
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = null!;
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = null!;
    /// <summary>
    /// 文件大小，单位字节
    /// </summary>
    public long FileSize { get; set; } 
    /// <summary>
    /// MIME类型，如application/pdf
    /// </summary>
    public string ContentType { get; set; } = null!;
    /// <summary>
    /// 状态，如待处理、处理中、已处理、处理失败
    /// </summary>
    public DocumentStatus Status { get; set; }
    /// <summary>
    /// 文档被切分成的块数量
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// 文件哈希值，用于防止重复上传相同文件
    /// </summary>
    public string? FileHash { get; set; }

    /// <summary>
    /// 文档的知识块集合
    /// </summary>
    public virtual ICollection<KnowledgeChunk>? Chunks { get; set; }

    // 私有构造函数用于工厂方法和反序列化
    private KnowledgeDocument() { }

    /// <summary>
    /// 创建知识文档的工厂方法
    /// </summary>
    public static KnowledgeDocument Create(
        Guid knowledgeBaseId, 
        string fileName, 
        string filePath, 
        long fileSize, 
        string contentType, 
        string? fileHash = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        if (fileSize <= 0)
            throw new ArgumentException("文件大小必须大于0", nameof(fileSize));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("内容类型不能为空", nameof(contentType));

        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            KnowledgeBaseId = knowledgeBaseId,
            FileName = fileName.Trim(),
            FilePath = filePath.Trim(),
            FileSize = fileSize,
            ContentType = contentType.Trim(),
            Status = DocumentStatus.Pending,
            ChunkCount = 0,
            FileHash = fileHash,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        return document;
    }

    /// <summary>
    /// 更新文档处理状态
    /// </summary>
    public void UpdateStatus(DocumentStatus status)
    {
        Status = status;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加知识块到文档
    /// </summary>
    public KnowledgeChunk AddChunk(string content, int sequenceNumber, string? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("知识块内容不能为空", nameof(content));
        if (sequenceNumber < 0)
            throw new ArgumentException("序列号不能为负数", nameof(sequenceNumber));

        var chunk = new KnowledgeChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = this.Id,
            Content = content.Trim(),
            ChunkIndex = sequenceNumber,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        Chunks ??= new List<KnowledgeChunk>();
        Chunks.Add(chunk);
        ChunkCount++;
        ModifiedAt = DateTime.UtcNow;

        return chunk;
    }

    /// <summary>
    /// 移除知识块
    /// </summary>
    public void RemoveChunk(Guid chunkId)
    {
        var chunk = Chunks?.FirstOrDefault(c => c.Id == chunkId);
        if (chunk == null)
            return;

        Chunks?.Remove(chunk);
        ChunkCount = Math.Max(0, ChunkCount - 1);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置文档处理完成（包括块数）
    /// </summary>
    public void MarkAsProcessed(int chunkCount)
    {
        if (chunkCount < 0)
            throw new ArgumentException("块数不能为负数", nameof(chunkCount));

        Status = DocumentStatus.Processed;
        ChunkCount = chunkCount;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 标记文档处理失败
    /// </summary>
    public void MarkAsFailed()
    {
        Status = DocumentStatus.Failed;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 验证文件名是否有效
    /// </summary>
    public static bool IsValidFileName(string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName) && 
            fileName.Length <= 500 && 
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;
    }

    /// <summary>
    /// 验证文件大小是否在合理范围内（最大1GB）
    /// </summary>
    public static bool IsValidFileSize(long fileSize)
    {
        return fileSize > 0 && fileSize <= 1024L * 1024 * 1024; // 1GB
    }
}
public enum DocumentStatus
{
    /// <summary>
    /// 待处理
    /// </summary>
    Pending = 0,
    /// <summary>
    /// 处理中
    /// </summary>
    Processing = 1,
    /// <summary>
    /// 已处理
    /// </summary>
    Processed = 2,
    /// <summary>
    /// 处理失败
    /// </summary>
    Failed = 3
} 