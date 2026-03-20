using DomainService.Entities.Enums;
using DomainService.Entities.VectorSearch;
using DomainService.ServiceContract.CrossCutting;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Infrastructure.EFCoreInfrastructure.Repository;

/// <summary>
/// 向量存储服务实现，使用EF Core和pgvector进行向量存储和相似性搜索
/// 作为基础设施层服务，不属于领域层仓储模式
/// </summary>
public class EmbeddingRepository :  IVectorStore
{
    private readonly RagDbContext _dbContext;
    private readonly DbSet<Embedding> _dbSet;
    public EmbeddingRepository(RagDbContext context, RagDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<Embedding>();
    }

    public async Task<Embedding?> GetByChunkIdAsync(Guid chunkId)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.ChunkId == chunkId);
    }

    public async Task<IEnumerable<Embedding>> SearchSimilarAsync(Vector queryVector, int limit, DistanceMetric metric = DistanceMetric.L2)
    {
        return metric switch
        {
            DistanceMetric.L2 => await SearchByL2Distance(queryVector, limit),
            DistanceMetric.Cosine => await SearchByCosineDistance(queryVector, limit),
            DistanceMetric.InnerProduct => throw new NotSupportedException("pgvector目前不支持InnerProduct距离度量，请使用L2或Cosine距离"),
            _ => throw new ArgumentException($"不支持的度量标准: {metric}")
        };
    }

    private async Task<IEnumerable<Embedding>> SearchByL2Distance(Vector queryVector, int limit)
    {
        return await _dbSet
            .OrderBy(e => e.Vector.L2Distance(queryVector))
            .Take(limit)
            .ToListAsync();
    }

    private async Task<IEnumerable<Embedding>> SearchByCosineDistance(Vector queryVector, int limit)
    {
        return await _dbSet
            .OrderBy(e => e.Vector.CosineDistance(queryVector))
            .Take(limit)
            .ToListAsync();
    }

    public async Task DeleteByChunkIdAsync(Guid chunkId)
    {
        var embedding = await GetByChunkIdAsync(chunkId);
        if (embedding != null)
        {
            _dbContext.Remove(embedding);
        }
    }

    public async Task AddAsync(Embedding embedding)
    {
        await _dbSet.AddAsync(embedding);
    }

    public async Task AddRangeAsync(IEnumerable<Embedding> embeddings)
    {
        await _dbSet.AddRangeAsync(embeddings);
    }

    public async Task<IEnumerable<Embedding>> SearchSimilarWithFilterAsync(
        Vector queryVector,
        int limit,
        DistanceMetric metric = DistanceMetric.L2,
        Guid? knowledgeBaseId = null,
        Guid? documentId = null)
    {
        var query = _dbSet.AsQueryable();

        // 应用过滤条件
        if (knowledgeBaseId.HasValue)
        {
            // 通过Chunk->Document->KnowledgeBase进行过滤
            query = query
                .Include(e => e.Chunk)
                .ThenInclude(c => c.Document)
                .Where(e => e.Chunk.Document.KnowledgeBaseId == knowledgeBaseId.Value);
        }

        if (documentId.HasValue)
        {
            query = query
                .Include(e => e.Chunk)
                .Where(e => e.Chunk.DocumentId == documentId.Value);
        }

        // 根据度量标准执行向量相似度搜索
        return metric switch
        {
            DistanceMetric.L2 => await query
                .OrderBy(e => e.Vector.L2Distance(queryVector))
                .Take(limit)
                .ToListAsync(),
            DistanceMetric.Cosine => await query
                .OrderBy(e => e.Vector.CosineDistance(queryVector))
                .Take(limit)
                .ToListAsync(),
            DistanceMetric.InnerProduct => throw new NotSupportedException("pgvector目前不支持InnerProduct距离度量，请使用L2或Cosine距离"),
            _ => throw new ArgumentException($"不支持的度量标准: {metric}")
        };
    }
}