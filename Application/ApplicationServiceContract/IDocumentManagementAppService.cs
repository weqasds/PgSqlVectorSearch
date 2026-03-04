using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.ApplicationServiceContract;

public interface IDocumentManagementAppService
{
    Task<DocumentDto> UploadDocumentAsync(Guid knowledgeBaseId, FileUpload file);
    Task<IEnumerable<DocumentDto>> GetDocumentsAsync(Guid knowledgeBaseId);
    Task DeleteDocumentAsync(Guid documentId);
    Task ReprocessDocumentAsync(Guid documentId);
}