using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.KnowledgeManagement;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using Application.ApplicationServiceContract;
using Application.ApplicationServiceContract.DocumentationService;
using ApplicationServiceContract.CrossCutting;

namespace Application.ApplicationService;

public class KnowledgeBaseAppService : IKnowledgeBaseAppService
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly IDocumentManagementAppService _documentManageAppService;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public KnowledgeBaseAppService(
        IKnowledgeBaseService knowledgeBaseService,
        IDocumentManagementAppService documentManagementAppService,
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IUnitOfWork unitOfWork)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _documentManageAppService = documentManagementAppService;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<KnowledgeBaseDto> CreateKnowledgeBaseAsync(CreateKnowledgeBaseRequest request)
    {
        var knowledgeBase = await _knowledgeBaseService.CreateKnowledgeBaseAsync(request.Name, request.Description);
        await _unitOfWork.SaveChangesAsync();
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

    public async Task UploadDocumentAsync(Guid knowledgeBaseId, FileUpload fileUpload)
    {
        // Validate knowledge base exists
        await _knowledgeBaseService.GetKnowledgeBaseAsync(knowledgeBaseId);

        // Upload document via document processing service
        await _documentManageAppService.UploadDocumentAsync(
            knowledgeBaseId,
            fileUpload
        );
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ProcessKnowledgeBaseAsync(Guid knowledgeBaseId)
    {
        // Get all documents in the knowledge base
        var documents = await _knowledgeDocumentRepository.GetByKnowledgeBaseIdAsync(knowledgeBaseId);

        // Process each document asynchronously
        var processingTasks = documents.Select(doc =>
            _documentManageAppService.ProcessDocumentAsync(doc.Id));
        //await _unitOfWork.SaveChangesAsync();
        await Task.WhenAll(processingTasks);
    }

    public async Task<KnowledgeBaseDto> UpdateKnowledgeBaseAsync(Guid id, CreateKnowledgeBaseRequest request)
    {
        var updatedKnowledgeBase = await _knowledgeBaseService.UpdateKnowledgeBaseAsync(id, request.Name, request.Description);
        await _unitOfWork.SaveChangesAsync();
        return await MapToDtoAsync(updatedKnowledgeBase);
    }

    public async Task DeleteKnowledgeBaseAsync(Guid id)
    {
        await _knowledgeBaseService.DeleteKnowledgeBaseAsync(id);
        await _unitOfWork.SaveChangesAsync();
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