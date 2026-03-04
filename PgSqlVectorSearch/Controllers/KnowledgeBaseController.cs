using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.Dto;

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

    [HttpGet]
    public async Task<ActionResult<PagedResult<KnowledgeBaseDto>>> GetKnowledgeBases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _knowledgeBaseAppService.ListKnowledgeBasesAsync(page, pageSize);
        return Ok(result);
    }

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

    [HttpPost]
    public async Task<ActionResult<KnowledgeBaseDto>> CreateKnowledgeBase(
        [FromBody] CreateKnowledgeBaseRequest request)
    {
        var result = await _knowledgeBaseAppService.CreateKnowledgeBaseAsync(request);
        return CreatedAtAction(nameof(GetKnowledgeBase), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<KnowledgeBaseDto>> UpdateKnowledgeBase(
        Guid id,
        [FromBody] CreateKnowledgeBaseRequest request)
    {
        // Note: Update method not defined in interface, may need to add
        // For now, return Not Implemented
        return StatusCode(501, "Update not implemented");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteKnowledgeBase(Guid id)
    {
        // Note: Delete method not defined in interface, may need to add
        // For now, return Not Implemented
        return StatusCode(501, "Delete not implemented");
    }

    [HttpPost("{id}/documents")]
    public async Task<ActionResult> UploadDocument(
        Guid id,
        [FromForm] UploadDocumentRequest request)
    {
        await _knowledgeBaseAppService.UploadDocumentAsync(id, request);
        return Accepted();
    }

    [HttpPost("{id}/process")]
    public async Task<ActionResult> ProcessKnowledgeBase(Guid id)
    {
        await _knowledgeBaseAppService.ProcessKnowledgeBaseAsync(id);
        return Accepted();
    }
}