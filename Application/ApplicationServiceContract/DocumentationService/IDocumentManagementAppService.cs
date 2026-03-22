using Application.Dto;
using Application.Models.DocumentParsing;
namespace Application.ApplicationServiceContract.DocumentationService;

public interface IDocumentManagementAppService
{
    /// <summary>
    /// 上传文档
    /// </summary>
    /// <param name="knowledgeBaseId"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    Task<DocumentDto> UploadDocumentAsync(Guid knowledgeBaseId, FileUpload file, DocumentParseOptions? documentParseOptions = default);
    /// <summary>
    /// 查询文档
    /// </summary>
    /// <param name="knowledgeBaseId"></param>
    /// <returns></returns>
    Task<IEnumerable<DocumentDto>> GetDocumentsAsync(Guid knowledgeBaseId);
    /// <summary>
    /// 删除文档
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns></returns>
    Task DeleteDocumentAsync(Guid documentId);
    /// <summary>
    /// 重新处理文档，删除原有知识块并重新生成
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns></returns>
    Task ReprocessDocumentAsync(Guid documentId);
    /// <summary>
    /// 处理文档，生成知识块并关联文档
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns></returns>
    Task ProcessDocumentAsync(Guid documentId, DocumentParseOptions? documentParseOptions = default);
    /// <summary>
    /// 根据文档ID获取文档详情，包括关联的知识块信息
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns></returns>
    Task<DocumentDto> GetDocumentByIdAsync(Guid documentId);
    /// <summary>
    /// 根据文档ID获取关联的知识块信息
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns></returns>
    Task<IEnumerable<KnowledgeChunkDto>> GetDocumentChunksAsync(Guid documentId);
    
}