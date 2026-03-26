using Application.ApplicationServiceContract.CrossCutting;
using Application.ApplicationServiceContract.DocumentParsing;
using Application.ServiceContract.CrossCutting;
using DomainService.ServiceContract.CrossCutting;
using Infrastructure.InfrastructureShared.DocumentParser;
using Infrastructure.InfrastructureShared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.InfrastructureShared.Extension;

/// <summary>
/// InfrastructureShared基础设施层服务注册扩展方法
/// </summary>
public static class InfrastructureSharedServiceCollectionExtensions
{
    /// <summary>
    /// 注册InfrastructureShared基础设施层的所有服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddInfrastructureSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<PythonDocumentParserOptions>()
            .Bind(configuration.GetSection(PythonDocumentParserOptions.SectionName));

        services.AddHttpClient<PythonDocumentParserClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PythonDocumentParserOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl), UriKind.Absolute);
            }

            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
        });

        // 注册Redis连接
        var redisConnectionString = configuration.GetSection("Redis:ConnectionString").Value ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        // 1. 注册跨领域服务
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISystemMetricsCollector, SystemMetricsCollector>();

        // 2. 注册基础设施服务
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ITaskQueueService, RedisTaskQueueService>(); // 使用Redis实现
        services.AddScoped<IAuditLogService, AuditLogService>();

        // 3. 注册文档解析器
        services.AddScoped<IDocumentParser, PlainTextDocumentParser>();
        services.AddScoped<IDocumentParser, PythonDocumentParser>();
        services.AddScoped<IDocumentParser, PdfDocumentParser>();
        services.AddScoped<IDocumentParser, WordDocumentParser>();
        services.AddScoped<IDocumentParser, ExcelDocumentParser>();
        services.AddScoped<IDocumentParser, PowerPointDocumentParser>();
        // 注意：同一个接口的多个实现会以集合形式注入，可通过 IEnumerable<IDocumentParser> 获取所有解析器。
        services.AddScoped<IDocumentParserSelector, DocumentParserSelector>();
        services.AddScoped<RequestTrackingService>();
        return services;
    }

    private static string EnsureTrailingSlash(string baseUrl)
    {
        return baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/";
    }
}
