using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities.Chat;
using DomainService.Entities.KnowledgeManagement;

namespace DomainService.ServiceContract.CrossCutting;

/// <summary>
/// 提示模板服务接口
/// 负责构建和优化RAG提示
/// </summary>
public interface IPromptTemplateService
{
    /// <summary>
    /// 构建RAG提示
    /// </summary>
    Task<string> BuildRagPromptAsync(
        string query,
        IEnumerable<KnowledgeChunk> context,
        ChatSession? history = null);

    /// <summary>
    /// 构建系统提示
    /// </summary>
    Task<string> BuildSystemPromptAsync(string role, string instructions);

    /// <summary>
    /// 获取提示模板
    /// </summary>
    Task<string> GetTemplateAsync(string templateName);

    /// <summary>
    /// 注册自定义模板
    /// </summary>
    Task RegisterTemplateAsync(string templateName, string templateContent);

    /// <summary>
    /// 优化上下文长度，确保不超过token限制
    /// </summary>
    Task<IEnumerable<KnowledgeChunk>> OptimizeContextAsync(
        IEnumerable<KnowledgeChunk> context,
        int maxTokens);
}