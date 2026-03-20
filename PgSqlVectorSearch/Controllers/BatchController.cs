using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using PgSqlVectorSearch.Model;
using Application.Extensions;
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
    /// <summary>
    /// 批量导入文档
    /// </summary>
    /// <param name="knowledgeBaseId"></param>
    /// <param name="zipFile"></param>
    /// <returns></returns>
    [HttpPost("import")]
    public async Task<ActionResult<BatchImportResultDto>> ImportDocuments(
        [FromQuery] Guid knowledgeBaseId,
        [FromForm] FileUploadDto uploadFile)
    {
        var file = uploadFile.File;
        var fileUpload = new FileUpload
        {
            Content = file.OpenReadStream(),
            FileName = file.FileName,
            ContentType = MimeTypeMapper.GetDocumentType(file.ContentType),
            FileSize = file.Length
        };
        var result = await _batchOperationAppService.ImportDocumentsAsync(knowledgeBaseId, fileUpload);
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