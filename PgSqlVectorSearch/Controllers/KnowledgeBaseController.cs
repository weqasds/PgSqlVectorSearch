using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;
using PgSqlVectorSearch.Model;
using Application.Extensions;
using Application.ApplicationServiceContract.DocumentationService;

namespace PgSqlVectorSearch.Controllers;

[ApiController]
[Route("api/knowledge-bases")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseAppService _knowledgeBaseAppService;
    public KnowledgeBaseController(IKnowledgeBaseAppService knowledgeBaseAppService)
    {
        _knowledgeBaseAppService = knowledgeBaseAppService;
    }
    /// <summary>
    /// 获取所有知识库
    /// </summary>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<KnowledgeBaseDto>>> GetKnowledgeBases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _knowledgeBaseAppService.ListKnowledgeBasesAsync(page, pageSize);
        return Ok(result);
    }
    /// <summary>
    /// 获取知识库
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<KnowledgeBaseDto>> GetKnowledgeBase(Guid id)
    {
        var result = await _knowledgeBaseAppService.GetKnowledgeBaseAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }
    /// <summary>
    /// 创建知识库
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<KnowledgeBaseDto>> CreateKnowledgeBase(
        [FromForm] CreateKnowledgeBaseRequest request,
        [FromForm] FileUploadDto? uploadFile = null
        )
    {
        var result = await _knowledgeBaseAppService.CreateKnowledgeBaseAsync(request);
        if (uploadFile != null)
        {
            var file = uploadFile.File;
            var fileUpload = new FileUpload
            {
                Content = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = MimeTypeMapper.GetDocumentType(file.ContentType),
                FileSize = file.Length
            };
            await _knowledgeBaseAppService.UploadDocumentAsync(result.Id, fileUpload);
        }
            ;
        return CreatedAtAction(nameof(GetKnowledgeBase), new { id = result.Id }, result);
    }
    /// <summary>
    /// 更新知识库
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<KnowledgeBaseDto>> UpdateKnowledgeBase(
        Guid id,
        [FromBody] CreateKnowledgeBaseRequest request)
    {
        try
        {
            var result = await _knowledgeBaseAppService.UpdateKnowledgeBaseAsync(id, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // 根据异常类型返回适当的HTTP状态码
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }
            return StatusCode(500, $"Update failed: {ex.Message}");
        }
    }
    /// <summary>
    /// 删除知识库
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteKnowledgeBase(Guid id)
    {
        try
        {
            await _knowledgeBaseAppService.DeleteKnowledgeBaseAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            // 根据异常类型返回适当的HTTP状态码
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }
            return StatusCode(500, $"Delete failed: {ex.Message}");
        }
    }
    /// <summary>
    /// 上传文档
    /// </summary>
    /// <param name="id"></param>
    /// <param name="uploadFile"></param>
    /// <returns></returns>
    [HttpPost("{id}/documents")]
    public async Task<ActionResult> UploadDocument(
        [FromQuery] Guid id,
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
        await _knowledgeBaseAppService.UploadDocumentAsync(id, fileUpload);
        return Accepted();
    }
    /// <summary>
    /// 处理知识库
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost("{id}/process")]
    public async Task<ActionResult> ProcessKnowledgeBase(Guid id)
    {
        await _knowledgeBaseAppService.ProcessKnowledgeBaseAsync(id);
        return Accepted();
    }
}