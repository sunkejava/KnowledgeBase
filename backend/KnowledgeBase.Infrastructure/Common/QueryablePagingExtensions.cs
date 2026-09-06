using KnowledgeBase.Contracts.Common;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Common;

/// <summary>
/// IQueryable 分页扩展方法，统一后端列表接口的分页处理方式。
/// </summary>
public static class QueryablePagingExtensions
{
    /// <summary>
    /// 将查询转换为统一分页结果。
    /// </summary>
    /// <typeparam name="T">查询实体或投影类型。</typeparam>
    /// <param name="query">待分页查询。</param>
    /// <param name="page">页码，从 1 开始。</param>
    /// <param name="pageSize">每页条数，最大 200。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static async Task<PageResult<T>> ToPageResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<T>(items, total, normalizedPage, normalizedPageSize);
    }

    /// <summary>
    /// SQLite 安全的 DateTimeOffset 排序分页。
    /// SQLite provider 不能将 DateTimeOffset 直接翻译到 ORDER BY，
    /// 因此先在数据库完成过滤与投影，再在内存中排序并分页。
    /// </summary>
    /// <remarks>
    /// 该方法适用于中小规模列表。对于超大数据量场景，应在表中额外维护可排序的 UTC Unix 毫秒时间列，
    /// 例如 CreatedAtUnixMs，并在数据库层对该 long 字段进行 ORDER BY + Skip/Take。
    /// 禁止在 SQLite 项目中直接对 DateTimeOffset 属性调用 OrderBy/OrderByDescending。
    /// </remarks>
    public static async Task<PageResult<T>> ToSqliteSafeDateTimeOffsetPageAsync<T>(
        this IQueryable<T> query,
        Func<T, DateTimeOffset> keySelector,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query.ToListAsync(cancellationToken);
        var ordered = descending ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector);
        var items = ordered
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return new PageResult<T>(items, total, normalizedPage, normalizedPageSize);
    }

    /// <summary>
    /// SQLite 安全的可空 DateTimeOffset 排序分页。
    /// </summary>
    public static async Task<PageResult<T>> ToSqliteSafeNullableDateTimeOffsetPageAsync<T>(
        this IQueryable<T> query,
        Func<T, DateTimeOffset?> keySelector,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query.ToListAsync(cancellationToken);
        var ordered = descending ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector);
        var items = ordered
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return new PageResult<T>(items, total, normalizedPage, normalizedPageSize);
    }

    /// <summary>
    /// SQLite 安全的 DateTimeOffset 排序列表。
    /// </summary>
    public static async Task<IReadOnlyList<T>> ToSqliteSafeDateTimeOffsetListAsync<T>(
        this IQueryable<T> query,
        Func<T, DateTimeOffset> keySelector,
        bool descending,
        CancellationToken cancellationToken = default)
    {
        var rows = await query.ToListAsync(cancellationToken);
        return descending ? rows.OrderByDescending(keySelector).ToList() : rows.OrderBy(keySelector).ToList();
    }

    /// <summary>
    /// SQLite 安全地取得按 DateTimeOffset 排序后的第一条记录。
    /// 主要用于任务队列获取最早 Pending 任务。
    /// </summary>
    public static async Task<T?> FirstOrDefaultSqliteSafeDateTimeOffsetAsync<T>(
        this IQueryable<T> query,
        Func<T, DateTimeOffset> keySelector,
        bool descending,
        CancellationToken cancellationToken = default)
    {
        var rows = await query.ToListAsync(cancellationToken);
        return descending ? rows.OrderByDescending(keySelector).FirstOrDefault() : rows.OrderBy(keySelector).FirstOrDefault();
    }
}
