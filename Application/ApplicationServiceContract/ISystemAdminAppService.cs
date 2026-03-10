using System;
using System.Threading.Tasks;

namespace Application.ApplicationServiceContract;

public interface ISystemAdminAppService
{
    Task<SystemStatusDto> GetSystemStatusAsync();
    Task<SystemMetricsDto> GetSystemMetricsAsync(TimeRange range);
    Task CleanupOldDataAsync(DateTimeOffset cutoffDate);
}