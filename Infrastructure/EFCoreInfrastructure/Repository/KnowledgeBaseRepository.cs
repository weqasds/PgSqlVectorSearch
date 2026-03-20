using System;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCoreInfrastructure.Repository;

public class KnowledgeBaseRepository : RepositoryBase<KnowledgeBase>, IKnowledgeBaseRepository
{
    public KnowledgeBaseRepository(RagDbContext context) : base(context)
    {
    }

    public async Task<KnowledgeBase?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(kb => kb.Name == name);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _dbSet.AnyAsync(kb => kb.Name == name);
    }

    public async Task<int> GetChunkCountAsync(Guid knowledgeBaseId)
    {
        // Count chunks via documents
        return await _context.KnowledgeChunks
            .Join(_context.KnowledgeDocuments,
                chunk => chunk.DocumentId,
                doc => doc.Id,
                (chunk, doc) => new { chunk, doc })
            .Where(joined => joined.doc.KnowledgeBaseId == knowledgeBaseId)
            .CountAsync();
    }

    public async Task<KnowledgeBase> GetKnowledgeBaseWithUserAccessesAsync(Guid knowledgeBaseId)
    {
        return await _dbSet
            .Include(kb => kb.UserAccesses)
                .ThenInclude(ua => ua.User)
            .FirstOrDefaultAsync(kb => kb.Id == knowledgeBaseId)
            .ContinueWith(task =>
            {
                var kb = task.Result;
                if (kb == null)
                    throw new KeyNotFoundException($"未找到ID为{knowledgeBaseId}的知识库");
                return kb;
            });
    }

    public async Task<IEnumerable<UserKnowledgeBase>> GetKnowledgeBaseUserAccessesAsync(Guid knowledgeBaseId)
    {
        return await _context.UserKnowledgeBases
            .Include(ukb => ukb.User)
            .Where(ukb => ukb.KnowledgeBaseId == knowledgeBaseId)
            .ToListAsync();
    }


}