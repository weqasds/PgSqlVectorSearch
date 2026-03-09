using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;

namespace DomainService.Services.KnowledgeManagement;

public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITaskQueueService _taskQueueService;

    public DocumentProcessingService(
        IKnowledgeBaseRepository knowledgeBaseRepository,
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IFileStorageService fileStorageService,
        ITaskQueueService taskQueueService)
    {
        _knowledgeBaseRepository = knowledgeBaseRepository;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
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
        var document =  KnowledgeDocument.Create(
            knowledgeBaseId: knowledgeBaseId,
            fileName: fileName,
            filePath: fileId, // 存储服务返回的文件ID或路径
            fileSize: fileStream.Length,
            contentType: "application/octet-stream",
            fileHash: null // 可选：计算文件哈希值以防止重复上传
        );

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
        var sampleChunk = document.AddChunk($"这是文档 '{document.FileName}' 的示例分块内容。", 0);

        // 更新文档状态为已处理
        document.MarkAsProcessed(1); // 假设处理了1个块

        // 保存文档变更（包括新知识块）
        await _knowledgeDocumentRepository.UpdateAsync(document);

        // 异步触发向量生成任务
        await _taskQueueService.EnqueueVectorGenerationAsync(sampleChunk.Id);
    }

    public async Task<KnowledgeDocument?> FindDuplicateDocumentAsync(Guid knowledgeBaseId, string fileHash)
    {
        if (string.IsNullOrWhiteSpace(fileHash))
            throw new ArgumentException("文件哈希不能为空", nameof(fileHash));

        // 获取知识库中的所有文档
        var documents = await _knowledgeDocumentRepository.GetByKnowledgeBaseIdAsync(knowledgeBaseId);

        // 查找具有相同文件哈希且未删除的文档
        foreach (var document in documents)
        {
            if (!document.IsDeleted && document.FileHash == fileHash)
            {
                return document;
            }
        }

        return null;
    }

    public async Task<IEnumerable<KnowledgeChunk>> GetDocumentChunksAsync(Guid documentId)
    {
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {documentId} 的文档");

        // 通过聚合根文档获取知识块
        if (document.Chunks == null)
            return Enumerable.Empty<KnowledgeChunk>();

        var activeChunks = new List<KnowledgeChunk>();

        foreach (var chunk in document.Chunks)
        {
            if (!chunk.IsDeleted)
                activeChunks.Add(chunk);
        }

        return activeChunks;
    }
}