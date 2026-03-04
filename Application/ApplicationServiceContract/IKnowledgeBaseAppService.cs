using System;
using System.Threading.Tasks;

namespace Application.ApplicationServiceContract;

public interface IKnowledgeBaseAppService
{
    Task<KnowledgeBaseDto> CreateKnowledgeBaseAsync(CreateKnowledgeBaseRequest request);
    Task<KnowledgeBaseDto> GetKnowledgeBaseAsync(Guid id);
    Task<PagedResult<KnowledgeBaseDto>> ListKnowledgeBasesAsync(int page, int pageSize);
    Task UploadDocumentAsync(Guid knowledgeBaseId, UploadDocumentRequest request);
    Task ProcessKnowledgeBaseAsync(Guid knowledgeBaseId);
}