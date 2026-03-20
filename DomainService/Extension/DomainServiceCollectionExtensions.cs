using Microsoft.Extensions.DependencyInjection;
using DomainService.ServiceContract.Services;
using DomainService.Services.Auth;
using DomainService.Services.Chat;
using DomainService.Services.KnowledgeManagement;
using DomainService.Services.RAG;
using DomainService.Services.UserManagement;
using DomainService.Services.VectorRetrieval;
using DomainService.ServiceContract.CrossCutting;
namespace DomainService.Extension;

/// <summary>
/// 扩展方法，用于将领域层的服务实现注册到依赖注入容器中。
/// </summary>
public static class DomainServiceCollectionExtensions
{
    /// <summary>
    /// 注册领域层的服务实现（Domain Services）。
    /// </summary>
    /// <param name="services">依赖注入服务集合。</param>
    /// <returns>返回相同的服务集合，以便进行链式调用。</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // 领域服务
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IPromptTemplateService, PromptTemplateService>();
        services.AddScoped<IRagGenerationService, RagGenerationService>();
        services.AddScoped<IVectorRetrievalService, VectorRetrievalService>();
        services.AddScoped<IKnowledgeBasePermissionService, KnowledgeBasePermissionService>();

        return services;
    }
}
