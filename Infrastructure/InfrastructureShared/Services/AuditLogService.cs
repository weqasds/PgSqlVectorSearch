using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using Application.ApplicationServiceContract.CrossCutting;

namespace InfrastructureShared.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogOperationAsync(string operation, Guid? userId, object details)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("操作类型不能为空", nameof(operation));

        if (details == null)
            throw new ArgumentNullException(nameof(details));

        var auditLog = AuditLog.Create(operation, userId, JsonSerializer.Serialize(details));

        await _auditLogRepository.AddAsync(auditLog);
    }
}