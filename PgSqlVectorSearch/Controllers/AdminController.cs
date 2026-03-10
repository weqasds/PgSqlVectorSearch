using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;
using Application.ApplicationServiceContract.CrossCutting;

namespace PgSqlVectorSearch.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ISystemAdminAppService _systemAdminAppService;
    private readonly IAuditLogService _auditLogService;

    public AdminController(ISystemAdminAppService systemAdminAppService, 
                            IAuditLogService auditLogService)
    {
        _systemAdminAppService = systemAdminAppService;
        _auditLogService = auditLogService;
    }

    [HttpGet("health")]
    public async Task<ActionResult<SystemStatusDto>> GetHealth()
    {
        var result = await _systemAdminAppService.GetSystemStatusAsync();
        return Ok(result);
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<SystemMetricsDto>> GetMetrics([FromQuery] TimeRange range)
    {
        var result = await _systemAdminAppService.GetSystemMetricsAsync(range);
        return Ok(result);
    }

    [HttpGet("logs")]
    public async Task<ActionResult> GetLogs(
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        [FromQuery] string? operation)
    {
        var logs = await _auditLogService.QueryLogsAsync(start, end, operation);
        return Ok(logs);
    }

    [HttpPost("cleanup")]
    public async Task<ActionResult> Cleanup([FromQuery] DateTimeOffset? cutoffDate)
    {
        var date = cutoffDate ?? DateTime.UtcNow.AddDays(-30);
        await _systemAdminAppService.CleanupOldDataAsync(date);
        return Accepted();
    }
}