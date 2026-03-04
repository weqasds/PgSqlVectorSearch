using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;


namespace PgSqlVectorSearch.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsAppService _analyticsAppService;

    public AnalyticsController(IAnalyticsAppService analyticsAppService)
    {
        _analyticsAppService = analyticsAppService;
    }

    [HttpGet("usage")]
    public async Task<ActionResult<UsageStatisticsDto>> GetUsageStatistics(
        [FromQuery] DateOnly? start,
        [FromQuery] DateOnly? end)
    {
        var startDate = start ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var endDate = end ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await _analyticsAppService.GetUsageStatisticsAsync(startDate, endDate);
        return Ok(result);
    }

    [HttpGet("knowledge-bases/{id}/usage")]
    public async Task<ActionResult<KnowledgeBaseUsageDto>> GetKnowledgeBaseUsage(Guid id)
    {
        var result = await _analyticsAppService.GetKnowledgeBaseUsageAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet("queries")]
    public async Task<ActionResult<PopularQueriesDto>> GetPopularQueries([FromQuery] int top = 10)
    {
        var result = await _analyticsAppService.GetPopularQueriesAsync(top);
        return Ok(result);
    }
}