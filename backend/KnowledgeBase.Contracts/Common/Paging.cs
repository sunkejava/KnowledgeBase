namespace KnowledgeBase.Contracts.Common;

/// <summary>
/// 通用分页查询参数。
/// </summary>
public sealed record PageQuery(int Page = 1, int PageSize = 20, string? Keyword = null)
{
    /// <summary>
    /// 获取规范化后的页码，最小值为 1。
    /// </summary>
    public int NormalizedPage => Math.Max(1, Page);

    /// <summary>
    /// 获取规范化后的每页条数，范围限制为 1 到 200。
    /// </summary>
    public int NormalizedPageSize => Math.Clamp(PageSize, 1, 200);
}

/// <summary>
/// 通用分页结果。
/// </summary>
/// <typeparam name="T">列表项类型。</typeparam>
public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    long Total,
    int Page,
    int PageSize)
{
    /// <summary>
    /// 获取总页数。
    /// </summary>
    public long TotalPages => Total == 0 ? 0 : (long)Math.Ceiling(Total / (double)PageSize);
}
