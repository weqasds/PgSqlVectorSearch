using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;
using Application.ApplicationServiceContract;

namespace Application.ApplicationService;

public class KnowledgeBaseAppService : IKnowledgeBaseAppService
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly IDocumentProcessingService _documentProcessingService;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;

    public KnowledgeBaseAppService(
        IKnowledgeBaseService knowledgeBaseService,
        IDocumentProcessingService documentProcessingService,
        IKnowledgeDocumentRepository knowledgeDocumentRepository)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _documentProcessingService = documentProcessingService;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
    }

    public async Task<KnowledgeBaseDto> CreateKnowledgeBaseAsync(CreateKnowledgeBaseRequest request)
    {
        var knowledgeBase = await _knowledgeBaseService.CreateKnowledgeBaseAsync(request.Name, request.Description);
        return await MapToDtoAsync(knowledgeBase);
    }

    public async Task<KnowledgeBaseDto> GetKnowledgeBaseAsync(Guid id)
    {
        var knowledgeBase = await _knowledgeBaseService.GetKnowledgeBaseAsync(id);
        return await MapToDtoAsync(knowledgeBase);
    }

    public async Task<PagedResult<KnowledgeBaseDto>> ListKnowledgeBasesAsync(int page, int pageSize)
    {
        var allKnowledgeBases = await _knowledgeBaseService.ListKnowledgeBasesAsync();
        var totalCount = allKnowledgeBases.Count();

        var pagedKnowledgeBases = allKnowledgeBases
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Convert each knowledge base to DTO asynchronously
        var items = new List<KnowledgeBaseDto>();
        foreach (var knowledgeBase in pagedKnowledgeBases)
        {
            var dto = await MapToDtoAsync(knowledgeBase);
            items.Add(dto);
        }

        return new PagedResult<KnowledgeBaseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task UploadDocumentAsync(Guid knowledgeBaseId, UploadDocumentRequest request)
    {
        // Validate knowledge base exists
        await _knowledgeBaseService.GetKnowledgeBaseAsync(knowledgeBaseId);

        // Upload document via document processing service
        await _documentProcessingService.UploadDocumentAsync(
            knowledgeBaseId,
            request.File.Content,
            request.FileName ?? request.File.FileName);
    }

    public async Task ProcessKnowledgeBaseAsync(Guid knowledgeBaseId)
    {
        // Get all documents in the knowledge base
        var documents = await _knowledgeDocumentRepository.GetByKnowledgeBaseIdAsync(knowledgeBaseId);

        // Process each document asynchronously
        var processingTasks = documents.Select(doc =>
            _documentProcessingService.ProcessDocumentAsync(doc.Id));

        await Task.WhenAll(processingTasks);
    }

    private async Task<KnowledgeBaseDto> MapToDtoAsync(KnowledgeBase knowledgeBase)
    {
        // Get actual document count from repository
        var documentCount = await _knowledgeDocumentRepository.GetDocumentCountAsync(knowledgeBase.Id);

        return new KnowledgeBaseDto
        {
            Id = knowledgeBase.Id,
            Name = knowledgeBase.Name,
            Description = knowledgeBase.Description,
            CreatedAt = knowledgeBase.CreatedAt,
            DocumentCount = documentCount
        };
    }
}