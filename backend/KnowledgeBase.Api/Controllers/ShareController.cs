using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 文档分享接口。创建、查看和停用分享链接需要文档管理权限。
/// </summary>
[ApiController]
[Route("api/share")]
public sealed class ShareController(IShareService service, IAccessControlService access) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize]
    [HttpPost("documents/{documentId:guid}")]
    public async Task<ActionResult<ShareLinkDto>> Create(Guid documentId, CreateShareLinkRequest request, CancellationToken ct)
    {
        if (!await CanManageDocumentAsync(documentId, ct)) return Forbid();
        return await service.CreateAsync(documentId, CurrentUserId, request.ExpiresAt, ct) is { } item ? Ok(item) : NotFound();
    }

    [Authorize]
    [HttpGet("documents/{documentId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ShareLinkDto>>> List(Guid documentId, CancellationToken ct)
    {
        if (!await CanManageDocumentAsync(documentId, ct)) return Forbid();
        return Ok(await service.GetDocumentLinksAsync(documentId, ct));
    }

    [Authorize]
    [HttpDelete("links/{id:guid}")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct)
    {
        var documentId = await service.GetDocumentIdByLinkAsync(id, ct);
        if (!documentId.HasValue) return NotFound();
        if (!await CanManageDocumentAsync(documentId.Value, ct)) return Forbid();
        return await service.DisableAsync(id, ct) ? NoContent() : NotFound();
    }

    [AllowAnonymous]
    [HttpGet("public/{token}")]
    public async Task<ActionResult<SharedDocumentDto>> Public(string token, CancellationToken ct)
        => await service.GetSharedDocumentAsync(token, ct) is { } doc ? Ok(doc) : NotFound();

    private Task<bool> CanManageDocumentAsync(Guid documentId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : access.CanManageDocumentAsync(documentId, CurrentUserId, ct);
}
