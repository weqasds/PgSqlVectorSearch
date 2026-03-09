using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.SemanticKernelAI.ServiceContract;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Pgvector;

namespace Infrastructure.SemanticKernelAI.Services;

/// <summary>
/// 语义内核服务实现
/// 封装Microsoft Semantic Kernel核心功能
/// </summary>
public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly string _embeddingModelName;
    private readonly int _embeddingDimension;

    public SemanticKernelService(
        Kernel kernel,
        string embeddingModelName = "text-embedding-3-small",
        int embeddingDimension = 1536)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        _embeddingModelName = embeddingModelName;
        _embeddingDimension = embeddingDimension;
    }

    public async Task InitializeAsync()
    {
        // 初始化语义内核
        // 这里可以执行任何需要的初始化逻辑
        await Task.CompletedTask;
    }

    public async Task<Vector> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("文本不能为空", nameof(text));

        try
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
            var vectorValues = embedding.ToArray();
            return new Vector(vectorValues);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"生成嵌入向量失败: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Vector>> GenerateEmbeddingsAsync(IEnumerable<string> texts)
    {
        if (texts == null)
            throw new ArgumentNullException(nameof(texts));

        var textList = texts.ToList();
        if (!textList.Any())
            return Enumerable.Empty<Vector>();

        try
        {
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(textList);

            var vectors = new List<Vector>();
            foreach (var embedding in embeddings)
            {
                var vectorValues = embedding.ToArray();
                vectors.Add(new Vector(vectorValues));
            }

            return vectors;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"批量生成嵌入向量失败: {ex.Message}", ex);
        }
    }

    public async Task<string> GenerateResponseAsync(string prompt, LLMOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("提示不能为空", nameof(prompt));

        try
        {
            var chatHistory = new ChatHistory();

            // 添加系统提示（如果有）
            if (!string.IsNullOrWhiteSpace(options?.SystemPrompt))
            {
                chatHistory.AddSystemMessage(options.SystemPrompt);
            }

            // 添加用户消息
            chatHistory.AddUserMessage(prompt);

            // 准备执行设置
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = options?.Temperature ?? 0.7f,
                MaxTokens = options?.MaxTokens ?? 2000
            };

            // 如果有指定模型名称，需要相应的处理
            if (!string.IsNullOrWhiteSpace(options?.ModelName))
            {
                // 在实际实现中，可能需要创建特定模型的chat service
                // 这里简化处理
            }

            // 生成响应
            var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings);
            return response.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"生成LLM响应失败: {ex.Message}", ex);
        }
    }

    public async IAsyncEnumerable<string> GenerateResponseStreamAsync(string prompt, LLMOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("提示不能为空", nameof(prompt));

        var chatHistory = new ChatHistory();

        // 添加系统提示（如果有）
        if (!string.IsNullOrWhiteSpace(options?.SystemPrompt))
        {
            chatHistory.AddSystemMessage(options.SystemPrompt);
        }

        // 添加用户消息
        chatHistory.AddUserMessage(prompt);

        // 准备执行设置
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = options?.Temperature ?? 0.7f,
            MaxTokens = options?.MaxTokens ?? 2000
        };

        // 流式响应
        await foreach (var content in _chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings))
        {
            if (!string.IsNullOrWhiteSpace(content.Content))
            {
                yield return content.Content;
            }
        }
    }

    public string GetEmbeddingModelName()
    {
        return _embeddingModelName;
    }

    public int GetEmbeddingDimension()
    {
        return _embeddingDimension;
    }

    public async Task<IEnumerable<string>> GetSupportedModelsAsync()
    {
        try
        {
            // 返回常用模型列表
            var models = new List<string>
            {
                "gpt-4",
                "gpt-4-turbo-preview",
                "gpt-3.5-turbo",
                "gpt-3.5-turbo-instruct"
            };

            return await Task.FromResult(models);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"获取支持的模型列表失败: {ex.Message}", ex);
        }
    }

    public async Task<int> CountTokensAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        try
        {
            // 简单的token估算
            return await Task.Run(() => EstimateTokenCount(text));
        }
        catch
        {
            // 如果估算失败，返回保守估计
            return text.Length / 4;
        }
    }

    private int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // 检查文本是否主要是中文
        int chineseCharCount = 0;
        int totalCharCount = text.Length;

        foreach (char c in text)
        {
            if (c >= 0x4E00 && c <= 0x9FFF) // 基本汉字范围
                chineseCharCount++;
        }

        // 如果超过30%的字符是中文，按中文处理
        if ((double)chineseCharCount / totalCharCount > 0.3)
        {
            return (int)(totalCharCount * 1.5);
        }
        else
        {
            return (int)Math.Ceiling(totalCharCount / 4.0);
        }
    }
}