using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.ServiceContract.CrossCutting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Infrastructure.SemanticKernelAI.Services;

/// <summary>
/// OpenAI LLM服务
/// 使用OpenAI聊天完成API生成文本响应
/// </summary>
public class OpenAILLMService : ILLMService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;

    public OpenAILLMService(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<string> GenerateResponseAsync(string prompt, LLMOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("提示不能为空", nameof(prompt));

        try
        {
            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

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
                Temperature = (float)(options?.Temperature ?? 0.7),
                MaxTokens = options?.MaxTokens ?? 2000,
                TopP = (float)(options?.TopP ?? 1.0),
                FrequencyPenalty = (float)(options?.FrequencyPenalty ?? 0.0),
                PresencePenalty = (float)(options?.PresencePenalty ?? 0.0)
            };

            // 如果有指定模型名称，需要相应的处理
            // 注意：Semantic Kernel的模型选择通常在Kernel构建时配置

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

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

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
            Temperature = (float)(options?.Temperature ?? 0.7),
            MaxTokens = options?.MaxTokens ?? 2000,
            TopP = (float)(options?.TopP ?? 1.0),
            FrequencyPenalty = (float)(options?.FrequencyPenalty ?? 0.0),
            PresencePenalty = (float)(options?.PresencePenalty ?? 0.0)
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

    public async Task<int> CountTokensAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        try
        {
            // Semantic Kernel提供了Token计数功能
            // 这里使用简单的估算方法，实际应该使用相应的Tokenizer
            // 注意：这是一个简化实现，实际应该使用准确的tokenizer
            return await Task.Run(() => EstimateTokenCount(text));
        }
        catch
        {
            // 如果估算失败，返回保守估计
            return text.Length / 4; // 粗略估算：平均每个token约4个字符
        }
    }

    public async Task<IEnumerable<string>> GetSupportedModelsAsync()
    {
        try
        {
            // 这里应该调用API获取支持的模型列表
            // 由于OpenAI API没有直接的模型列表端点，我们返回常用模型
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

    private int EstimateTokenCount(string text)
    {
        // 简单的token估算：英文平均每个token约4个字符，中文约2个字符
        // 这是一个非常粗略的估算，实际应该使用相应的Tokenizer
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
            // 中文：大致每个汉字1-2个token
            return (int)(totalCharCount * 1.5);
        }
        else
        {
            // 英文：大致每4个字符1个token
            return (int)Math.Ceiling(totalCharCount / 4.0);
        }
    }
}