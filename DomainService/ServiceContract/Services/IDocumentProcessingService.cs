using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract.Services;

public interface IDocumentProcessingService
{
    Task<KnowledgeDocument> UploadDocumentAsync(Guid knowledgeBaseId, Stream fileStream, string fileName);
    Task ProcessDocumentAsync(Guid documentId);
    Task<IEnumerable<KnowledgeChunk>> GetDocumentChunksAsync(Guid documentId);
}