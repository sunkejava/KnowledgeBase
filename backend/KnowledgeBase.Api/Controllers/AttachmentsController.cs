using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace KnowledgeBase.Api.Controllers;
[ApiController][Authorize][Route("api/attachments")]
public sealed class AttachmentsController(IAttachmentService service):ControllerBase
{
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("document/{documentId:guid}")] public Task<IReadOnlyList<AttachmentDto>> List(Guid documentId,CancellationToken ct)=>service.GetListAsync(documentId,ct);
    [HttpPost("document/{documentId:guid}")][RequestSizeLimit(52_428_800)] public async Task<ActionResult<AttachmentDto>> Upload(Guid documentId,IFormFile file,CancellationToken ct){if(file.Length<=0)return BadRequest(new{message="文件为空"});await using var stream=file.OpenReadStream();return Ok(await service.SaveAsync(documentId,file.FileName,file.ContentType,file.Length,stream,UserId,ct));}
    [HttpGet("{id:guid}/download")] public async Task<IActionResult> Download(Guid id,CancellationToken ct){var file=await service.GetAsync(id,ct);if(file is null||!System.IO.File.Exists(file.Value.Path))return NotFound();return PhysicalFile(file.Value.Path,file.Value.ContentType,string.IsNullOrWhiteSpace(file.Value.FileName)?"attachment":file.Value.FileName);}
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id,CancellationToken ct)=>await service.DeleteAsync(id,ct)?NoContent():NotFound();
}
