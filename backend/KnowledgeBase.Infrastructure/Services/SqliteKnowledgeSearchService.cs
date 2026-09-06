using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 基于 SQLite/EF Core 的默认全文搜索实现。
/// 适用于本地部署和中小规模知识库，不依赖外部搜索服务。
/// </summary>
public sealed class SqliteKnowledgeSearchService(KnowledgeDbContext db) : IKnowledgeSearchService
{
    public string ProviderName => "sqlite";

    /// <summary>
    /// 在数据库层执行权限过滤、知识库过滤、分页和排序。
    /// </summary>
    public async Task<PageResult<SearchResultDto>> SearchAsync(
        string keyword,
        Guid userId,
        bool isSuperAdmin,
        Guid? knowledgeBaseId,
        PageQuery query,
        CancellationToken ct)
    {
        keyword = (keyword ?? string.Empty).Trim();
        if (keyword.Length == 0)
            return new PageResult<SearchResultDto>([], 0, query.NormalizedPage, query.NormalizedPageSize);

        var source =
            from document in db.Documents.AsNoTracking()
            join content in db.DocumentContents.AsNoTracking() on document.Id equals content.DocumentId
            where document.Title.Contains(keyword) || content.Markdown.Contains(keyword)
            select new { document, content };

        if (knowledgeBaseId.HasValue)
            source = source.Where(x => x.document.KnowledgeBaseId == knowledgeBaseId.Value);

        if (!isSuperAdmin)
        {
            source = source.Where(x => db.KnowledgeBaseMembers.Any(member =>
                member.KnowledgeBaseId == x.document.KnowledgeBaseId && member.UserId == userId));
        }

        var total = await source.LongCountAsync(ct);
        var rows = await source
            .OrderByDescending(x => x.document.UpdatedAt)
            .Skip((query.NormalizedPage - 1) * query.NormalizedPageSize)
            .Take(query.NormalizedPageSize)
            .Select(x => new
            {
                x.document.Id,
                x.document.KnowledgeBaseId,
                x.document.Title,
                x.content.Markdown,
                x.document.UpdatedAt
            })
            .ToListAsync(ct);

        var items = rows.Select(x => new SearchResultDto(
            x.Id,
            x.KnowledgeBaseId,
            x.Title,
            BuildSnippet(x.Markdown, keyword),
            x.UpdatedAt)).ToList();

        return new PageResult<SearchResultDto>(items, total, query.NormalizedPage, query.NormalizedPageSize);
    }

    /// <summary>SQLite 直接查询业务表，不维护外部索引，因此单文档更新无需额外操作。</summary>
    public Task UpsertDocumentAsync(Guid documentId, CancellationToken ct) => Task.CompletedTask;

    /// <summary>SQLite 直接查询业务表，不维护外部索引，因此单文档删除无需额外操作。</summary>
    public Task DeleteDocumentAsync(Guid documentId, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// SQLite 实现直接读取业务表，不维护独立索引，因此无需执行重建动作。
    /// </summary>
    public Task RebuildIndexAsync(CancellationToken ct) => Task.CompletedTask;

    private static string BuildSnippet(string text, string keyword)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return text[..Math.Min(text.Length, 180)];
        var start = Math.Max(0, index - 70);
        var length = Math.Min(text.Length - start, 180);
        return text.Substring(start, length).Replace("\r", " ").Replace("\n", " ");
    }
}
