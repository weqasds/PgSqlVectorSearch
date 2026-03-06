using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities;
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
        return MapToDto(knowledgeBase);
    }

    public async Task<KnowledgeBaseDto> GetKnowledgeBaseAsync(Guid id)
    {
        var knowledgeBase = await _knowledgeBaseService.GetKnowledgeBaseAsync(id);
        return MapToDto(knowledgeBase);
    }

    public async Task<PagedResult<KnowledgeBaseDto>> ListKnowledgeBasesAsync(int page, int pageSize)
    {
        var allKnowledgeBases = await _knowledgeBaseService.ListKnowledgeBasesAsync();
        var totalCount = allKnowledgeBases.Count();
        var items = allKnowledgeBases
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto)
            .ToList();

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

    private KnowledgeBaseDto MapToDto(KnowledgeBase knowledgeBase)
    {
        // Note: DocumentCount needs to be retrieved from repository
        // For simplicity, we'll set to 0 initially; could be enhanced with caching or direct query
        return new KnowledgeBaseDto
        {
            Id = knowledgeBase.Id,
            Name = knowledgeBase.Name,
            Description = knowledgeBase.Description,
            CreatedAt = knowledgeBase.CreatedAt,
            DocumentCount = 0 // Will be updated when needed
        };
    }
}