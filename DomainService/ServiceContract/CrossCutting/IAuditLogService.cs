using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract.CrossCutting;

public interface IAuditLogService
{
    Task LogOperationAsync(string operation, Guid? userId, object details);
    Task<IEnumerable<AuditLog>> QueryLogsAsync(DateTime start, DateTime end, string? operation);
}