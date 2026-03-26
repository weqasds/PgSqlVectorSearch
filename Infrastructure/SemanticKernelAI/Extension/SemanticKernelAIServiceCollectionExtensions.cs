using DomainService.ServiceContract.CrossCutting;
using Infrastructure.SemanticKernelAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Options;
using Application.ApplicationServiceContract.AI;
using System.Net.Http;

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
    /// <param name="configuration">配置</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddSemanticKernelAIServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 绑定AI配置
        var aiOptions = configuration.GetSection("AIOptions").Get<AIOptions>() ?? new AIOptions();

        // 注册AIOptions供其他服务使用
        services.Configure<AIOptions>(options =>
            configuration.GetSection("AIOptions").Bind(options));

        // 注册语义内核服务
        var kernelBuilder = services.AddKernel();

        // 根据配置注册AI服务
        if (aiOptions.DeepSeek.Enabled && !string.IsNullOrEmpty(aiOptions.DeepSeek.ApiKey))
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: aiOptions.DeepSeek.Model,
                endpoint: new Uri(aiOptions.DeepSeek.BaseUrl),
                apiKey: aiOptions.DeepSeek.ApiKey);
        }

        if (aiOptions.LocalEmbedding.Enabled)
        {
            #pragma warning disable
            kernelBuilder.AddOpenAIEmbeddingGenerator(
                modelId: aiOptions.LocalEmbedding.EmbeddingModel,
                apiKey: aiOptions.LocalEmbedding.ApiKey,
                httpClient: new HttpClient()
                {
                    BaseAddress = new Uri(aiOptions.LocalEmbedding.BaseUrl)
                });

        }

        if (aiOptions.OpenAI.Enabled && !string.IsNullOrEmpty(aiOptions.OpenAI.ApiKey))
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: aiOptions.OpenAI.Model,
                endpoint: new Uri(aiOptions.OpenAI.BaseUrl),
                apiKey: aiOptions.OpenAI.ApiKey);

            kernelBuilder.AddOpenAIEmbeddingGenerator(
                modelId: aiOptions.OpenAI.EmbeddingModel,
                apiKey: aiOptions.OpenAI.ApiKey,
                httpClient:new HttpClient()
                {
                    BaseAddress = new Uri(aiOptions.OpenAI.BaseUrl)
                }
            );

        }

        // 注册AI相关服务
        services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
        services.AddScoped<ILLMService, OpenAILLMService>();
        services.AddScoped<ISemanticKernelService, SemanticKernelService>();

        return services;
    }
}
