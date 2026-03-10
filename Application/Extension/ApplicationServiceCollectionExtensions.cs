using Application.ApplicationService;
using Application.ApplicationServiceContract;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

/// <summary>
/// 应用层服务注册扩展方法
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// 注册所有应用层服务（接口和实现都在应用层内部的服务）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 注册所有应用服务（接口和实现都在Application项目内）
        services.AddScoped<ISystemAdminAppService, SystemAdminAppService>();
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IRagChatAppService, RagChatAppService>();
        services.AddScoped<IKnowledgeBaseAppService, KnowledgeBaseAppService>();
        services.AddScoped<IDocumentManagementAppService, DocumentManagementAppService>();
        services.AddScoped<IBatchOperationAppService, BatchOperationAppService>();
        services.AddScoped<IAnalyticsAppService, AnalyticsAppService>();

        return services;
    }
}