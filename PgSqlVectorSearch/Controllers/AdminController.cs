using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;
using Application.ApplicationServiceContract.CrossCutting;
using Application.QueryServices;
using DomainService.Entities.System;

namespace PgSqlVectorSearch.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ISystemAdminAppService _systemAdminAppService;
    private readonly IAuditLogService _auditLogService;
    private readonly IQueryService<AuditLog> _auditQueryService;

    public AdminController(
        ISystemAdminAppService systemAdminAppService,
        IAuditLogService auditLogService,
        IQueryService<AuditLog> auditQueryService
        )
    {
        _systemAdminAppService = systemAdminAppService;
        _auditLogService = auditLogService;
        _auditQueryService = auditQueryService;
    }
    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    /// <returns></returns>
    [HttpGet("health")]
    public async Task<ActionResult<SystemStatusDto>> GetHealth()
    {
        var result = await _systemAdminAppService.GetSystemStatusAsync();
        return Ok(result);
    }
    /// <summary>
    /// 获取系统指标数据
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    [HttpGet("metrics")]
    public async Task<ActionResult<SystemMetricsDto>> GetMetrics([FromQuery] TimeRange range)
    {
        var result = await _systemAdminAppService.GetSystemMetricsAsync(range);
        return Ok(result);
    }

    /// <summary>
    /// 获取日志
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    [HttpGet("logs")]
    public async Task<ActionResult> GetLogs(
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        [FromQuery] string? operation)
    {
        var logs = await _auditQueryService.FindAsync(log =>
            (!start.HasValue || log.CreatedAt >= start.Value) &&
            (!end.HasValue || log.CreatedAt <= end.Value) &&
            (string.IsNullOrEmpty(operation) || log.Operation == operation)
        );
        return Ok(logs);
    }

    /// <summary>
    /// 清理旧数据
    /// </summary>
    /// <param name="cutoffDate"></param>
    /// <returns></returns>
    [HttpPost("cleanup")]
    public async Task<ActionResult> Cleanup([FromQuery] DateTimeOffset? cutoffDate)
    {
        var date = cutoffDate ?? DateTime.UtcNow.AddDays(-30);
        await _systemAdminAppService.CleanupOldDataAsync(date);
        return Accepted();
    }
}