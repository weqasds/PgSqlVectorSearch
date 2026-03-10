using Application.ApplicationServiceContract.CrossCutting;
using DomainService.ServiceContract.CrossCutting;
using InfrastructureShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InfrastructureShared.Extensions;

/// <summary>
/// InfrastructureShared基础设施层服务注册扩展方法
/// </summary>
public static class InfrastructureSharedServiceCollectionExtensions
{
    /// <summary>
    /// 注册InfrastructureShared基础设施层的所有服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddInfrastructureSharedServices(this IServiceCollection services)
    {
        
        // 1. 注册跨领域服务
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISystemMetricsCollector, SystemMetricsCollector>();

        // 2. 注册基础设施服务
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ITaskQueueService, TaskQueueService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }
}