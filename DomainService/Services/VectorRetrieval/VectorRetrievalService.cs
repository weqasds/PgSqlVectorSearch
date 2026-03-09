using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;
using DomainService.Entities.Enums;
using DomainService.Entities.RAG;
using DomainService.Entities.VectorSearch;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.CrossCutting;
using Pgvector;

namespace DomainService.Services.VectorRetrieval;

public class VectorRetrievalService : IVectorRetrievalService
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingService _embeddingService;

    public VectorRetrievalService(
        IVectorStore vectorStore,
        IEmbeddingService embeddingService)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
    }

    public async Task<IEnumerable<RetrievalResult>> SearchSimilarChunksAsync(
        string query,
        int topK = 5,
        Guid? knowledgeBaseId = null,
        Guid? documentId = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("查询文本不能为空", nameof(query));

        if (topK <= 0)
            throw new ArgumentException("topK必须大于0", nameof(topK));

        // 生成查询向量
        var queryVector = await GenerateEmbeddingAsync(query);

        // 搜索相似向量，使用余弦距离以保持与相似度计算的一致性
        IEnumerable<Embedding> similarEmbeddings;
        if (knowledgeBaseId.HasValue || documentId.HasValue)
        {
            // 使用带过滤的搜索
            similarEmbeddings = await _vectorStore.SearchSimilarWithFilterAsync(
                queryVector,
                topK,
                DistanceMetric.Cosine,
                knowledgeBaseId,
                documentId);
        }
        else
        {
            // 使用普通搜索
            similarEmbeddings = await _vectorStore.SearchSimilarAsync(queryVector, topK, DistanceMetric.Cosine);
        }

        var results = new List<RetrievalResult>();
        foreach (var embedding in similarEmbeddings)
        {
            // 检查知识块是否存在且未被删除
            // 注意：如果使用了Include，embedding.Chunk可能已经加载
            var chunk = embedding.Chunk;
            if (chunk == null || chunk.IsDeleted)
                continue;

            // 计算相似度分数（余弦相似度）
            var similarity = await CalculateSimilarityAsync(queryVector, embedding.Vector);

            results.Add(new RetrievalResult
            {
                Chunk = chunk,
                SimilarityScore = similarity
            });
        }

        return results;
    }

    public async Task<Vector> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("文本不能为空", nameof(text));

        // 使用IEmbeddingService生成文本向量
        return await _embeddingService.GenerateEmbeddingAsync(text);
    }

    public async Task<double> CalculateSimilarityAsync(Vector v1, Vector v2)
    {
        if (v1 == null) throw new ArgumentNullException(nameof(v1));
        if (v2 == null) throw new ArgumentNullException(nameof(v2));

        // 使用余弦相似度计算两个向量的相似度
        // 公式: cosθ = (A·B) / (||A|| * ||B||)

        var dotProduct = 0.0f;
        var normA = 0.0f;
        var normB = 0.0f;

        var aArray = v1.ToArray();
        var bArray = v2.ToArray();

        if (aArray.Length != bArray.Length)
            throw new ArgumentException("向量维度不匹配");

        for (int i = 0; i < aArray.Length; i++)
        {
            dotProduct += aArray[i] * bArray[i];
            normA += aArray[i] * aArray[i];
            normB += bArray[i] * bArray[i];
        }

        normA = (float)Math.Sqrt(normA);
        normB = (float)Math.Sqrt(normB);

        if (normA == 0 || normB == 0)
            return 0;

        return dotProduct / (normA * normB);
    }
}