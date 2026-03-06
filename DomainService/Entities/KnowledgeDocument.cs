using System;
using System.Collections.Generic;
using System.Text;

namespace DomainService.Entities;
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