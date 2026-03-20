using System.IO.Compression;
using DomainService.ServiceContract.Repositories;
using Application.ServiceContract.CrossCutting;
using Application.ApplicationServiceContract;
using Application.ApplicationServiceContract.DocumentationService;
using Application.Extensions;
using Application.ApplicationServiceContract.CrossCutting;
using Application.Dto;
namespace Application.ApplicationService;

public class BatchOperationAppService : IBatchOperationAppService
{
    private readonly IDocumentManagementAppService _documentManageService;
    private readonly IKnowledgeDocumentRepository _knowledgeDocumentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public BatchOperationAppService(
        IDocumentManagementAppService documentManagementService,
        IKnowledgeDocumentRepository knowledgeDocumentRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork)
    {
        _documentManageService = documentManagementService;
        _knowledgeDocumentRepository = knowledgeDocumentRepository;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<BatchImportResultDto> ImportDocumentsAsync(Guid knowledgeBaseId, FileUpload file)
    {
        var results = new List<ImportResult>();
        int successCount = 0;
        int failureCount = 0;
        
        try
        {
            if (file.ContentType==FileType.Text||file.ContentType==FileType.Markdown||file.ContentType==FileType.Pdf) { }
            if (file.ContentType == FileType.Zip)
            {
                // Create a temporary directory to extract zip files
                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                // Extract zip file
                using (var archive = new ZipArchive(file.Content, ZipArchiveMode.Read))
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
                                var document = await _documentManageService.UploadDocumentAsync(
                                    knowledgeBaseId,
                                    new FileUpload
                                    {
                                        Content = fileStream,
                                        FileName = entry.Name,
                                        ContentType = MimeTypeMapper.GetDocumentType(entry.FullName)
                                    });

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
                if (Directory.Exists(tempDir))
                    // Clean up temporary directory
                    Directory.Delete(tempDir, recursive: true);
                
            }
            if (file.ContentType == FileType.Unknown)
            {
                results.Add(new ImportResult
                {
                    FileName = file.FileName,
                    Success = false,
                    ErrorMessage = $"Unsupported file type: {file.ContentType}"
                });
                failureCount++;
            }
        }
        catch (Exception ex)
        {
            // Handle zip extraction errors
            results.Add(new ImportResult
            {
                FileName = file.FileName,
                Success = false,
                ErrorMessage = $"Failed to process zip file: {ex.Message}"
            });
            failureCount++;
        }
        await _unitOfWork.SaveChangesAsync();
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
                await _documentManageService.ProcessDocumentAsync(document.Id);

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
        await _unitOfWork.SaveChangesAsync();
        return new BatchProcessResultDto
        {
            TotalDocuments = processedCount + failedCount,
            ProcessedCount = processedCount,
            FailedCount = failedCount,
            Results = results
        };
    }
}