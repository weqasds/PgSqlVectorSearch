using Application.QueryServices;
using Application.ApplicationServiceContract.CrossCutting;
using DomainService.Entities.System;
using DomainService.Entities.VectorSearch;
using DomainService.ServiceContract.CrossCutting;
using DomainService.ServiceContract.Repositories;
using Infrastructure.EFCoreInfrastructure.QueryServices;
using Infrastructure.EFCoreInfrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.EFCoreInfrastructure.Extensions;

/// <summary>
/// EFCore基础设施层服务注册扩展方法
/// </summary>
public static class EFCoreInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// 注册EFCore基础设施层的所有服务（仓储和查询服务）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddEFCoreInfrastructureServices(this IServiceCollection services)
    {
        // 1. 注册仓储接口实现
        services.AddScoped<IKnowledgeBaseRepository, KnowledgeBaseRepository>();
        services.AddScoped<IKnowledgeDocumentRepository, KnowledgeDocumentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        //services.AddScoped<IRepository<Embedding>, EmbeddingRepository>();
        services.AddScoped<IVectorStore, EmbeddingRepository>();
        services.AddScoped<IRagGenerationRepository, RagGenerationRepository>();
        services.AddScoped<IRagRetrievalLogRepository, RagRetrievalLogRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // 2. 注册查询服务实现
        //uof单元注册
        //保证无论是使用IUnitOfWork还是使用上下文都是同一个实例
        services.AddScoped<IUnitOfWork>(sp=> sp.GetRequiredService<RagDbContext>());
        //通用查询服务注册
        services.AddScoped<IQueryService<Embedding>, VectorSearchQueryService>();
        services.AddScoped<IQueryService<AuditLog>, AuditLogQueryService>();

        services.AddScoped<IVectorSearchQueryService, VectorSearchQueryService>();
        services.AddScoped<IKnowledgeBaseQueryService, KnowledgeBaseQueryService>();
        services.AddScoped<IKnowledgeChunkQueryService, KnowledgeChunkQueryService>();
        return services;
    }
}

