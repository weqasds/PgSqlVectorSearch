using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract;

namespace DomainService.Services.KnowledgeManagement;

public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IKnowledgeChunkRepository _knowledgeChunkRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITaskQueueService _taskQueueService;

    public DocumentProcessingService(
        IKnowledgeBaseRepository knowledgeBaseRepository,
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IKnowledgeChunkRepository knowledgeChunkRepository,
        IFileStorageService fileStorageService,
        ITaskQueueService taskQueueService)
    {
        _knowledgeBaseRepository = knowledgeBaseRepository;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _knowledgeChunkRepository = knowledgeChunkRepository;
        _fileStorageService = fileStorageService;
        _taskQueueService = taskQueueService;
    }

    public async Task<KnowledgeDocument> UploadDocumentAsync(Guid knowledgeBaseId, Stream fileStream, string fileName)
    {
        // 验证知识库是否存在
        var knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {knowledgeBaseId} 的知识库");

        // 上传文件到存储服务
        var fileId = await _fileStorageService.UploadFileAsync(fileStream, fileName, "application/octet-stream");

        // 创建文档记录
        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            KnowledgeBaseId = knowledgeBaseId,
            FileName = fileName,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _knowledgeDocumentRepository.AddAsync(document);

        // 异步触发文档处理任务
        await _taskQueueService.EnqueueDocumentProcessingAsync(document.Id);

        return document;
    }

    public async Task ProcessDocumentAsync(Guid documentId)
    {
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {documentId} 的文档");

        // TODO: 实现文档处理逻辑，包括：
        // 1. 从文件存储下载文件
        // 2. 解析文档内容（支持PDF、Word、TXT等格式）
        // 3. 分块处理文本
        // 4. 为每个分块生成向量
        // 5. 保存分块和向量到数据库

        // 临时实现：创建示例分块
        var sampleChunk = new KnowledgeChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ChunkIndex = 0,
            Content = $"这是文档 '{document.FileName}' 的示例分块内容。",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _knowledgeChunkRepository.AddAsync(sampleChunk);

        // 异步触发向量生成任务
        await _taskQueueService.EnqueueVectorGenerationAsync(sampleChunk.Id);
    }

    public async Task<IEnumerable<KnowledgeChunk>> GetDocumentChunksAsync(Guid documentId)
    {
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {documentId} 的文档");

        var chunks = await _knowledgeChunkRepository.GetByDocumentIdAsync(documentId);
        var activeChunks = new List<KnowledgeChunk>();

        foreach (var chunk in chunks)
        {
            if (!chunk.IsDeleted)
                activeChunks.Add(chunk);
        }

        return activeChunks;
    }
}