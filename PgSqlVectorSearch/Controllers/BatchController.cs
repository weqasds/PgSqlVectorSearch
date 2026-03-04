using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;

namespace PgSqlVectorSearch.Controllers;

[ApiController]
[Route("api/batch")]
public class BatchController : ControllerBase
{
    private readonly IBatchOperationAppService _batchOperationAppService;

    public BatchController(IBatchOperationAppService batchOperationAppService)
    {
        _batchOperationAppService = batchOperationAppService;
    }

    [HttpPost("import")]
    public async Task<ActionResult<BatchImportResultDto>> ImportDocuments(
        [FromQuery] Guid knowledgeBaseId,
        [FromForm] FileUpload zipFile)
    {
        var result = await _batchOperationAppService.ImportDocumentsAsync(knowledgeBaseId, zipFile);
        return Accepted(result);
    }

    [HttpPost("reprocess")]
    public async Task<ActionResult<BatchProcessResultDto>> ReprocessKnowledgeBase(
        [FromQuery] Guid knowledgeBaseId)
    {
        var result = await _batchOperationAppService.ReprocessKnowledgeBaseAsync(knowledgeBaseId);
        return Accepted(result);
    }
}