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

public class DocumentManagementAppService : IDocumentManagementAppService
{
    private readonly IDocumentProcessingService _documentProcessingService;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IKnowledgeChunkRepository _knowledgeChunkRepository;

    public DocumentManagementAppService(
        IDocumentProcessingService documentProcessingService,
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IKnowledgeChunkRepository knowledgeChunkRepository)
    {
        _documentProcessingService = documentProcessingService;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _knowledgeChunkRepository = knowledgeChunkRepository;
    }

    public async Task<DocumentDto> UploadDocumentAsync(Guid knowledgeBaseId, FileUpload file)
    {
        // Upload document via document processing service
        var document = await _documentProcessingService.UploadDocumentAsync(
            knowledgeBaseId,
            file.Content,
            file.FileName);

        // Map to DTO
        return MapToDto(document);
    }

    public async Task<IEnumerable<DocumentDto>> GetDocumentsAsync(Guid knowledgeBaseId)
    {
        var documents = await _knowledgeDocumentRepository.GetByKnowledgeBaseIdAsync(knowledgeBaseId);
        return documents.Select(MapToDto);
    }

    public async Task DeleteDocumentAsync(Guid documentId)
    {
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            throw new ArgumentException($"Document with id {documentId} not found.");
        }

        // Delete associated chunks first
        var chunks = await _knowledgeChunkRepository.GetByDocumentIdAsync(documentId);
        foreach (var chunk in chunks)
        {
            await _knowledgeChunkRepository.DeleteAsync(chunk);
        }

        // Delete document
        await _knowledgeDocumentRepository.DeleteAsync(document);
    }

    public async Task ReprocessDocumentAsync(Guid documentId)
    {
        var document = await _knowledgeDocumentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            throw new ArgumentException($"Document with id {documentId} not found.");
        }

        // Reprocess document via document processing service
        await _documentProcessingService.ProcessDocumentAsync(documentId);
    }

    private DocumentDto MapToDto(KnowledgeDocument document)
    {
        // ChunkCount can be retrieved from repository; for simplicity we'll set to 0
        return new DocumentDto
        {
            Id = document.Id,
            FileName = document.FileName,
            FilePath = document.FilePath,
            FileSize = document.FileSize,
            ContentType = document.ContentType,
            UploadedAt = document.CreatedAt,
            Status = document.Status.ToString(),
            ChunkCount = document.ChunkCount
        };
    }
}