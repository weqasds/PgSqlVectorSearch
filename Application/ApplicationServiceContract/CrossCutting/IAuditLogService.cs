using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.System;

namespace Application.ApplicationServiceContract.CrossCutting;

public interface IAuditLogService
{
    Task LogOperationAsync(string operation, Guid? userId, object details);
    Task<IEnumerable<AuditLog>> QueryLogsAsync(DateTimeOffset? start, DateTimeOffset? end, string? operation);
}
