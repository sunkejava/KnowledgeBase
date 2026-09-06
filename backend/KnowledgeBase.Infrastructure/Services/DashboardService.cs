using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Dashboard;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 工作台统计服务。
/// 所有统计均来自真实业务表，并在后端按当前用户知识库权限进行隔离。
/// </summary>
public sealed class DashboardService(KnowledgeDbContext db) : IDashboardService
{
    /// <summary>获取当前用户可访问范围内的工作台汇总。</summary>
    public async Task<DashboardOverviewDto> GetOverviewAsync(Guid userId, bool isSuperAdmin, CancellationToken ct)
    {
        var knowledgeBases = db.KnowledgeBases.AsNoTracking().AsQueryable();
        if (!isSuperAdmin)
        {
            knowledgeBases = knowledgeBases.Where(k => db.KnowledgeBaseMembers.Any(m =>
                m.KnowledgeBaseId == k.Id && m.UserId == userId));
        }

        var accessibleKnowledgeBaseIds = knowledgeBases.Select(x => x.Id);
        var knowledgeBaseCount = await knowledgeBases.LongCountAsync(ct);
        var documentCount = await db.Documents.AsNoTracking()
            .LongCountAsync(x => accessibleKnowledgeBaseIds.Contains(x.KnowledgeBaseId), ct);
        var favoriteCount = await db.DocumentFavorites.AsNoTracking()
            .LongCountAsync(x => x.UserId == userId && db.Documents.Any(d =>
                d.Id == x.DocumentId && accessibleKnowledgeBaseIds.Contains(d.KnowledgeBaseId)), ct);
        var unreadNotificationCount = await db.UserNotifications.AsNoTracking()
            .LongCountAsync(x => x.UserId == userId && !x.IsRead, ct);
        var memberCount = await db.KnowledgeBaseMembers.AsNoTracking()
            .Where(x => accessibleKnowledgeBaseIds.Contains(x.KnowledgeBaseId))
            .Select(x => x.UserId)
            .Distinct()
            .LongCountAsync(ct);

        // SQLite 不支持 DateTimeOffset ORDER BY，因此先完成权限过滤和投影，再在内存中按时间排序。
        var recentRows = await (
            from recent in db.DocumentRecentViews.AsNoTracking()
            join document in db.Documents.AsNoTracking() on recent.DocumentId equals document.Id
            join kb in db.KnowledgeBases.AsNoTracking() on document.KnowledgeBaseId equals kb.Id
            where recent.UserId == userId && accessibleKnowledgeBaseIds.Contains(document.KnowledgeBaseId)
            select new DashboardRecentDocumentDto(
                document.Id,
                document.KnowledgeBaseId,
                document.Title,
                kb.Name,
                recent.LastViewedAt,
                recent.ViewCount))
            .ToListAsync(ct);
        var recentDocuments = recentRows.OrderByDescending(x => x.LastViewedAt).Take(8).ToList();

        // 热门文档按累计访问次数排序，排序字段为 INTEGER，可安全交给 SQLite 执行。
        var popularDocuments = await (
            from recent in db.DocumentRecentViews.AsNoTracking()
            join document in db.Documents.AsNoTracking() on recent.DocumentId equals document.Id
            where accessibleKnowledgeBaseIds.Contains(document.KnowledgeBaseId)
            group recent by new { document.Id, document.KnowledgeBaseId, document.Title } into g
            orderby g.Sum(x => x.ViewCount) descending
            select new DashboardPopularDocumentDto(
                g.Key.Id,
                g.Key.KnowledgeBaseId,
                g.Key.Title,
                g.Sum(x => (long)x.ViewCount)))
            .Take(8)
            .ToListAsync(ct);

        var exportSummary = await BuildTaskSummaryAsync("导出任务", db.ExportTasks.AsNoTracking().Where(x => isSuperAdmin || x.UserId == userId), ct);
        var importSummary = await BuildTaskSummaryAsync("导入任务", db.ImportTasks.AsNoTracking().Where(x => isSuperAdmin || x.UserId == userId), ct);
        var searchSummary = isSuperAdmin
            ? await BuildSearchTaskSummaryAsync(ct)
            : new DashboardTaskSummaryDto("索引任务", 0, 0, 0);
        var taskSummaries = new[] { exportSummary, importSummary, searchSummary };
        var failedTaskCount = taskSummaries.Sum(x => x.Failed);

        return new DashboardOverviewDto(
            knowledgeBaseCount,
            documentCount,
            favoriteCount,
            unreadNotificationCount,
            memberCount,
            failedTaskCount,
            recentDocuments,
            popularDocuments,
            taskSummaries);
    }

    private static async Task<DashboardTaskSummaryDto> BuildTaskSummaryAsync<T>(
        string type,
        IQueryable<T> source,
        CancellationToken ct) where T : class
    {
        // 任务实体的 Status 属性类型一致，但泛型无法直接访问属性，因此通过 EF.Property 保持查询在数据库执行。
        var pending = await source.LongCountAsync(x => EF.Property<string>(x, "Status") == "Pending", ct);
        var running = await source.LongCountAsync(x => EF.Property<string>(x, "Status") == "Running", ct);
        var failed = await source.LongCountAsync(x => EF.Property<string>(x, "Status") == "Failed", ct);
        return new DashboardTaskSummaryDto(type, pending, running, failed);
    }

    private async Task<DashboardTaskSummaryDto> BuildSearchTaskSummaryAsync(CancellationToken ct)
    {
        var source = db.SearchIndexTasks.AsNoTracking();
        return new DashboardTaskSummaryDto(
            "索引任务",
            await source.LongCountAsync(x => x.Status == "Pending", ct),
            await source.LongCountAsync(x => x.Status == "Running", ct),
            await source.LongCountAsync(x => x.Status == "Failed", ct));
    }
}
