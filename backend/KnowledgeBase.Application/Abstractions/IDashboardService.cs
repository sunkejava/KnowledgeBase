using KnowledgeBase.Contracts.Dashboard;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>工作台统计服务。</summary>
public interface IDashboardService
{
    /// <summary>获取当前用户可访问范围内的工作台统计数据。</summary>
    Task<DashboardOverviewDto> GetOverviewAsync(Guid userId, bool isSuperAdmin, CancellationToken ct);
}
