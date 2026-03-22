using DomainService.ServiceContract.CrossCutting;
using Infrastructure.SemanticKernelAI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Application.ApplicationServiceContract.AI;

namespace Infrastructure.SemanticKernelAI.Extensions;

/// <summary>
/// SemanticKernelAI基础设施层服务注册扩展方法
/// </summary>
public static class SemanticKernelAIServiceCollectionExtensions
{
    /// <summary>
    /// 注册SemanticKernelAI基础设施层的所有服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddSemanticKernelAIServices(this IServiceCollection services)
    {
        //services.
        // 注册语义内核服务
#pragma warning disable
        services.AddKernel().
            AddOpenAIChatCompletion("deepseek-chat", new Uri("https://api.deepseek.com"), "sk-68f5ad488a6f45bba3d695b8098859f0").
            AddOpenAIEmbeddingGenerator(
            "Qwen/Qwen3-Embedding-0.6B",
            "sk-1233",
            httpClient: new HttpClient()
            {
                BaseAddress = new Uri("http://127.0.0.1:30000/v1")
            });
        // 2. 注册AI相关服务
        services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
        services.AddScoped<ILLMService, OpenAILLMService>();
        services.AddScoped<ISemanticKernelService, SemanticKernelService>();

        return services;
    }
}