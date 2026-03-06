using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;
using Pgvector;

namespace DomainService.Services.VectorRetrieval;

public class VectorRetrievalService : IVectorRetrievalService
{
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly IKnowledgeChunkRepository _knowledgeChunkRepository;

    public VectorRetrievalService(
        IEmbeddingRepository embeddingRepository,
        IKnowledgeChunkRepository knowledgeChunkRepository)
    {
        _embeddingRepository = embeddingRepository;
        _knowledgeChunkRepository = knowledgeChunkRepository;
    }

    public async Task<IEnumerable<RetrievalResult>> SearchSimilarChunksAsync(string query, int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("查询文本不能为空", nameof(query));

        if (topK <= 0)
            throw new ArgumentException("topK必须大于0", nameof(topK));

        // 生成查询向量
        var queryVector = await GenerateEmbeddingAsync(query);

        // 搜索相似向量
        var similarEmbeddings = await _embeddingRepository.SearchSimilarAsync(queryVector, topK);

        var results = new List<RetrievalResult>();
        foreach (var embedding in similarEmbeddings)
        {
            // 获取对应的知识块
            var chunk = await _knowledgeChunkRepository.GetByIdAsync(embedding.ChunkId);
            if (chunk == null || chunk.IsDeleted)
                continue;

            // 计算相似度分数（假设SearchSimilarAsync已经按相似度排序）
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

        // TODO: 调用AI服务（如OpenAI、Azure OpenAI等）生成文本向量
        // 这里返回一个模拟的1536维向量
        // 实际实现应该调用Semantic Kernel或其他嵌入服务

        var random = new Random();
        var values = new float[1536];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = (float)random.NextDouble() * 2 - 1; // 生成-1到1之间的随机数
        }

        return new Vector(values);
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