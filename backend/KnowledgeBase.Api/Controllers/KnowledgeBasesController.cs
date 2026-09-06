using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.KnowledgeBases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/knowledge-bases")]
public sealed class KnowledgeBasesController(IKnowledgeBaseService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<KnowledgeBaseDto>> GetList(CancellationToken cancellationToken)
        => service.GetListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<KnowledgeBaseDto>> Get(Guid id, CancellationToken cancellationToken)
        => await service.GetAsync(id, cancellationToken) is { } item ? Ok(item) : NotFound();

    [HttpPost]
    public async Task<ActionResult<KnowledgeBaseDto>> Create(SaveKnowledgeBaseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "知识库名称不能为空" });
        var item = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<KnowledgeBaseDto>> Update(Guid id, SaveKnowledgeBaseRequest request, CancellationToken cancellationToken)
        => await service.UpdateAsync(id, request, cancellationToken) is { } item ? Ok(item) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
