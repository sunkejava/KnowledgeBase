using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 知识资产接口，负责标签、收藏、最近访问、版本和基础搜索。
/// </summary>
[ApiController]
[Authorize]
[Route("api/knowledge-assets")]
public sealed class KnowledgeAssetsController(
    IKnowledgeAssetService service,
    IAccessControlService accessControl) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("tags")]
    public Task<IReadOnlyList<TagDto>> Tags(CancellationToken ct) => service.GetTagsAsync(ct);

    [HttpPost("tags")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<TagDto> CreateTag(SaveTagRequest request, CancellationToken ct)
        => service.SaveTagAsync(null, request, ct);

    [HttpPut("tags/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<TagDto> UpdateTag(Guid id, SaveTagRequest request, CancellationToken ct)
        => service.SaveTagAsync(id, request, ct);

    [HttpDelete("tags/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken ct)
        => await service.DeleteTagAsync(id, ct) ? NoContent() : NotFound();

    [HttpGet("documents/{documentId:guid}/tags")]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> DocumentTags(Guid documentId, CancellationToken ct)
    {
        if (!await CanViewDocumentAsync(documentId, ct)) return Forbid();
        return Ok(await service.GetDocumentTagsAsync(documentId, ct));
    }

    [HttpPut("documents/{documentId:guid}/tags")]
    public async Task<IActionResult> SetTags(Guid documentId, [FromBody] Guid[] tagIds, CancellationToken ct)
    {
        if (!await CanEditDocumentAsync(documentId, ct)) return Forbid();
        await service.SetDocumentTagsAsync(documentId, tagIds, ct);
        return NoContent();
    }

    [HttpPost("documents/{documentId:guid}/favorite")]
    public async Task<ActionResult<object>> Favorite(Guid documentId, CancellationToken ct)
    {
        if (!await CanViewDocumentAsync(documentId, ct)) return Forbid();
        return Ok(new { favorite = await service.ToggleFavoriteAsync(UserId, documentId, ct) });
    }

    [HttpGet("favorites")]
    public Task<PageResult<FavoriteDocumentDto>> Favorites([FromQuery] PageQuery query, CancellationToken ct)
        => service.GetFavoritesPageAsync(UserId, query, ct);

    [HttpPost("documents/{documentId:guid}/recent")]
    public async Task<IActionResult> Track(Guid documentId, CancellationToken ct)
    {
        if (!await CanViewDocumentAsync(documentId, ct)) return Forbid();
        await service.TrackRecentAsync(UserId, documentId, ct);
        return NoContent();
    }

    [HttpGet("recent")]
    public Task<PageResult<RecentDocumentDto>> Recent([FromQuery] PageQuery query, CancellationToken ct)
        => service.GetRecentPageAsync(UserId, query, ct);

    [HttpPost("documents/{documentId:guid}/versions")]
    public async Task<ActionResult<VersionDto>> Version(Guid documentId, CreateVersionRequest request, CancellationToken ct)
    {
        if (!await CanEditDocumentAsync(documentId, ct)) return Forbid();
        return await service.CreateVersionAsync(documentId, UserId, request.ChangeNote, ct) is { } version
            ? Ok(version)
            : NotFound();
    }

    [HttpGet("documents/{documentId:guid}/versions")]
    public async Task<ActionResult<IReadOnlyList<VersionDto>>> Versions(Guid documentId, CancellationToken ct)
    {
        if (!await CanViewDocumentAsync(documentId, ct)) return Forbid();
        return Ok(await service.GetVersionsAsync(documentId, ct));
    }

    [HttpGet("versions/{versionId:guid}")]
    public async Task<ActionResult<VersionDetailDto>> VersionDetail(Guid versionId, CancellationToken ct)
    {
        var version = await service.GetVersionAsync(versionId, ct);
        if (version is null) return NotFound();
        if (!await CanViewDocumentAsync(version.DocumentId, ct)) return Forbid();
        return Ok(version);
    }

    [HttpPost("versions/{versionId:guid}/restore")]
    public async Task<IActionResult> Restore(Guid versionId, CancellationToken ct)
    {
        var version = await service.GetVersionAsync(versionId, ct);
        if (version is null) return NotFound();
        if (!await CanEditDocumentAsync(version.DocumentId, ct)) return Forbid();
        return await service.RestoreVersionAsync(versionId, UserId, ct) ? NoContent() : NotFound();
    }

    [HttpGet("search")]
    public Task<PageResult<SearchResultDto>> Search(
        [FromQuery] string keyword,
        [FromQuery] Guid? knowledgeBaseId,
        [FromQuery] PageQuery query,
        CancellationToken ct)
        => service.SearchPageAsync(keyword, UserId, User.IsInRole("SUPER_ADMIN"), knowledgeBaseId, query, ct);

    private Task<bool> CanViewDocumentAsync(Guid documentId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : accessControl.CanViewDocumentAsync(documentId, UserId, ct);

    private Task<bool> CanEditDocumentAsync(Guid documentId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : accessControl.CanEditDocumentAsync(documentId, UserId, ct);
}
