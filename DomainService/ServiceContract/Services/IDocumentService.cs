using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;

namespace DomainService.ServiceContract.Services;

public interface IDocumentService
{
    /// <summary>
    /// 获取重复文档
    /// </summary>
    /// <param name="knowledgeBaseId"></param>
    /// <param name="fileHash"></param>
    /// <returns></returns>
    Task<KnowledgeDocument?> FindDuplicateDocumentAsync(Guid knowledgeBaseId, string fileHash);
    /// <summary>
    /// 获取文档chunks
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns></returns>
    Task<IEnumerable<KnowledgeChunk>> GetDocumentChunksAsync(Guid documentId);
}