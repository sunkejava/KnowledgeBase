namespace KnowledgeBase.Contracts.Dashboard;

/// <summary>工作台汇总数据。</summary>
public sealed record DashboardOverviewDto(
    long KnowledgeBaseCount,
    long DocumentCount,
    long FavoriteCount,
    long UnreadNotificationCount,
    long MemberCount,
    long FailedTaskCount,
    IReadOnlyList<DashboardRecentDocumentDto> RecentDocuments,
    IReadOnlyList<DashboardPopularDocumentDto> PopularDocuments,
    IReadOnlyList<DashboardTaskSummaryDto> TaskSummaries);

/// <summary>工作台最近访问文档。</summary>
public sealed record DashboardRecentDocumentDto(
    Guid DocumentId,
    Guid KnowledgeBaseId,
    string Title,
    string KnowledgeBaseName,
    DateTimeOffset LastViewedAt,
    int ViewCount);

/// <summary>工作台热门文档。</summary>
public sealed record DashboardPopularDocumentDto(
    Guid DocumentId,
    Guid KnowledgeBaseId,
    string Title,
    long ViewCount);

/// <summary>工作台后台任务状态汇总。</summary>
public sealed record DashboardTaskSummaryDto(string Type, long Pending, long Running, long Failed);
