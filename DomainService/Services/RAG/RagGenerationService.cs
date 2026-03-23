using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;

namespace DomainService.Services.RAG;

public class RagGenerationService : IRagGenerationService
{
    private readonly IVectorRetrievalService _vectorRetrievalService;
    private readonly IChatService _chatService;
    private readonly ILLMService _llmService;
    private readonly IRagGenerationRepository _ragGenerationRepository;
    private readonly IRagRetrievalLogRepository _ragRetrievalLogRepository;

    public RagGenerationService(
        IVectorRetrievalService vectorRetrievalService,
        IChatService chatService,
        ILLMService llmService,
        IRagGenerationRepository ragGenerationRepository,
        IRagRetrievalLogRepository ragRetrievalLogRepository)
    {
        _vectorRetrievalService = vectorRetrievalService;
        _chatService = chatService;
        _llmService = llmService;
        _ragGenerationRepository = ragGenerationRepository;
        _ragRetrievalLogRepository = ragRetrievalLogRepository;
    }

    public async Task<RagResponse> GenerateResponseAsync(string query, Guid? sessionId = null, Guid? knowledgeBaseId = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("查询内容不能为空", nameof(query));

        // 检索相关文档块，支持知识库过滤
        var retrievalResults = await _vectorRetrievalService.SearchSimilarChunksAsync(
            query,
            knowledgeBaseId: knowledgeBaseId);

        // 生成响应
        var response = await GenerateResponseWithContextAsync(query, retrievalResults);

        // 如果有会话ID，将消息添加到会话
        if (sessionId.HasValue)
        {
            await _chatService.AddMessageAsync(sessionId.Value, query, ChatRole.User);
            await _chatService.AddMessageAsync(sessionId.Value, response.Answer, ChatRole.Assistant);
        }

        // 记录检索日志
        var retrievalLogs = retrievalResults.Select((result, index) => new RetrievalLog
        {
            ChunkId = result.Chunk.Id,
            Score = result.SimilarityScore,
            Rank = index + 1,
            RetrievedAt = DateTime.UtcNow
        });

        // 保存生成记录
        var ragGeneration = new RagGeneration
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId ?? Guid.Empty,
            Prompt = query,
            Answer = response.Answer
        };

        await _ragGenerationRepository.AddAsync(ragGeneration);

        // 保存检索日志
        await LogRetrievalAsync(ragGeneration.Id, retrievalLogs);

        return response;
    }

    public async Task<RagResponse> GenerateResponseWithContextAsync(string query, IEnumerable<RetrievalResult> retrievalResults)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("查询内容不能为空", nameof(query));

        if (retrievalResults == null)
            throw new ArgumentNullException(nameof(retrievalResults));

        var resultsList = retrievalResults.ToList();
        if (resultsList.Count == 0)
            throw new ArgumentException("检索结果不能为空", nameof(retrievalResults));

        // 从检索结果中提取知识块构建上下文
        var context = resultsList.Select(r => r.Chunk).ToList();
        var prompt = BuildPrompt(query, context);
        var answer = await CallLargeLanguageModelAsync(prompt);

        return new RagResponse
        {
            Answer = answer,
            RetrievedChunks = resultsList, // 使用传入的检索结果，包含实际的相似度分数
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task LogRetrievalAsync(Guid generationId, IEnumerable<RetrievalLog> logs)
    {
        if (logs == null)
            throw new ArgumentNullException(nameof(logs));

        // 将检索日志转换为RagRetrievalLog实体并保存
        var ragRetrievalLogs = logs.Select(log => new RagRetrievalLog
        {
            Id = Guid.NewGuid(),
            RagGenerationId = generationId,
            ChunkId = log.ChunkId,
            Score = (float)log.Score
        });

        await _ragRetrievalLogRepository.AddRangeAsync(ragRetrievalLogs);
    }

    private string BuildPrompt(string query, List<KnowledgeChunk> context)
    {
        var contextText = string.Join("\n\n", context.Select((c, i) => $"[文档片段 {i + 1}]:\n{c.Content}"));

        return $@"请基于以下上下文信息回答用户的问题。

上下文信息：
{contextText}

用户问题：{query}

请提供准确、有用的回答，并注明信息来源于上述上下文。如果上下文信息不足以回答问题，请说明这一点。";
    }

    private async Task<string> CallLargeLanguageModelAsync(string prompt)
    {
        // 调用真实AI服务
        return await _llmService.GenerateResponseAsync(prompt);
    }
}