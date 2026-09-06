using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Common;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 搜索索引任务实现。任务持久化到数据库，由后台 Worker 顺序执行，避免多个全量重建同时占用资源。
/// </summary>
public sealed class SearchIndexTaskService(
    KnowledgeDbContext db,
    IKnowledgeSearchService searchService,
    IConfiguration configuration) : ISearchIndexTaskService
{
    /// <summary>创建索引重建任务。</summary>
    public async Task<SearchIndexTaskDto> CreateAsync(Guid userId, CancellationToken ct)
    {
        var running = await db.SearchIndexTasks.AnyAsync(x => x.Status == "Pending" || x.Status == "Running", ct);
        if (running)
            throw new InvalidOperationException("已有索引重建任务正在排队或执行，请勿重复创建。");

        var entity = new SearchIndexTask(userId, searchService.ProviderName);
        db.SearchIndexTasks.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    /// <summary>分页获取索引任务历史。SQLite 下 CreatedAt 使用统一安全排序分页。</summary>
    public Task<PageResult<SearchIndexTaskDto>> GetPageAsync(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return db.SearchIndexTasks.AsNoTracking()
            .Select(x => new SearchIndexTaskDto(x.Id, x.Provider, x.Status, x.ErrorMessage, x.CreatedAt, x.StartedAt, x.CompletedAt))
            .ToSqliteSafeDateTimeOffsetPageAsync(x => x.CreatedAt, descending: true, page, pageSize, ct);
    }

    /// <summary>重试失败的索引任务。</summary>
    public async Task<bool> RetryAsync(Guid taskId, CancellationToken ct)
    {
        var entity = await db.SearchIndexTasks.FirstOrDefaultAsync(x => x.Id == taskId, ct);
        if (entity is null || !entity.Retry()) return false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// 清理指定天数之前已完成或失败的任务历史。
    /// SQLite 不直接对 DateTimeOffset 执行范围比较，因此先按状态过滤，再在内存判断时间阈值。
    /// </summary>
    public async Task<int> CleanupAsync(int retentionDays, CancellationToken ct)
    {
        retentionDays = Math.Clamp(retentionDays, 1, 3650);
        var threshold = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var candidates = await db.SearchIndexTasks
            .Where(x => x.Status == "Completed" || x.Status == "Failed")
            .ToListAsync(ct);
        var rows = candidates.Where(x => x.CreatedAt < threshold).ToList();
        if (rows.Count == 0) return 0;
        db.SearchIndexTasks.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    /// <summary>领取并执行下一个待处理索引任务。SQLite 下先过滤 Pending，再在内存按 CreatedAt 排序。</summary>
    public async Task ProcessNextPendingAsync(CancellationToken ct)
    {
        var entity = await db.SearchIndexTasks
            .Where(x => x.Status == "Pending")
            .FirstOrDefaultSqliteSafeDateTimeOffsetAsync(x => x.CreatedAt, descending: false, ct);
        if (entity is null) return;

        entity.Start();
        await db.SaveChangesAsync(ct);
        try
        {
            await searchService.RebuildIndexAsync(ct);
            entity.Complete();
        }
        catch (Exception ex)
        {
            entity.Fail(ex.Message);
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>返回当前配置的搜索引擎状态。</summary>
    public SearchProviderStatusDto GetProviderStatus()
    {
        var provider = searchService.ProviderName;
        var external = provider.Equals("meilisearch", StringComparison.OrdinalIgnoreCase);
        var endpoint = external ? configuration["Search:Meilisearch:Endpoint"] : null;
        return new SearchProviderStatusDto(provider, external, endpoint);
    }

    private static SearchIndexTaskDto Map(SearchIndexTask x)
        => new(x.Id, x.Provider, x.Status, x.ErrorMessage, x.CreatedAt, x.StartedAt, x.CompletedAt);
}
