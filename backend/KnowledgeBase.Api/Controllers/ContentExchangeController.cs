using System.Security.Claims;
using System.Text;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 文档内容交换接口，负责 Markdown 导入导出与版本差异比较。
/// </summary>
[ApiController]
[Authorize]
[Route("api/content-exchange")]
public sealed class ContentExchangeController(
    IContentExchangeService service,
    IKnowledgeAssetService knowledgeAssets,
    IAccessControlService access) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsSuperAdmin => User.IsInRole("SUPER_ADMIN");

    [HttpGet("documents/{documentId:guid}/markdown")]
    public async Task<IActionResult> ExportMarkdown(Guid documentId, CancellationToken ct)
    {
        if (!await access.CanViewDocumentAsync(documentId, UserId, IsSuperAdmin, ct)) return Forbid();
        var item = await service.ExportMarkdownAsync(documentId, ct);
        return item is null
            ? NotFound()
            : File(Encoding.UTF8.GetBytes(item.Markdown), "text/markdown; charset=utf-8", item.FileName);
    }

    [HttpPost("knowledge-bases/{knowledgeBaseId:guid}/markdown")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ImportMarkdownResultDto>> ImportMarkdown(
        Guid knowledgeBaseId,
        [FromQuery] Guid? parentId,
        IFormFile file,
        CancellationToken ct)
    {
        if (!await access.CanEditKnowledgeBaseAsync(knowledgeBaseId, UserId, IsSuperAdmin, ct)) return Forbid();
        if (parentId.HasValue && !await access.CanEditDocumentAsync(parentId.Value, UserId, IsSuperAdmin, ct)) return Forbid();
        if (file.Length == 0) return BadRequest(new { message = "文件不能为空" });

        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "仅支持 Markdown 文件" });

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true);
        var markdown = await reader.ReadToEndAsync(ct);
        return Ok(await service.ImportMarkdownAsync(knowledgeBaseId, parentId, file.FileName, markdown, ct));
    }

    [HttpGet("versions/{versionId:guid}/diff-current")]
    public async Task<ActionResult<DocumentDiffDto>> Diff(Guid versionId, CancellationToken ct)
    {
        var version = await knowledgeAssets.GetVersionAsync(versionId, ct);
        if (version is null) return NotFound();
        if (!await access.CanViewDocumentAsync(version.DocumentId, UserId, IsSuperAdmin, ct)) return Forbid();
        return await service.DiffVersionToCurrentAsync(versionId, ct) is { } diff ? Ok(diff) : NotFound();
    }
}
