using Microsoft.AspNetCore.Mvc;
using Application.ApplicationServiceContract;
using Application.ApplicationServiceContract.DocumentationService;


namespace PgSqlVectorSearch.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentManagementAppService _documentManagementAppService;

    public DocumentController(IDocumentManagementAppService documentManagementAppService)
    {
        _documentManagementAppService = documentManagementAppService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDocuments(
        [FromQuery] Guid? knowledgeBaseId = null)
    {
        // If knowledgeBaseId is provided, get documents for that knowledge base
        // Otherwise, get all documents (may need a separate service method)
        if (knowledgeBaseId.HasValue)
        {
            var result = await _documentManagementAppService.GetDocumentsAsync(knowledgeBaseId.Value);
            return Ok(result);
        }
        else
        {
            // For now, return empty list - need to implement GetAllDocumentsAsync method
            return Ok(Array.Empty<DocumentDto>());
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentDto>> GetDocument(Guid id)
    {
        try
        {
            var result = await _documentManagementAppService.GetDocumentByIdAsync(id);
            return Ok(result);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Get document failed: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDocument(Guid id)
    {
        await _documentManagementAppService.DeleteDocumentAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/reprocess")]
    public async Task<ActionResult> ReprocessDocument(Guid id)
    {
        await _documentManagementAppService.ReprocessDocumentAsync(id);
        return Accepted();
    }

    [HttpGet("{id}/chunks")]
    public async Task<ActionResult<IEnumerable<KnowledgeChunkDto>>> GetDocumentChunks(Guid id)
    {
        try
        {
            var result = await _documentManagementAppService.GetDocumentChunksAsync(id);
            return Ok(result);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Get document chunks failed: {ex.Message}");
        }
    }
}