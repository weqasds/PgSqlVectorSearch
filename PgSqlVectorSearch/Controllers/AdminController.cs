using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;

namespace PgSqlVectorSearch.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ISystemAdminAppService _systemAdminAppService;

    public AdminController(ISystemAdminAppService systemAdminAppService)
    {
        _systemAdminAppService = systemAdminAppService;
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
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] string? operation)
    {
        // Note: Audit log query not defined in ISystemAdminAppService
        // May need to use IAuditLogService or add method to ISystemAdminAppService
        return StatusCode(501, "Audit log query not implemented");
    }

    [HttpPost("cleanup")]
    public async Task<ActionResult> Cleanup([FromQuery] DateTime? cutoffDate)
    {
        var date = cutoffDate ?? DateTime.UtcNow.AddDays(-30);
        await _systemAdminAppService.CleanupOldDataAsync(date);
        return Accepted();
    }
}