using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;

namespace DomainService.ServiceContract.Repositories;

public interface IRagRetrievalLogRepository : IRepository<RagRetrievalLog>
{
    /// <summary>
    /// 获取早于指定日期的检索日志
    /// </summary>
    /// <param name="cutoffDate">截止日期</param>
    /// <returns>早于截止日期的日志列表</returns>
    Task<IEnumerable<RagRetrievalLog>> GetLogsOlderThanAsync(DateTime cutoffDate);

    /// <summary>
    /// 批量删除检索日志
    /// </summary>
    /// <param name="logs">要删除的日志列表</param>
    Task DeleteLogsAsync(IEnumerable<RagRetrievalLog> logs);

    /// <summary>
    /// 按日期范围统计日志数量
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>日志数量</returns>
    Task<int> GetCountByDateRangeAsync(DateTime startDate, DateTime endDate);
}