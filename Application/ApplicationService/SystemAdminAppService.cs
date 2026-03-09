using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;
using Application.ApplicationServiceContract;
using Application.QueryServices;

namespace Application.ApplicationService;

public class SystemAdminAppService : ISystemAdminAppService
{
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IKnowledgeChunkQueryService _knowledgeChunkQueryService;
    private readonly IUserRepository _userRepository;
    private readonly IChatSessionRepository _chatSessionRepository;

    public SystemAdminAppService(
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IKnowledgeChunkQueryService knowledgeChunkQueryService,
        IUserRepository userRepository,
        IChatSessionRepository chatSessionRepository)
    {
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _knowledgeChunkQueryService = knowledgeChunkQueryService;
        _userRepository = userRepository;
        _chatSessionRepository = chatSessionRepository;
    }

    public async Task<SystemStatusDto> GetSystemStatusAsync()
    {
        // Check database connectivity
        bool databaseConnected = await IsDatabaseConnectedAsync();

        // Get counts
        var totalDocuments = await _knowledgeDocumentRepository.GetTotalCountAsync();
        var totalChunks = await _knowledgeChunkQueryService.GetTotalCountAsync();
        var activeUsers = await _userRepository.GetActiveUserCountAsync();

        // For simplicity, assume vector search is available if database is connected
        bool vectorSearchAvailable = databaseConnected;

        // Simulate system load (in a real implementation, use performance counters)
        var load = new SystemLoad
        {
            CpuUsage = 0.15, // 15%
            MemoryUsage = 0.45, // 45%
            ActiveThreads = 12
        };

        return new SystemStatusDto
        {
            DatabaseConnected = databaseConnected,
            VectorSearchAvailable = vectorSearchAvailable,
            ActiveUsers = activeUsers,
            TotalDocuments = totalDocuments,
            TotalChunks = totalChunks,
            Load = load
        };
    }

    public async Task<SystemMetricsDto> GetSystemMetricsAsync(TimeRange range)
    {
        // Get actual system data from repositories
        var totalDocuments = await _knowledgeDocumentRepository.GetTotalCountAsync();
        var totalChunks = await _knowledgeChunkQueryService.GetTotalCountAsync();
        var totalUsers = await _userRepository.GetTotalCountAsync();
        var totalSessions = await _chatSessionRepository.GetTotalCountAsync();

        // Note: Real request metrics would require API monitoring/logging system
        // For now, return mock data with actual counts integrated
        var requestMetrics = new List<RequestMetric>
        {
            new RequestMetric { Endpoint = "/api/chat/messages", RequestCount = 150, AverageResponseTime = 245.5, ErrorCount = 2 },
            new RequestMetric { Endpoint = "/api/knowledge-bases", RequestCount = 80, AverageResponseTime = 120.3, ErrorCount = 0 },
            new RequestMetric { Endpoint = "/api/documents", RequestCount = 45, AverageResponseTime = 350.8, ErrorCount = 1 }
        };

        // Processing metrics with actual counts where possible
        var processingMetrics = new List<ProcessingMetric>
        {
            new ProcessingMetric { Operation = "DocumentProcessing", Count = totalDocuments, AverageDuration = 1250.2 },
            new ProcessingMetric { Operation = "VectorGeneration", Count = totalChunks, AverageDuration = 85.7 },
            new ProcessingMetric { Operation = "RAGGeneration", Count = 150, AverageDuration = 320.4 } // Still mock
        };

        // Database metrics - using actual query counts from repositories
        var databaseMetrics = new DatabaseMetrics
        {
            ConnectionCount = 8, // Would require connection pool monitoring
            QueryCount = totalDocuments + totalChunks + totalUsers + totalSessions, // Approximate total queries
            CacheHitRatio = 0.92 // Would require database performance counters
        };

        return new SystemMetricsDto
        {
            RequestMetrics = requestMetrics,
            ProcessingMetrics = processingMetrics,
            DatabaseMetrics = databaseMetrics
        };
    }

    public async Task CleanupOldDataAsync(DateTime cutoffDate)
    {
        // Delete old chat sessions and messages
        var oldSessions = await _chatSessionRepository.GetSessionsOlderThanAsync(cutoffDate);
        foreach (var session in oldSessions)
        {
            // Delete associated messages first (assuming cascade delete not configured)
            var messages = await _chatSessionRepository.GetMessagesBySessionIdAsync(session.Id);
            foreach (var message in messages)
            {
                await _chatSessionRepository.DeleteMessageAsync(message.Id);
            }
            await _chatSessionRepository.DeleteAsync(session);
        }

        // Delete old RAG generation logs (if applicable)
        // This would require a repository for RagGeneration and RagRetrievalLog
        // For now, we'll skip
    }

    private async Task<bool> IsDatabaseConnectedAsync()
    {
        try
        {
            // Try to execute a simple query to check database connectivity
            var count = await _knowledgeDocumentRepository.GetTotalCountAsync();
            return true; // If no exception, database is connected
        }
        catch (Exception ex)
        {
            // Log the connection error (in a real implementation, use ILogger)
            Console.WriteLine($"Database connection check failed: {ex.Message}");
            return false;
        }
    }
}