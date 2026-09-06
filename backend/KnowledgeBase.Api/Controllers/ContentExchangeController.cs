using System.Text;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/content-exchange")]
public sealed class ContentExchangeController(IContentExchangeService service) : ControllerBase
{
    [HttpGet("documents/{documentId:guid}/markdown")]
    public async Task<IActionResult> ExportMarkdown(Guid documentId,CancellationToken ct)
    {
        var item=await service.ExportMarkdownAsync(documentId,ct);
        return item is null?NotFound():File(Encoding.UTF8.GetBytes(item.Markdown),"text/markdown; charset=utf-8",item.FileName);
    }

    [HttpPost("knowledge-bases/{knowledgeBaseId:guid}/markdown")]
    [RequestSizeLimit(20*1024*1024)]
    public async Task<ActionResult<ImportMarkdownResultDto>> ImportMarkdown(Guid knowledgeBaseId,[FromQuery]Guid? parentId,IFormFile file,CancellationToken ct)
    {
        if(file.Length==0)return BadRequest(new{message="文件不能为空"});
        if(!Path.GetExtension(file.FileName).Equals(".md",StringComparison.OrdinalIgnoreCase)&&!Path.GetExtension(file.FileName).Equals(".markdown",StringComparison.OrdinalIgnoreCase))return BadRequest(new{message="仅支持 Markdown 文件"});
        using var reader=new StreamReader(file.OpenReadStream(),Encoding.UTF8,true);
        var markdown=await reader.ReadToEndAsync(ct);
        return Ok(await service.ImportMarkdownAsync(knowledgeBaseId,parentId,file.FileName,markdown,ct));
    }

    [HttpGet("versions/{versionId:guid}/diff-current")]
    public async Task<ActionResult<DocumentDiffDto>> Diff(Guid versionId,CancellationToken ct)
        => await service.DiffVersionToCurrentAsync(versionId,ct) is{} diff?Ok(diff):NotFound();
}
