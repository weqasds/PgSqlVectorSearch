using DomainService.Entities.System;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EFCoreInfrastructure.Repository;
public class AuditLogRepository : RepositoryBase<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(RagDbContext context) : base(context)
    {
    }

    public async Task<int> DeleteOlderThanAsync(int days)
    {
        if (days <= 0)
            return 0;

        var cutoff = DateTime.UtcNow.AddDays(-days);
        var toDelete = await _dbSet
            .Where(x => !x.IsDeleted && x.CreatedAt < cutoff)
            .ToListAsync();

        if (!toDelete.Any())
            return 0;

        _dbSet.RemoveRange(toDelete);

        return toDelete.Count;
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        return await _dbSet
            .Where(x => !x.IsDeleted && x.CreatedAt >= startDate && x.CreatedAt <= endDate)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByOperationAsync(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
            return Enumerable.Empty<AuditLog>();

        operation = operation.Trim();
        return await _dbSet
            .Where(x => !x.IsDeleted && x.Operation.Contains(operation, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAndDateRangeAsync(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        return await _dbSet
            .Where(x => !x.IsDeleted && x.UserId == userId && x.CreatedAt >= startDate && x.CreatedAt <= endDate)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(x => !x.IsDeleted && x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetSystemOperationsAsync()
    {
        return await _dbSet
            .Where(x => !x.IsDeleted && (x.UserId == null || x.UserId == Guid.Empty))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}

