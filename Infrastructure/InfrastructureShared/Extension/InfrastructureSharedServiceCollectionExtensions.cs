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
        services
            .AddOptions<MinerUDocumentParserOptions>()
            .Bind(configuration.GetSection(MinerUDocumentParserOptions.SectionName));
        services
            .AddOptions<RedisInfrastructureOptions>()
            .Bind(configuration.GetSection(RedisInfrastructureOptions.SectionName));

        services.AddHttpClient<PythonDocumentParserClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PythonDocumentParserOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl), UriKind.Absolute);
            }

            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
        });

        services.AddHttpClient<MinerUDocumentParserClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MinerUDocumentParserOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl), UriKind.Absolute);
            }

            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
        });

        // 1. 注册跨领域服务
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISystemMetricsCollector, SystemMetricsCollector>();

        // 2. 注册基础设施服务
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        RegisterTaskQueueServices(services, configuration);
        services.AddScoped<IAuditLogService, AuditLogService>();

        // 3. 注册文档解析器
        services.AddScoped<IDocumentParser, PlainTextDocumentParser>();
        services.AddScoped<IDocumentParser, MinerUDocumentParser>();
        services.AddScoped<IDocumentParser, PythonDocumentParser>();
        services.AddScoped<IDocumentParser, PdfDocumentParser>();
        services.AddScoped<IDocumentParser, WordDocumentParser>();
        services.AddScoped<IDocumentParser, ExcelDocumentParser>();
        services.AddScoped<IDocumentParser, PowerPointDocumentParser>();
        // 注意：同一个接口的多个实现会以集合形式注入，可通过 IEnumerable<IDocumentParser> 获取所有解析器。
        services.AddScoped<IDocumentParserSelector, DocumentParserSelector>();
        services.AddSingleton<RequestTrackingService>();
        return services;
    }

    private static string EnsureTrailingSlash(string baseUrl)
    {
        return baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/";
    }

    private static void RegisterTaskQueueServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new RedisInfrastructureOptions();
        configuration.GetSection(RedisInfrastructureOptions.SectionName).Bind(options);

        if (!options.Enabled)
        {
            services.AddScoped<ITaskQueueService, TaskQueueService>();
            return;
        }

        try
        {
            var redisOptions = ConfigurationOptions.Parse(options.ConnectionString, true);
            redisOptions.AbortOnConnectFail = false;
            redisOptions.ConnectRetry = 1;
            redisOptions.ConnectTimeout = Math.Max(500, options.ConnectTimeoutMs);

            var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
            if (!multiplexer.IsConnected)
            {
                Console.WriteLine("Redis 已启用但当前不可用，任务队列将回退到内存实现。");
                services.AddScoped<ITaskQueueService, TaskQueueService>();
                return;
            }

            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            services.AddScoped<ITaskQueueService, RedisTaskQueueService>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Redis 连接失败，任务队列将回退到内存实现。错误: {ex.Message}");
            services.AddScoped<ITaskQueueService, TaskQueueService>();
        }
    }
}
