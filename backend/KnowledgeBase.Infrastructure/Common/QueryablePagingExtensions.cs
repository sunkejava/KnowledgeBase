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
}
