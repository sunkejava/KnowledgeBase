using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace KnowledgeBase.Api.Controllers;
[ApiController][Authorize][Route("api/knowledge-assets")]
public sealed class KnowledgeAssetsController(IKnowledgeAssetService service):ControllerBase
{
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("tags")] public Task<IReadOnlyList<TagDto>> Tags(CancellationToken ct)=>service.GetTagsAsync(ct);
    [HttpPost("tags")] public Task<TagDto> CreateTag(SaveTagRequest r,CancellationToken ct)=>service.SaveTagAsync(null,r,ct);
    [HttpPut("tags/{id:guid}")] public Task<TagDto> UpdateTag(Guid id,SaveTagRequest r,CancellationToken ct)=>service.SaveTagAsync(id,r,ct);
    [HttpDelete("tags/{id:guid}")] public async Task<IActionResult> DeleteTag(Guid id,CancellationToken ct)=>await service.DeleteTagAsync(id,ct)?NoContent():NotFound();
    [HttpGet("documents/{documentId:guid}/tags")] public Task<IReadOnlyList<TagDto>> DocumentTags(Guid documentId,CancellationToken ct)=>service.GetDocumentTagsAsync(documentId,ct);
    [HttpPut("documents/{documentId:guid}/tags")] public async Task<IActionResult> SetTags(Guid documentId,[FromBody]Guid[] tagIds,CancellationToken ct){await service.SetDocumentTagsAsync(documentId,tagIds,ct);return NoContent();}
    [HttpPost("documents/{documentId:guid}/favorite")] public async Task<object> Favorite(Guid documentId,CancellationToken ct)=>new{favorite=await service.ToggleFavoriteAsync(UserId,documentId,ct)};
    [HttpGet("favorites")] public Task<IReadOnlyList<FavoriteDocumentDto>> Favorites(CancellationToken ct)=>service.GetFavoritesAsync(UserId,ct);
    [HttpPost("documents/{documentId:guid}/recent")] public async Task<IActionResult> Track(Guid documentId,CancellationToken ct){await service.TrackRecentAsync(UserId,documentId,ct);return NoContent();}
    [HttpGet("recent")] public Task<IReadOnlyList<RecentDocumentDto>> Recent([FromQuery]int take=30,CancellationToken ct=default)=>service.GetRecentAsync(UserId,take,ct);
    [HttpPost("documents/{documentId:guid}/versions")] public async Task<ActionResult<VersionDto>> Version(Guid documentId,CreateVersionRequest r,CancellationToken ct)=>await service.CreateVersionAsync(documentId,UserId,r.ChangeNote,ct) is{} v?Ok(v):NotFound();
    [HttpGet("documents/{documentId:guid}/versions")] public Task<IReadOnlyList<VersionDto>> Versions(Guid documentId,CancellationToken ct)=>service.GetVersionsAsync(documentId,ct);
    [HttpGet("versions/{versionId:guid}")] public async Task<ActionResult<VersionDetailDto>> VersionDetail(Guid versionId,CancellationToken ct)=>await service.GetVersionAsync(versionId,ct) is{} v?Ok(v):NotFound();
    [HttpPost("versions/{versionId:guid}/restore")] public async Task<IActionResult> Restore(Guid versionId,CancellationToken ct)=>await service.RestoreVersionAsync(versionId,UserId,ct)?NoContent():NotFound();
    [HttpGet("search")] public Task<IReadOnlyList<SearchResultDto>> Search([FromQuery]string keyword,[FromQuery]Guid? knowledgeBaseId,[FromQuery]int take=50,CancellationToken ct=default)=>service.SearchAsync(keyword,knowledgeBaseId,take,ct);
}
