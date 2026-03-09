using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Chat;
using DomainService.Entities.KnowledgeManagement;
using DomainService.ServiceContract.CrossCutting;

namespace DomainService.Services.KnowledgeManagement;

/// <summary>
/// 提示模板服务实现
/// 负责构建和优化RAG提示
/// </summary>
public class PromptTemplateService : IPromptTemplateService
{
    private readonly Dictionary<string, string> _templates;

    public PromptTemplateService()
    {
        _templates = new Dictionary<string, string>
        {
            ["rag_default"] = @"请基于以下上下文信息回答用户的问题。

上下文信息：
{context}

用户问题：{query}

请提供准确、有用的回答，并注明信息来源于上述上下文。如果上下文信息不足以回答问题，请说明这一点。",

            ["system_default"] = @"你是一个有帮助的AI助手，基于提供的上下文信息回答问题。",

            ["rag_with_history"] = @"以下是对话历史：
{history}

请基于以下上下文信息回答用户的最新问题。

上下文信息：
{context}

用户最新问题：{query}

请提供准确、有用的回答，并注明信息来源于上述上下文。如果上下文信息不足以回答问题，请说明这一点。"
        };
    }

    public async Task<string> BuildRagPromptAsync(
        string query,
        IEnumerable<KnowledgeChunk> context,
        ChatSession? history = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("查询不能为空", nameof(query));

        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var contextList = context.ToList();
        if (!contextList.Any())
            throw new ArgumentException("上下文不能为空", nameof(context));

        // 构建上下文文本
        var contextText = BuildContextText(contextList);

        // 选择模板
        string template;
        if (history != null && history.Messages.Any())
        {
            template = _templates["rag_with_history"];
            var historyText = BuildHistoryText(history.Messages.ToList());
            return template
                .Replace("{history}", historyText)
                .Replace("{context}", contextText)
                .Replace("{query}", query);
        }
        else
        {
            template = _templates["rag_default"];
            return template
                .Replace("{context}", contextText)
                .Replace("{query}", query);
        }
    }

    public async Task<string> BuildSystemPromptAsync(string role, string instructions)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("角色不能为空", nameof(role));

        var baseTemplate = _templates["system_default"];
        var prompt = $"{baseTemplate}\n\n角色：{role}";

        if (!string.IsNullOrWhiteSpace(instructions))
        {
            prompt += $"\n\n附加指令：{instructions}";
        }

        return await Task.FromResult(prompt);
    }

    public async Task<string> GetTemplateAsync(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("模板名称不能为空", nameof(templateName));

        if (_templates.TryGetValue(templateName, out var template))
        {
            return await Task.FromResult(template);
        }

        throw new KeyNotFoundException($"未找到名称为 '{templateName}' 的模板");
    }

    public async Task RegisterTemplateAsync(string templateName, string templateContent)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("模板名称不能为空", nameof(templateName));

        if (string.IsNullOrWhiteSpace(templateContent))
            throw new ArgumentException("模板内容不能为空", nameof(templateContent));

        _templates[templateName] = templateContent;
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<KnowledgeChunk>> OptimizeContextAsync(
        IEnumerable<KnowledgeChunk> context,
        int maxTokens)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (maxTokens <= 0)
            throw new ArgumentException("最大token数必须大于0", nameof(maxTokens));

        var contextList = context.ToList();
        if (!contextList.Any())
            return Enumerable.Empty<KnowledgeChunk>();

        // 简单的优化策略：按内容长度排序，优先选择短的内容
        // 实际实现应该考虑内容的相关性和重要性
        var optimized = new List<KnowledgeChunk>();
        var currentTokens = 0;

        // 按内容长度排序（短的优先）
        var sortedChunks = contextList.OrderBy(c => c.Content?.Length ?? 0);

        foreach (var chunk in sortedChunks)
        {
            var chunkTokens = EstimateTokenCount(chunk.Content ?? "");
            if (currentTokens + chunkTokens <= maxTokens)
            {
                optimized.Add(chunk);
                currentTokens += chunkTokens;
            }
            else
            {
                // 如果添加当前块会超过限制，尝试截断
                if (chunkTokens > 0)
                {
                    var remainingTokens = maxTokens - currentTokens;
                    if (remainingTokens > 100) // 至少保留100个token才有意义
                    {
                        var truncatedChunk = TruncateChunk(chunk, remainingTokens);
                        optimized.Add(truncatedChunk);
                    }
                }
                break;
            }
        }

        return await Task.FromResult(optimized);
    }

    private string BuildContextText(List<KnowledgeChunk> context)
    {
        var textParts = new List<string>();
        for (int i = 0; i < context.Count; i++)
        {
            var chunk = context[i];
            var chunkText = $"[文档片段 {i + 1}]:\n{chunk.Content}";
            textParts.Add(chunkText);
        }

        return string.Join("\n\n", textParts);
    }

    private string BuildHistoryText(List<ChatMessage> messages)
    {
        var historyParts = new List<string>();
        foreach (var message in messages)
        {
            var roleText = message.Role == ChatRole.User ? "用户" : "助手";
            historyParts.Add($"{roleText}: {message.Content}");
        }

        return string.Join("\n", historyParts);
    }

    private int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // 简单的token估算
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

    private KnowledgeChunk TruncateChunk(KnowledgeChunk chunk, int maxTokens)
    {
        // 简单的截断逻辑：按字符数截断
        // 实际实现应该考虑句子边界和token边界
        var content = chunk.Content ?? "";
        var estimatedChars = maxTokens * 3; // 粗略估计：每个token约3个字符

        if (content.Length <= estimatedChars)
            return chunk;

        var truncatedContent = content.Substring(0, estimatedChars) + "... [已截断]";
        return new KnowledgeChunk
        {
            Id = chunk.Id,
            DocumentId = chunk.DocumentId,
            ChunkIndex = chunk.ChunkIndex,
            Content = truncatedContent,
            CreatedAt = chunk.CreatedAt,
            IsDeleted = chunk.IsDeleted
        };
    }
}