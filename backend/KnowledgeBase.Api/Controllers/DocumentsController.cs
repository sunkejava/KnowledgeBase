using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(IDocumentService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<DocumentListItemDto>> GetList([FromQuery] Guid knowledgeBaseId, CancellationToken cancellationToken)
        => service.GetListAsync(knowledgeBaseId, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDetailDto>> Get(Guid id, CancellationToken cancellationToken)
        => await service.GetAsync(id, cancellationToken) is { } item ? Ok(item) : NotFound();

    [HttpPost]
    public async Task<ActionResult<DocumentDetailDto>> Create(CreateDocumentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "文档标题不能为空" });
        var item = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentDetailDto>> Update(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken)
        => await service.UpdateAsync(id, request, cancellationToken) is { } item ? Ok(item) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
