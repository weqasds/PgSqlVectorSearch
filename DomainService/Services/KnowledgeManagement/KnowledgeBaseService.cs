using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract;

namespace DomainService.Services.KnowledgeManagement;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;

    public KnowledgeBaseService(IKnowledgeBaseRepository knowledgeBaseRepository)
    {
        _knowledgeBaseRepository = knowledgeBaseRepository;
    }

    public async Task<KnowledgeBase> CreateKnowledgeBaseAsync(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("知识库名称不能为空", nameof(name));

        // 检查名称是否已存在
        var exists = await _knowledgeBaseRepository.ExistsByNameAsync(name);
        if (exists)
            throw new InvalidOperationException($"知识库名称 '{name}' 已存在");

        var knowledgeBase = new KnowledgeBase
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _knowledgeBaseRepository.AddAsync(knowledgeBase);
        return knowledgeBase;
    }

    public async Task<KnowledgeBase> GetKnowledgeBaseAsync(Guid id)
    {
        var knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(id);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {id} 的知识库");

        return knowledgeBase;
    }

    public async Task<IEnumerable<KnowledgeBase>> ListKnowledgeBasesAsync()
    {
        var allBases = await _knowledgeBaseRepository.GetAllAsync();
        var activeBases = new List<KnowledgeBase>();

        foreach (var kb in allBases)
        {
            if (!kb.IsDeleted)
                activeBases.Add(kb);
        }

        return activeBases;
    }

    public async Task DeleteKnowledgeBaseAsync(Guid id)
    {
        var knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(id);
        if (knowledgeBase == null || knowledgeBase.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {id} 的知识库");

        knowledgeBase.IsDeleted = true;
        await _knowledgeBaseRepository.UpdateAsync(knowledgeBase);
    }
}