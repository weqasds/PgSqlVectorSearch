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
namespace Application.ApplicationService;

public class DocumentManagementAppService : IDocumentManagementAppService
{
    private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly IDocumentService _documentService;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IKnowledgeChunkQueryService _knowledgeChunkService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    //后台任务服务
    private readonly ITaskQueueService _taskQueueService;
    public DocumentManagementAppService(
        IKnowledgeBaseRepository knowledgeBaseRepository,
        ISemanticKernelService semanticKernelService,
        IKnowledgeChunkQueryService knowledgeChunkService,
        IDocumentService documentService,
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        ITaskQueueService taskQueueService)
    {
        _knowledgeBaseRepository = knowledgeBaseRepository;
        _knowledgeChunkService = knowledgeChunkService;
        _documentService = documentService;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _taskQueueService = taskQueueService;
        _semanticKernelService= semanticKernelService;
    }
    /// <summary>
    /// 上传文档
    /// </summary>
    /// <param name="knowledgeBaseId"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<DocumentDto> UploadDocumentAsync(Guid knowledgeBaseId, FileUpload file)
    {
        // Upload document via document processing service
        // 验证知识库是否存在
        var knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {knowledgeBaseId} 的知识库");
        // 上传文件到存储服务
        var fileId = await _fileStorageService.UploadFileAsync(file.Content, file.FileName);

        // 创建文档记录
        var document =  KnowledgeDocument.Create(
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

    

    public async Task ProcessDocumentAsync(Guid documentId)
    {
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null || document.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {documentId} 的文档");

        // TODO: 实现文档处理逻辑，包括：
        // 1. 从文件存储下载文件
        var fileStream = await _fileStorageService.DownloadFileAsync(document.FilePath);
        // 2. 解析文档内容（支持PDF、Word、TXT等格式）
        if (fileStream == null)
            throw new FileNotFoundException($"未找到文档文件，路径: {document.FilePath}");
        string content = _documentService.ToString()?? "";
        // 3. 分块处理文本
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("");
        var chunks=new List<string>();
        // 4. 为每个分块生成向量
        var vectors=await _semanticKernelService.GenerateEmbeddingsAsync(chunks);
        // 5. 保存分块和向量到数据库
        var chunkEntites = new List<KnowledgeChunk>();
        foreach(var item in chunks){}
        var sampleChunk = document.CreateChunk($"这是文档 '{document.FileName}' 的示例分块内容。", 0);
        // 更新文档状态为已处理
        document.MarkAsProcessed(1); // 假设处理了1个块
        
        // 保存文档变更（包括新知识块）
        //await _knowledgeDocumentRepository.UpdateAsync(document);
        await _knowledgeDocumentRepository.AddChunksAsync(new List<KnowledgeChunk>() { sampleChunk });
        await _unitOfWork.SaveChangesAsync();

        // 异步触发向量生成任务
        await _taskQueueService.EnqueueVectorGenerationAsync(sampleChunk.Id);
    }

}