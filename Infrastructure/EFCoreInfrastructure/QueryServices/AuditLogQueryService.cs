using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.QueryServices;
using DomainService.Entities.Core;
using DomainService.Entities.KnowledgeManagement;
using DomainService.Entities.KnowledgeGraph;
using DomainService.Entities.Enums;
using DomainService.Entities.VectorSearch;
using DomainService.Entities.Chat;
using DomainService.Entities.UserManagement;
using DomainService.Entities.System;
using DomainService.Entities.RAG;
using DomainService.Entities.Analytics;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.QueryServices;

public class AuditLogQueryService : QueryServiceBase<AuditLog>
{
    public AuditLogQueryService(RagDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetByOperationAsync(string operation)
    {
        return await _dbSet.Where(log => log.Operation == operation && !log.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet.Where(log => log.UserId == userId && !log.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        return await _dbSet.Where(log => log.CreatedAt >= startDate && log.CreatedAt <= endDate && !log.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAndDateRangeAsync(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        return await _dbSet.Where(log => log.UserId == userId && log.CreatedAt >= startDate && log.CreatedAt <= endDate && !log.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetSystemOperationsAsync()
    {
        return await _dbSet.Where(log => log.UserId == null && !log.IsDeleted).ToListAsync();
    }

    public async Task<int> DeleteOlderThanAsync(int days)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var oldLogs = await _dbSet.Where(log => log.CreatedAt < cutoffDate).ToListAsync();

        if (oldLogs.Any())
        {
            _dbSet.RemoveRange(oldLogs);
            return await _context.SaveChangesAsync();
        }

        return 0;
    }
}