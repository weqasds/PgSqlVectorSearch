using Application.QueryServices;
using DomainService.Entities.VectorSearch;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Infrastructure.EFCoreInfrastructure.QueryServices;

/// <summary>
/// 向量搜索查询服务实现
/// 负责向量检索相关的复杂查询操作
/// </summary>
public class VectorSearchQueryService : QueryServiceBase<Embedding>, IVectorSearchQueryService
{
    public VectorSearchQueryService(RagDbContext context) : base(context)
    {
    }

    public async Task<Embedding?> GetByChunkIdAsync(Guid chunkId)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.ChunkId == chunkId);
    }

    public async Task<IEnumerable<Embedding>> SearchSimilarAsync(Vector queryVector, int limit)
    {
        // 使用pgvector的向量相似度搜索
        // 注意：这里使用了EF.Functions，需要确保pgvector扩展已正确配置
        return await _dbSet
            .OrderBy(e => e.Vector.L2Distance(queryVector))
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Embedding>> SearchSimilarWithFilterAsync(
        Vector queryVector,
        int limit,
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

        // 执行向量相似度搜索
        return await query
            .OrderBy(e => e.Vector.L2Distance(queryVector))
            .Take(limit)
            .ToListAsync();
    }
}