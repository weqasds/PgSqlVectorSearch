using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;

namespace DomainService.ServiceContract.Repositories;

public interface IRagGenerationRepository : IRepository<RagGeneration>
{
    // Analytics methods
    Task<int> GetCountByDateRangeAsync(DateOnly start, DateOnly end);
    Task<int> GetCountByDateAsync(DateOnly date);
    Task<int> GetCountByKnowledgeBaseIdAsync(Guid knowledgeBaseId);
    Task<DateTime?> GetLastQueryTimeByKnowledgeBaseIdAsync(Guid knowledgeBaseId);
    Task<IEnumerable<QueryStat>> GetTopQueriesByKnowledgeBaseIdAsync(Guid knowledgeBaseId, int topN);
    Task<IEnumerable<QueryStat>> GetPopularQueriesAsync(int topN);
}