using System;
using System.Threading.Tasks;

namespace Application.ApplicationServiceContract;

public interface IAnalyticsAppService
{
    Task<UsageStatisticsDto> GetUsageStatisticsAsync(DateOnly start, DateOnly end);
    Task<KnowledgeBaseUsageDto> GetKnowledgeBaseUsageAsync(Guid knowledgeBaseId);
    Task<PopularQueriesDto> GetPopularQueriesAsync(int topN);
}