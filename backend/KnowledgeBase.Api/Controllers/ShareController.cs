using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>文档分享、密码访问与访问日志接口。</summary>
[ApiController]
[Route("api/share")]
public sealed class ShareController(IShareService service,IAccessControlService accessControl):ControllerBase
{
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize][HttpPost("documents/{documentId:guid}")]
    public async Task<ActionResult<ShareLinkDto>> Create(Guid documentId,CreateShareLinkRequest request,CancellationToken ct)
    {
        if(!await CanManageDocumentAsync(documentId,ct))return Forbid();
        return await service.CreateAsync(documentId,UserId,request.ExpiresAt,request.Password,ct) is{} item?Ok(item):NotFound();
    }

    [Authorize][HttpGet("documents/{documentId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ShareLinkDto>>> List(Guid documentId,CancellationToken ct)
    {
        if(!await CanManageDocumentAsync(documentId,ct))return Forbid();
        return Ok(await service.GetDocumentLinksAsync(documentId,ct));
    }

    [Authorize][HttpGet("documents/{documentId:guid}/access-logs")]
    public async Task<ActionResult<IReadOnlyList<ShareAccessLogDto>>> Logs(Guid documentId,[FromQuery]int take=200,CancellationToken ct=default)
    {
        if(!await CanManageDocumentAsync(documentId,ct))return Forbid();
        return Ok(await service.GetAccessLogsAsync(documentId,take,ct));
    }

    [Authorize][HttpDelete("links/{id:guid}")]
    public async Task<IActionResult> Disable(Guid id,CancellationToken ct)
    {
        var documentId=await service.GetDocumentIdByLinkAsync(id,ct);
        if(!documentId.HasValue)return NotFound();
        if(!await CanManageDocumentAsync(documentId.Value,ct))return Forbid();
        return await service.DisableAsync(id,ct)?NoContent():NotFound();
    }

    [AllowAnonymous][HttpPost("public/{token}/access")]
    public async Task<ActionResult<SharedDocumentDto>> Public(string token,PublicShareAccessRequest request,CancellationToken ct)
    {
        var ip=HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent=Request.Headers.UserAgent.ToString();
        return await service.GetSharedDocumentAsync(token,request.Password,ip,userAgent,ct) is{} doc?Ok(doc):Unauthorized(new{message="分享链接不可用、已过期或访问密码错误"});
    }

    private Task<bool> CanManageDocumentAsync(Guid documentId,CancellationToken ct)
        =>User.IsInRole("SUPER_ADMIN")?Task.FromResult(true):accessControl.CanManageDocumentAsync(documentId,UserId,ct);
}
