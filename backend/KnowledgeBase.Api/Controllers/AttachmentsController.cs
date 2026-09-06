using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 文档附件接口。附件读取、上传和删除均执行文档级资源权限校验。
/// </summary>
[ApiController]
[Authorize]
[Route("api/attachments")]
public sealed class AttachmentsController(IAttachmentService service, IAccessControlService access) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsSuperAdmin => User.IsInRole("SUPER_ADMIN");

    [HttpGet("document/{documentId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> List(Guid documentId, CancellationToken ct)
    {
        if (!await access.CanViewDocumentAsync(documentId, UserId, IsSuperAdmin, ct)) return Forbid();
        return Ok(await service.GetListAsync(documentId, ct));
    }

    [HttpPost("document/{documentId:guid}")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<AttachmentDto>> Upload(Guid documentId, IFormFile file, CancellationToken ct)
    {
        if (!await access.CanEditDocumentAsync(documentId, UserId, IsSuperAdmin, ct)) return Forbid();
        if (file.Length <= 0) return BadRequest(new { message = "文件为空" });
        await using var stream = file.OpenReadStream();
        return Ok(await service.SaveAsync(documentId, file.FileName, file.ContentType, file.Length, stream, UserId, ct));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var file = await service.GetAsync(id, ct);
        if (file is null) return NotFound();
        if (!await access.CanViewDocumentAsync(file.Value.DocumentId, UserId, IsSuperAdmin, ct)) return Forbid();
        if (!System.IO.File.Exists(file.Value.Path)) return NotFound();
        return PhysicalFile(file.Value.Path, file.Value.ContentType, string.IsNullOrWhiteSpace(file.Value.FileName) ? "attachment" : file.Value.FileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var file = await service.GetAsync(id, ct);
        if (file is null) return NotFound();
        if (!await access.CanEditDocumentAsync(file.Value.DocumentId, UserId, IsSuperAdmin, ct)) return Forbid();
        return await service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
