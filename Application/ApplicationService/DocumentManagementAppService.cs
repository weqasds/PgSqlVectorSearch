using DomainService.Entities.KnowledgeManagement;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using Application.ApplicationServiceContract.DocumentationService;
using Application.QueryServices;
using Application.ServiceContract.CrossCutting;
using Application.ApplicationServiceContract.CrossCutting;
using Application.ApplicationServiceContract.AI;
using DomainService.ServiceContract.CrossCutting;
using Application.Dto;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.Models.DocumentParsing;
using Application.ApplicationService.DocumentParsing;
using DomainService.Entities.VectorSearch;
using Pgvector;
namespace Application.ApplicationService;

public class DocumentManagementAppService : IDocumentManagementAppService
{
    private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly IDocumentService _documentService;
    private readonly IDocumentParserSelector _documentParserSelector;
    private readonly IDocumentParseStrategyResolver _documentParseStrategyResolver;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IKnowledgeChunkQueryService _knowledgeChunkService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    //后台任务服务
    private readonly ITaskQueueService _taskQueueService;
    private readonly IVectorStore _vectorStore;
    private readonly DocumentParseExecutor _documentParseExecutor;
    public DocumentManagementAppService(
        IKnowledgeBaseRepository knowledgeBaseRepository,
        ISemanticKernelService semanticKernelService,
        IKnowledgeChunkQueryService knowledgeChunkService,
        IDocumentService documentService,
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        ITaskQueueService taskQueueService,
        IDocumentParserSelector documentParserSelector,
        IDocumentParseStrategyResolver documentParseStrategyResolver,
        IVectorStore vectorStore,
        DocumentParseExecutor documentParseExecutor
        )
    {
        _knowledgeBaseRepository = knowledgeBaseRepository;
        _knowledgeChunkService = knowledgeChunkService;
        _documentService = documentService;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _taskQueueService = taskQueueService;
        _semanticKernelService = semanticKernelService;
        _documentParserSelector = documentParserSelector;
        _documentParseStrategyResolver = documentParseStrategyResolver;
        _vectorStore = vectorStore;
        _documentParseExecutor = documentParseExecutor;
    }
    /// <summary>
    /// 上传文档
    /// </summary>
    /// <param name="knowledgeBaseId"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<DocumentDto> UploadDocumentAsync(Guid knowledgeBaseId, FileUpload file, DocumentParseOptions? documentParseOptions = default)
    {
        // Upload document via document processing service
        // 验证知识库是否存在
        var knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {knowledgeBaseId} 的知识库");
        // 上传文件到存储服务
        var fileId = await _fileStorageService.UploadFileAsync(file.Content, file.FileName);

        // 创建文档记录
        var document = KnowledgeDocument.Create(
            knowledgeBaseId: knowledgeBaseId,
            fileName: file.FileName,
            filePath: fileId, // 存储服务返回的文件ID或路径
            fileSize: file.Content.Length,
            contentType: file.ContentType,
            fileHash: null // 可选：计算文件哈希值以防止重复上传
        );

        await _knowledgeDocumentRepository.AddAsync(document);
        await _unitOfWork.SaveChangesAsync();
        await ProcessDocumentAsync(document.Id);

        // 异步触发文档处理任务
        await _taskQueueService.EnqueueDocumentProcessingAsync(document.Id);

        return DocumentDto.ToDto(document);
    }

    public async Task<IEnumerable<DocumentDto>> GetDocumentsAsync(Guid knowledgeBaseId)
    {
        var documents = await _knowledgeDocumentRepository.GetByKnowledgeBaseIdAsync(knowledgeBaseId);
        return documents.Select(DocumentDto.ToDto);
    }
    /// <summary>
    /// 删除文档及其关联的知识块
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task DeleteDocumentAsync(Guid documentId)
    {
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            throw new ArgumentException($"Document with id {documentId} not found.");
        }

        // Delete associated chunks first
        var chunks = await _knowledgeChunkService.GetByDocumentIdAsync(documentId);
        foreach (var chunk in chunks)
        {
            chunk.IsDeleted = true;
        }

        // Delete document
        await _knowledgeDocumentRepository.DeleteAsync(document);
        await _unitOfWork.SaveChangesAsync();
    }
    /// <summary>
    /// 重新处理文档
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task ReprocessDocumentAsync(Guid documentId)
    {
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            throw new ArgumentException($"Document with id {documentId} not found.");
        }

        // Reprocess document via document processing service
        await ProcessDocumentAsync(documentId);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<DocumentDto> GetDocumentByIdAsync(Guid documentId)
    {
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            throw new ArgumentException($"Document with id {documentId} not found.");
        }

        return DocumentDto.ToDto(document);
    }

    public async Task<IEnumerable<KnowledgeChunkDto>> GetDocumentChunksAsync(Guid documentId)
    {
        // 验证文档存在
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            throw new ArgumentException($"Document with id {documentId} not found.");
        }

        // 获取文档的所有知识块
        var chunks = await _knowledgeChunkService.GetByDocumentIdAsync(documentId);

        // 映射到DTO
        return chunks.Select(chunk => new KnowledgeChunkDto
        {
            Id = chunk.Id,
            Content = chunk.Content,
            DocumentId = chunk.DocumentId,
            Index = chunk.ChunkIndex,
            CreatedAt = chunk.CreatedAt,
            VectorDimension = 0  // 需要从Embedding实体获取，暂时设为0
        });
    }



    public async Task ProcessDocumentAsync(Guid documentId, DocumentParseOptions? documentParseOptions = default)
    {
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {documentId} 的文档");

        // 1. 从文件存储下载文件
        var fileStream = await _fileStorageService.DownloadFileAsync(document.FilePath);
        if (fileStream == null)
            throw new FileNotFoundException($"未找到文档文件，路径: {document.FilePath}");

        // 2. 将FileType枚举转换为字符串
        var contentTypeString = document.ContentType.ToString();

        // 3. 使用解析策略决定解析模式
        var parseMode = _documentParseStrategyResolver.Resolve(document.FileName, document.FileSize, contentTypeString);

        // 4. 选择解析器并使用解析执行器解析文档
        var parser = _documentParserSelector.GetParser(document.FileName) ?? throw new NullReferenceException("找不到合适的文档解析器");

        // 使用解析执行器根据解析模式解析文档
        var parsedDocument = await _documentParseExecutor.ParseDocumentAsync(
            parser, parseMode, fileStream, document.FileName, documentParseOptions);

        // 5. 为每个分块生成向量
        var chunks = parsedDocument.Blocks.Select(b => b.Text).ToList();
        var vectors = await _semanticKernelService.GenerateEmbeddingsAsync(chunks);
        var vectorList = vectors.ToList();

        if (chunks.Count != vectorList.Count)
        {
            throw new InvalidOperationException($"向量生成数量不匹配: 分块数={chunks.Count}, 向量数={vectorList.Count}");
        }

        // 6. 创建知识块实体
        var chunkEntities = new List<KnowledgeChunk>();
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = document.CreateChunk(chunks[i], i);
            chunkEntities.Add(chunk);
        }

        // 7. 保存知识块到数据库
        await _knowledgeDocumentRepository.AddChunksAsync(chunkEntities);
        await _unitOfWork.SaveChangesAsync();

        // 8. 创建并保存向量数据
        var embeddings = new List<Embedding>();
        for (int i = 0; i < chunkEntities.Count; i++)
        {
            var chunk = chunkEntities[i];
            var vector = vectorList[i];

            var embedding = new Embedding
            {
                ChunkId = chunk.Id,
                Chunk = chunk,
                Vector = vector
            };
            embeddings.Add(embedding);
        }

        if (embeddings.Count > 0)
        {
            await _vectorStore.AddRangeAsync(embeddings);
        }

        // 9. 更新文档状态为已处理
        document.MarkAsProcessed(chunkEntities.Count);
        await _unitOfWork.SaveChangesAsync();

        // 10. 异步触发向量生成任务（用于监控或重试）
        foreach (var chunk in chunkEntities)
        {
            await _taskQueueService.EnqueueVectorGenerationAsync(chunk.Id);
        }
    }

}