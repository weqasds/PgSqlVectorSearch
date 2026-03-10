
using DomainService.Entities.System;

namespace DomainService.ServiceContract.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// 根据操作类型查询审计日志
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByOperationAsync(string operation);

    /// <summary>
    /// 根据用户ID查询审计日志
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// 查询时间范围内的审计日志
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// 查询特定用户在时间范围内的审计日志
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserIdAndDateRangeAsync(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// 查询系统操作日志（无用户关联）
    /// </summary>
    Task<IEnumerable<AuditLog>> GetSystemOperationsAsync();

    /// <summary>
    /// 删除指定天数前的审计日志
    /// </summary>
    Task<int> DeleteOlderThanAsync(int days);
}