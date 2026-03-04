using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract;

namespace DomainService.Services.Infrastructure;

public class AuditLogService : IAuditLogService
{
    private readonly IRepository<AuditLog> _auditLogRepository;

    public AuditLogService(IRepository<AuditLog> auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogOperationAsync(string operation, Guid? userId, object details)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("操作类型不能为空", nameof(operation));

        if (details == null)
            throw new ArgumentNullException(nameof(details));

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Operation = operation,
            UserId = userId,
            Details = JsonSerializer.Serialize(details),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _auditLogRepository.AddAsync(auditLog);
    }

    public async Task<IEnumerable<AuditLog>> QueryLogsAsync(DateTime start, DateTime end, string? operation)
    {
        if (start > end)
            throw new ArgumentException("开始时间不能晚于结束时间", nameof(start));

        // 获取所有审计日志
        var allLogs = await _auditLogRepository.GetAllAsync();
        var filteredLogs = new List<AuditLog>();

        foreach (var log in allLogs)
        {
            if (log.IsDeleted)
                continue;

            // 过滤时间范围
            if (log.CreatedAt < start || log.CreatedAt > end)
                continue;

            // 过滤操作类型
            if (!string.IsNullOrEmpty(operation) && !log.Operation.Contains(operation, StringComparison.OrdinalIgnoreCase))
                continue;

            filteredLogs.Add(log);
        }

        return filteredLogs.OrderByDescending(log => log.CreatedAt);
    }
}