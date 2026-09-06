using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>工作台统计接口。</summary>
[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    /// <summary>获取当前用户工作台真实统计数据。</summary>
    [HttpGet("overview")]
    public Task<DashboardOverviewDto> Overview(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isSuperAdmin = User.IsInRole("SUPER_ADMIN");
        return service.GetOverviewAsync(userId, isSuperAdmin, ct);
    }
}
