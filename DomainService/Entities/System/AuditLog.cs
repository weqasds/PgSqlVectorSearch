using System;
using DomainService.Entities.Core;

namespace DomainService.Entities.System;

public class AuditLog : Entity<Guid>
{
    public string Operation { get; set; } = null!;
    public Guid? UserId { get; set; }
    public string Details { get; set; } = null!;

    // 私有构造函数用于工厂方法和反序列化
    private AuditLog() { }

    /// <summary>
    /// 创建审计日志的工厂方法
    /// </summary>
    public static AuditLog Create(string operation, Guid? userId = null, string? details = null)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("操作类型不能为空", nameof(operation));

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Operation = operation.Trim(),
            UserId = userId,
            Details = details ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        return auditLog;
    }

    /// <summary>
    /// 更新审计日志详情
    /// </summary>
    public void UpdateDetails(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
            throw new ArgumentException("详情不能为空", nameof(details));

        Details = details;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加操作详情
    /// </summary>
    public void AppendDetails(string additionalDetails)
    {
        if (string.IsNullOrWhiteSpace(additionalDetails))
            throw new ArgumentException("附加详情不能为空", nameof(additionalDetails));

        Details = string.IsNullOrEmpty(Details) ? additionalDetails : Details + "; " + additionalDetails;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 验证操作类型是否有效
    /// </summary>
    public static bool IsValidOperation(string operation)
    {
        return !string.IsNullOrWhiteSpace(operation) && operation.Length <= 100;
    }

    /// <summary>
    /// 验证详情长度是否有效
    /// </summary>
    public static bool IsValidDetails(string details)
    {
        return details.Length <= 5000; // 最大5000字符
    }

    /// <summary>
    /// 检查是否为系统操作（无用户关联）
    /// </summary>
    public bool IsSystemOperation()
    {
        return !UserId.HasValue;
    }

    /// <summary>
    /// 获取操作摘要（前50个字符）
    /// </summary>
    public string GetOperationSummary()
    {
        if (string.IsNullOrEmpty(Operation))
            return string.Empty;

        return Operation.Length <= 50 ? Operation : Operation.Substring(0, 47) + "...";
    }

    /// <summary>
    /// 获取详情摘要（前100个字符）
    /// </summary>
    public string GetDetailsSummary()
    {
        if (string.IsNullOrEmpty(Details))
            return string.Empty;

        return Details.Length <= 100 ? Details : Details.Substring(0, 97) + "...";
    }
}