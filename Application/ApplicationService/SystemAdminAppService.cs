using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.ServiceContract;
using Application.ApplicationServiceContract;

namespace Application.ApplicationService;

public class SystemAdminAppService : ISystemAdminAppService
{
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IKnowledgeChunkRepository _knowledgeChunkRepository;
    private readonly IUserRepository _userRepository;
    private readonly IChatSessionRepository _chatSessionRepository;

    public SystemAdminAppService(
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IKnowledgeChunkRepository knowledgeChunkRepository,
        IUserRepository userRepository,
        IChatSessionRepository chatSessionRepository)
    {
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _knowledgeChunkRepository = knowledgeChunkRepository;
        _userRepository = userRepository;
        _chatSessionRepository = chatSessionRepository;
    }

    public async Task<SystemStatusDto> GetSystemStatusAsync()
    {
        // Check database connectivity
        bool databaseConnected = await IsDatabaseConnectedAsync();

        // Get counts
        var totalDocuments = await _knowledgeDocumentRepository.GetTotalCountAsync();
        var totalChunks = await _knowledgeChunkRepository.GetTotalCountAsync();
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
        // In a real implementation, query metrics from monitoring system
        // For now, return mock data
        var requestMetrics = new List<RequestMetric>
        {
            new RequestMetric { Endpoint = "/api/chat/messages", RequestCount = 150, AverageResponseTime = 245.5, ErrorCount = 2 },
            new RequestMetric { Endpoint = "/api/knowledge-bases", RequestCount = 80, AverageResponseTime = 120.3, ErrorCount = 0 },
            new RequestMetric { Endpoint = "/api/documents", RequestCount = 45, AverageResponseTime = 350.8, ErrorCount = 1 }
        };

        var processingMetrics = new List<ProcessingMetric>
        {
            new ProcessingMetric { Operation = "DocumentProcessing", Count = 25, AverageDuration = 1250.2 },
            new ProcessingMetric { Operation = "VectorGeneration", Count = 120, AverageDuration = 85.7 },
            new ProcessingMetric { Operation = "RAGGeneration", Count = 150, AverageDuration = 320.4 }
        };

        var databaseMetrics = new DatabaseMetrics
        {
            ConnectionCount = 8,
            QueryCount = 1250,
            CacheHitRatio = 0.92
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
            await _chatSessionRepository.DeleteAsync(session.Id);
        }

        // Delete old RAG generation logs (if applicable)
        // This would require a repository for RagGeneration and RagRetrievalLog
        // For now, we'll skip
    }

    private async Task<bool> IsDatabaseConnectedAsync()
    {
        // For simplicity, assume database is connected
        // In a real implementation, you might inject a health check service
        await Task.CompletedTask;
        return true;
    }
}