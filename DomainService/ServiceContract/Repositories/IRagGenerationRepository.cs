using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;

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