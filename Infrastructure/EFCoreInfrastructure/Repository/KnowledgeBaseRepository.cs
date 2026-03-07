using System;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.Repository;

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
}