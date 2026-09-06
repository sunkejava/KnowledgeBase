using System.Security.Claims;
using System.Text;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>文档导入、导出和版本差异接口。</summary>
[ApiController]
[Authorize]
[Route("api/content-exchange")]
public sealed class ContentExchangeController(IContentExchangeService service, IAccessControlService accessControl) : ControllerBase
{
    private static readonly string[] SupportedDocumentExtensions =
        [".txt", ".md", ".markdown", ".html", ".htm", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv"];

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>返回当前支持的知识文档导入格式。</summary>
    [HttpGet("supported-formats")]
    public IActionResult SupportedFormats() => Ok(new
    {
        extensions = SupportedDocumentExtensions,
        descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".txt"] = "纯文本",
            [".md"] = "Markdown",
            [".markdown"] = "Markdown",
            [".html"] = "HTML",
            [".htm"] = "HTML",
            [".pdf"] = "文本型 PDF",
            [".doc"] = "Word 97-2003",
            [".docx"] = "Word Open XML",
            [".xls"] = "Excel 97-2003",
            [".xlsx"] = "Excel Open XML",
            [".csv"] = "CSV 表格"
        },
        note = "扫描件或纯图片 PDF 暂不自动 OCR，未提取到文本时会明确提示。"
    });

    /// <summary>导出单篇 Markdown 文档。</summary>
    [HttpGet("documents/{documentId:guid}/markdown")]
    public async Task<IActionResult> ExportMarkdown(Guid documentId, CancellationToken ct)
    {
        if (!await CanViewDocumentAsync(documentId, ct)) return Forbid();
        var item = await service.ExportMarkdownAsync(documentId, ct);
        return item is null ? NotFound() : File(Encoding.UTF8.GetBytes(item.Markdown), "text/markdown; charset=utf-8", item.FileName);
    }

    /// <summary>
    /// 统一导入常见文档格式。
    /// 支持 TXT、Markdown、HTML、PDF、DOC、DOCX、XLS、XLSX、CSV。
    /// </summary>
    [HttpPost("knowledge-bases/{knowledgeBaseId:guid}/document")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<ImportMarkdownResultDto>> ImportDocument(
        Guid knowledgeBaseId,
        [FromQuery] Guid? parentId,
        IFormFile file,
        CancellationToken ct)
    {
        if (!await CanEditKnowledgeBaseAsync(knowledgeBaseId, ct)) return Forbid();
        if (parentId.HasValue && !await CanEditDocumentAsync(parentId.Value, ct)) return Forbid();
        if (file.Length == 0) return BadRequest(new { message = "文件不能为空" });

        var extension = Path.GetExtension(file.FileName);
        if (!SupportedDocumentExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = $"不支持的文件格式：{extension}。支持 TXT、Markdown、HTML、PDF、DOC、DOCX、XLS、XLSX、CSV。" });

        await using var stream = file.OpenReadStream();
        try
        {
            return Ok(await service.ImportDocumentAsync(knowledgeBaseId, parentId, file.FileName, stream, ct));
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is IOException or FormatException)
        {
            return BadRequest(new { message = $"文档解析失败：{ex.Message}" });
        }
    }

    /// <summary>导入单篇 Markdown 文档。保留旧接口用于兼容已有前端和外部调用。</summary>
    [HttpPost("knowledge-bases/{knowledgeBaseId:guid}/markdown")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ImportMarkdownResultDto>> ImportMarkdown(Guid knowledgeBaseId, [FromQuery] Guid? parentId, IFormFile file, CancellationToken ct)
    {
        if (!await CanEditKnowledgeBaseAsync(knowledgeBaseId, ct)) return Forbid();
        if (parentId.HasValue && !await CanEditDocumentAsync(parentId.Value, ct)) return Forbid();
        if (file.Length == 0) return BadRequest(new { message = "文件不能为空" });
        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".md", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".markdown", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "仅支持 Markdown 文件" });

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true);
        return Ok(await service.ImportMarkdownAsync(knowledgeBaseId, parentId, file.FileName, await reader.ReadToEndAsync(ct), ct));
    }

    /// <summary>导入单篇 HTML 文档，并转换为 Markdown。保留兼容接口。</summary>
    [HttpPost("knowledge-bases/{knowledgeBaseId:guid}/html")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ImportMarkdownResultDto>> ImportHtml(Guid knowledgeBaseId, [FromQuery] Guid? parentId, IFormFile file, CancellationToken ct)
    {
        if (!await CanEditKnowledgeBaseAsync(knowledgeBaseId, ct)) return Forbid();
        if (parentId.HasValue && !await CanEditDocumentAsync(parentId.Value, ct)) return Forbid();
        var ext = Path.GetExtension(file.FileName);
        if (file.Length == 0 || (!ext.Equals(".html", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".htm", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { message = "仅支持 HTML 文件" });

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true);
        return Ok(await service.ImportHtmlAsync(knowledgeBaseId, parentId, file.FileName, await reader.ReadToEndAsync(ct), ct));
    }

    /// <summary>导入 DOCX 文档，提取文本和标题层级后转换为 Markdown。保留兼容接口。</summary>
    [HttpPost("knowledge-bases/{knowledgeBaseId:guid}/docx")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ImportMarkdownResultDto>> ImportDocx(Guid knowledgeBaseId, [FromQuery] Guid? parentId, IFormFile file, CancellationToken ct)
    {
        if (!await CanEditKnowledgeBaseAsync(knowledgeBaseId, ct)) return Forbid();
        if (parentId.HasValue && !await CanEditDocumentAsync(parentId.Value, ct)) return Forbid();
        if (file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "仅支持 DOCX 文件" });

        await using var stream = file.OpenReadStream();
        try
        {
            return Ok(await service.ImportDocxAsync(knowledgeBaseId, parentId, file.FileName, stream, ct));
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>同步导入 Markdown ZIP。较大 ZIP 建议使用 /api/import-tasks 异步任务接口。</summary>
    [HttpPost("knowledge-bases/{knowledgeBaseId:guid}/markdown-zip")]
    [RequestSizeLimit(200 * 1024 * 1024)]
    public async Task<ActionResult<ImportZipResultDto>> ImportZip(Guid knowledgeBaseId, IFormFile file, CancellationToken ct)
    {
        if (!await CanEditKnowledgeBaseAsync(knowledgeBaseId, ct)) return Forbid();
        if (file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "请选择 ZIP 文件" });

        await using var stream = file.OpenReadStream();
        return Ok(await service.ImportMarkdownZipAsync(knowledgeBaseId, stream, null, ct));
    }

    /// <summary>对比历史版本和当前正文。</summary>
    [HttpGet("versions/{versionId:guid}/diff-current")]
    public async Task<ActionResult<DocumentDiffDto>> Diff(Guid versionId, CancellationToken ct)
    {
        var diff = await service.DiffVersionToCurrentAsync(versionId, ct);
        if (diff is null) return NotFound();
        if (!await CanViewDocumentAsync(diff.DocumentId, ct)) return Forbid();
        return Ok(diff);
    }

    private Task<bool> CanViewDocumentAsync(Guid id, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN") ? Task.FromResult(true) : accessControl.CanViewDocumentAsync(id, UserId, ct);

    private Task<bool> CanEditDocumentAsync(Guid id, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN") ? Task.FromResult(true) : accessControl.CanEditDocumentAsync(id, UserId, ct);

    private Task<bool> CanEditKnowledgeBaseAsync(Guid id, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN") ? Task.FromResult(true) : accessControl.CanEditKnowledgeBaseAsync(id, UserId, ct);
}
