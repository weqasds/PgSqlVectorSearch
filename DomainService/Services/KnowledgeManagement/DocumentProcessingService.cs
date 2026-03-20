using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DomainService.Entities.KnowledgeManagement;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;
namespace DomainService.Services.KnowledgeManagement;

public class DocumentService : IDocumentService
{
    private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly ITaskQueueService _taskQueueService;

    public DocumentService(
        IKnowledgeBaseRepository knowledgeBaseRepository,
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        ITaskQueueService taskQueueService)
    {
        _knowledgeBaseRepository = knowledgeBaseRepository;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _taskQueueService = taskQueueService;
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