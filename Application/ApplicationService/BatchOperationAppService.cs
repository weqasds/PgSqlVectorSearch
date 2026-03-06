using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;
using Application.ApplicationServiceContract;

namespace Application.ApplicationService;

public class BatchOperationAppService : IBatchOperationAppService
{
    private readonly IDocumentProcessingService _documentProcessingService;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IFileStorageService _fileStorageService;

    public BatchOperationAppService(
        IDocumentProcessingService documentProcessingService,
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IFileStorageService fileStorageService)
    {
        _documentProcessingService = documentProcessingService;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<BatchImportResultDto> ImportDocumentsAsync(Guid knowledgeBaseId, FileUpload zipFile)
    {
        var results = new List<ImportResult>();
        int successCount = 0;
        int failureCount = 0;

        // Create a temporary directory to extract zip files
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Extract zip file
            using (var archive = new ZipArchive(zipFile.Content, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    // Skip directories
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var filePath = Path.Combine(tempDir, entry.Name);
                    try
                    {
                        // Extract entry
                        entry.ExtractToFile(filePath, overwrite: true);

                        // Read file as stream
                        using (var fileStream = File.OpenRead(filePath))
                        {
                            // Upload document
                            var document = await _documentProcessingService.UploadDocumentAsync(
                                knowledgeBaseId,
                                fileStream,
                                entry.Name);

                            results.Add(new ImportResult
                            {
                                FileName = entry.Name,
                                Success = true,
                                DocumentId = document.Id
                            });
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ImportResult
                        {
                            FileName = entry.Name,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                        failureCount++;
                    }
                }
            }
        }
        finally
        {
            // Clean up temporary directory
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        return new BatchImportResultDto
        {
            TotalFiles = successCount + failureCount,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results
        };
    }

    public async Task<BatchProcessResultDto> ReprocessKnowledgeBaseAsync(Guid knowledgeBaseId)
    {
        var results = new List<ProcessResult>();
        int processedCount = 0;
        int failedCount = 0;

        // Get all documents in the knowledge base
        var documents = await _knowledgeDocumentRepository.GetByKnowledgeBaseIdAsync(knowledgeBaseId);

        foreach (var document in documents)
        {
            try
            {
                // Reprocess document
                await _documentProcessingService.ProcessDocumentAsync(document.Id);

                // Get chunk count after processing
                var chunkCount = await _knowledgeDocumentRepository.GetChunkCountAsync(document.Id);

                results.Add(new ProcessResult
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    Success = true,
                    ChunksGenerated = chunkCount
                });
                processedCount++;
            }
            catch (Exception ex)
            {
                results.Add(new ProcessResult
                {
                    DocumentId = document.Id,
                    FileName = document.FileName,
                    Success = false,
                    ErrorMessage = ex.Message
                });
                failedCount++;
            }
        }

        return new BatchProcessResultDto
        {
            TotalDocuments = processedCount + failedCount,
            ProcessedCount = processedCount,
            FailedCount = failedCount,
            Results = results
        };
    }
}