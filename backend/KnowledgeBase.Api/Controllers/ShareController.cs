using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Route("api/share")]
public sealed class ShareController(IShareService service):ControllerBase
{
    [Authorize]
    [HttpPost("documents/{documentId:guid}")]
    public async Task<ActionResult<ShareLinkDto>> Create(Guid documentId,CreateShareLinkRequest request,CancellationToken ct)
    {
        var userId=Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:(Guid?)null;
        return await service.CreateAsync(documentId,userId,request.ExpiresAt,ct) is{} item?Ok(item):NotFound();
    }

    [Authorize]
    [HttpGet("documents/{documentId:guid}")]
    public Task<IReadOnlyList<ShareLinkDto>> List(Guid documentId,CancellationToken ct)=>service.GetDocumentLinksAsync(documentId,ct);

    [Authorize]
    [HttpDelete("links/{id:guid}")]
    public async Task<IActionResult> Disable(Guid id,CancellationToken ct)=>await service.DisableAsync(id,ct)?NoContent():NotFound();

    [AllowAnonymous]
    [HttpGet("public/{token}")]
    public async Task<ActionResult<SharedDocumentDto>> Public(string token,CancellationToken ct)
        => await service.GetSharedDocumentAsync(token,ct) is{} doc?Ok(doc):NotFound();
}
