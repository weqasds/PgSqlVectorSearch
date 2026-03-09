using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using Pgvector;

namespace DomainService.ServiceContract.Services;

/// <summary>
/// 向量检索服务接口
/// 负责处理文本向量化和相似度计算
/// </summary>
public interface IVectorRetrievalService
{
    Task<IEnumerable<RetrievalResult>> SearchSimilarChunksAsync(
        string query,
        int topK = 5,
        Guid? knowledgeBaseId = null,
        Guid? documentId = null);
    Task<Vector> GenerateEmbeddingAsync(string text);
    Task<double> CalculateSimilarityAsync(Vector v1, Vector v2);
}