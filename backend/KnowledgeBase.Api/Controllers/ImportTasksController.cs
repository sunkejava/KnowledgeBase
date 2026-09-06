using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>服务端导入任务中心接口。</summary>
[ApiController]
[Authorize]
[Route("api/import-tasks")]
public sealed class ImportTasksController(IImportTaskService service, IAccessControlService accessControl) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsSuperAdmin => User.IsInRole("SUPER_ADMIN");

    /// <summary>上传 Markdown ZIP 并创建异步导入任务。</summary>
    [HttpPost("knowledge-bases/{knowledgeBaseId:guid}/zip")]
    [RequestSizeLimit(200 * 1024 * 1024)]
    public async Task<ActionResult<ImportTaskDto>> CreateZip(Guid knowledgeBaseId, IFormFile file, CancellationToken ct)
    {
        if (!IsSuperAdmin && !await accessControl.CanEditKnowledgeBaseAsync(knowledgeBaseId, UserId, ct)) return Forbid();
        if (file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "请选择 ZIP 文件" });

        await using var stream = file.OpenReadStream();
        return Ok(await service.CreateZipAsync(UserId, knowledgeBaseId, file.FileName, stream, ct));
    }

    /// <summary>分页获取当前用户的导入任务。</summary>
    [HttpGet]
    public Task<PageResult<ImportTaskDto>> Page([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => service.GetPageAsync(UserId, page, pageSize, ct);

    /// <summary>取消尚未开始的导入任务。</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => await service.CancelAsync(id, UserId, IsSuperAdmin, ct) ? NoContent() : Conflict(new { message = "仅排队中的任务可以取消" });

    /// <summary>重试失败或已取消的导入任务。</summary>
    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
        => await service.RetryAsync(id, UserId, IsSuperAdmin, ct) ? NoContent() : Conflict(new { message = "仅失败或已取消任务可以重试" });

    /// <summary>清理指定保留天数之前的已结束任务及导入源文件。</summary>
    [HttpDelete("cleanup")]
    public async Task<IActionResult> Cleanup([FromQuery] int olderThanDays = 7, CancellationToken ct = default)
        => Ok(new { deletedCount = await service.CleanupAsync(UserId, IsSuperAdmin, olderThanDays, ct) });
}
