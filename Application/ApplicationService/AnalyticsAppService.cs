using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.ServiceContract;
using Application.ApplicationServiceContract;

namespace Application.ApplicationService;

public class AnalyticsAppService : IAnalyticsAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
    private readonly IRagGenerationRepository _ragGenerationRepository;

    public AnalyticsAppService(
        IUserRepository userRepository,
        IChatSessionRepository chatSessionRepository,
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IKnowledgeBaseRepository knowledgeBaseRepository,
        IRagGenerationRepository ragGenerationRepository)
    {
        _userRepository = userRepository;
        _chatSessionRepository = chatSessionRepository;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _knowledgeBaseRepository = knowledgeBaseRepository;
        _ragGenerationRepository = ragGenerationRepository;
    }

    public async Task<UsageStatisticsDto> GetUsageStatisticsAsync(DateOnly start, DateOnly end)
    {
        // Get total users
        var totalUsers = await _userRepository.GetTotalCountAsync();

        // Get active users (users with activity within the date range)
        var activeUsers = await _userRepository.GetActiveUserCountAsync(start, end);

        // Get total queries (RAG generations) within date range
        var totalQueries = await _ragGenerationRepository.GetCountByDateRangeAsync(start, end);

        // Get total documents uploaded within date range
        var totalDocumentsUploaded = await _knowledgeDocumentRepository.GetCountByDateRangeAsync(start, end);

        // Get daily usage breakdown
        var dailyUsages = new List<DailyUsage>();
        var currentDate = start;
        while (currentDate <= end)
        {
            var queryCount = await _ragGenerationRepository.GetCountByDateAsync(currentDate);
            var userCount = await _userRepository.GetActiveUserCountByDateAsync(currentDate);
            var documentCount = await _knowledgeDocumentRepository.GetCountByDateAsync(currentDate);

            dailyUsages.Add(new DailyUsage
            {
                Date = currentDate,
                QueryCount = queryCount,
                UserCount = userCount,
                DocumentCount = documentCount
            });

            currentDate = currentDate.AddDays(1);
        }

        return new UsageStatisticsDto
        {
            StartDate = start,
            EndDate = end,
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalQueries = totalQueries,
            TotalDocumentsUploaded = totalDocumentsUploaded,
            DailyUsages = dailyUsages
        };
    }

    public async Task<KnowledgeBaseUsageDto> GetKnowledgeBaseUsageAsync(Guid knowledgeBaseId)
    {
        var knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId);
        if (knowledgeBase == null)
        {
            throw new ArgumentException($"Knowledge base with id {knowledgeBaseId} not found.");
        }

        // Get document count
        var documentCount = await _knowledgeDocumentRepository.GetDocumentCountAsync(knowledgeBaseId);

        // Get chunk count (via knowledge base or separate repository)
        var chunkCount = await _knowledgeBaseRepository.GetChunkCountAsync(knowledgeBaseId);

        // Get query count (RAG generations referencing this knowledge base)
        var queryCount = await _ragGenerationRepository.GetCountByKnowledgeBaseIdAsync(knowledgeBaseId);

        // Get last query time
        var lastQueryTime = await _ragGenerationRepository.GetLastQueryTimeByKnowledgeBaseIdAsync(knowledgeBaseId);

        // Get top queries
        var topQueries = await _ragGenerationRepository.GetTopQueriesByKnowledgeBaseIdAsync(knowledgeBaseId, 10);

        return new KnowledgeBaseUsageDto
        {
            KnowledgeBaseId = knowledgeBaseId,
            KnowledgeBaseName = knowledgeBase.Name,
            DocumentCount = documentCount,
            ChunkCount = chunkCount,
            QueryCount = queryCount,
            LastQueryTime = lastQueryTime,
            TopQueries = topQueries.Select(q => new QueryStat
            {
                Query = q.Query,
                Count = q.Count,
                LastQueried = q.LastQueried
            })
        };
    }

    public async Task<PopularQueriesDto> GetPopularQueriesAsync(int topN)
    {
        var popularQueries = await _ragGenerationRepository.GetPopularQueriesAsync(topN);

        return new PopularQueriesDto
        {
            TopN = topN,
            Queries = popularQueries.Select(q => new QueryStat
            {
                Query = q.Query,
                Count = q.Count,
                LastQueried = q.LastQueried
            })
        };
    }
}