using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 知识文档管理接口。
/// </summary>
[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(
    IDocumentService service,
    IAccessControlService accessControl) : ControllerBase
{
    /// <summary>
    /// 获取指定知识库的文档目录。Viewer 及以上角色可读取。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentListItemDto>>> GetList(
        [FromQuery] Guid knowledgeBaseId,
        CancellationToken cancellationToken)
    {
        if (!await CanViewKnowledgeBaseAsync(knowledgeBaseId, cancellationToken)) return Forbid();
        return Ok(await service.GetListAsync(knowledgeBaseId, cancellationToken));
    }

    /// <summary>
    /// 获取单篇文档正文。文档显式权限优先于知识库成员权限。
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanViewDocumentAsync(id, cancellationToken)) return Forbid();
        return await service.GetAsync(id, cancellationToken) is { } item ? Ok(item) : NotFound();
    }

    /// <summary>
    /// 创建文档。Editor、Manager 或超级管理员可操作。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DocumentDetailDto>> Create(CreateDocumentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "文档标题不能为空" });
        if (!await CanEditKnowledgeBaseAsync(request.KnowledgeBaseId, cancellationToken)) return Forbid();

        var item = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    /// <summary>
    /// 更新文档。文档显式编辑权限或知识库 Editor/Manager 可操作。
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentDetailDto>> Update(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken)
    {
        if (!await CanEditDocumentAsync(id, cancellationToken)) return Forbid();
        return await service.UpdateAsync(id, request, cancellationToken) is { } item ? Ok(item) : NotFound();
    }

    /// <summary>
    /// 删除文档，仅拥有文档管理权限、知识库 Manager 或超级管理员可操作。
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanManageDocumentAsync(id, cancellationToken)) return Forbid();
        return await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<bool> CanViewKnowledgeBaseAsync(Guid knowledgeBaseId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : accessControl.CanViewKnowledgeBaseAsync(knowledgeBaseId, CurrentUserId, ct);

    private Task<bool> CanEditKnowledgeBaseAsync(Guid knowledgeBaseId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : accessControl.CanEditKnowledgeBaseAsync(knowledgeBaseId, CurrentUserId, ct);

    private Task<bool> CanViewDocumentAsync(Guid documentId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : accessControl.CanViewDocumentAsync(documentId, CurrentUserId, ct);

    private Task<bool> CanEditDocumentAsync(Guid documentId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : accessControl.CanEditDocumentAsync(documentId, CurrentUserId, ct);

    private Task<bool> CanManageDocumentAsync(Guid documentId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : accessControl.CanManageDocumentAsync(documentId, CurrentUserId, ct);
}
