using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.Repository;

public class RagRetrievalLogRepository : RepositoryBase<RagRetrievalLog>, IRagRetrievalLogRepository
{
    public RagRetrievalLogRepository(RagDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<RagRetrievalLog>> GetLogsOlderThanAsync(DateTimeOffset cutoffDate)
    {
        return await _dbSet
            .Where(r => r.RagGeneration.CreatedAt < cutoffDate)
            .ToListAsync();
    }

    public async Task DeleteLogsAsync(IEnumerable<RagRetrievalLog> logs)
    {
        _dbSet.RemoveRange(logs);
    }

    public async Task<int> GetCountByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        return await _dbSet
            .CountAsync(r => r.RagGeneration.CreatedAt >= startDate && r.RagGeneration.CreatedAt <= endDate);
    }
}