using System;
using System.Threading.Tasks;

namespace Application.ApplicationServiceContract;

public interface IBatchOperationAppService
{
    Task<BatchImportResultDto> ImportDocumentsAsync(Guid knowledgeBaseId, FileUpload zipFile);
    Task<BatchProcessResultDto> ReprocessKnowledgeBaseAsync(Guid knowledgeBaseId);
}