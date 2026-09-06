using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 搜索引擎管理接口。仅超级管理员可以重建索引、重试任务和清理索引任务历史。
/// </summary>
[ApiController]
[Authorize(Roles = "SUPER_ADMIN")]
[Route("api/search-management")]
public sealed class SearchManagementController(ISearchIndexTaskService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>获取当前搜索引擎配置状态。</summary>
    [HttpGet("status")]
    public ActionResult<SearchProviderStatusDto> Status() => Ok(service.GetProviderStatus());

    /// <summary>创建全量索引重建任务。</summary>
    [HttpPost("rebuild")]
    public async Task<ActionResult<SearchIndexTaskDto>> Rebuild(CancellationToken ct)
        => Accepted(await service.CreateAsync(UserId, ct));

    /// <summary>分页获取索引任务历史。</summary>
    [HttpGet("tasks")]
    public Task<PageResult<SearchIndexTaskDto>> Tasks([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => service.GetPageAsync(page, pageSize, ct);

    /// <summary>重试失败的索引任务。</summary>
    [HttpPost("tasks/{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
        => await service.RetryAsync(id, ct) ? NoContent() : BadRequest(new { message = "仅失败任务可以重试" });

    /// <summary>清理历史索引任务。</summary>
    [HttpDelete("tasks")]
    public async Task<ActionResult<object>> Cleanup([FromQuery] int retentionDays = 30, CancellationToken ct = default)
        => Ok(new { deleted = await service.CleanupAsync(retentionDays, ct) });
}
