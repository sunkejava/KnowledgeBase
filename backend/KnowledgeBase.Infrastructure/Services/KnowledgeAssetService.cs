using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Common;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 知识资产服务，负责标签、收藏、最近访问、版本与基础全文搜索。
/// </summary>
public sealed class KnowledgeAssetService(KnowledgeDbContext db) : IKnowledgeAssetService
{
    public async Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken ct)
        => await db.DocumentTags.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new TagDto(x.Id, x.Name, x.Color))
            .ToListAsync(ct);

    public async Task<TagDto> SaveTagAsync(Guid? id, SaveTagRequest request, CancellationToken ct)
    {
        DocumentTag entity;
        if (id is null)
        {
            entity = new DocumentTag(request.Name, request.Color);
            db.DocumentTags.Add(entity);
        }
        else
        {
            entity = await db.DocumentTags.FirstAsync(x => x.Id == id, ct);
            entity.Update(request.Name, request.Color);
        }

        await db.SaveChangesAsync(ct);
        return new(entity.Id, entity.Name, entity.Color);
    }

    public async Task<bool> DeleteTagAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.DocumentTags.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.DocumentTagLinks.RemoveRange(db.DocumentTagLinks.Where(x => x.TagId == id));
        db.DocumentTags.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task SetDocumentTagsAsync(Guid documentId, IReadOnlyList<Guid> tagIds, CancellationToken ct)
    {
        db.DocumentTagLinks.RemoveRange(db.DocumentTagLinks.Where(x => x.DocumentId == documentId));
        foreach (var tagId in tagIds.Distinct())
            db.DocumentTagLinks.Add(new DocumentTagLink(documentId, tagId));

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TagDto>> GetDocumentTagsAsync(Guid documentId, CancellationToken ct)
        => await (
            from link in db.DocumentTagLinks
            join tag in db.DocumentTags on link.TagId equals tag.Id
            where link.DocumentId == documentId
            select new TagDto(tag.Id, tag.Name, tag.Color)
        ).ToListAsync(ct);

    public async Task<bool> ToggleFavoriteAsync(Guid userId, Guid documentId, CancellationToken ct)
    {
        var entity = await db.DocumentFavorites.FirstOrDefaultAsync(
            x => x.UserId == userId && x.DocumentId == documentId, ct);

        if (entity is null)
        {
            db.DocumentFavorites.Add(new DocumentFavorite(userId, documentId));
            await db.SaveChangesAsync(ct);
            return true;
        }

        db.DocumentFavorites.Remove(entity);
        await db.SaveChangesAsync(ct);
        return false;
    }

    /// <summary>
    /// 分页获取当前用户收藏，并支持按文档标题查询。
    /// </summary>
    public Task<PageResult<FavoriteDocumentDto>> GetFavoritesPageAsync(Guid userId, PageQuery query, CancellationToken ct)
    {
        var source =
            from favorite in db.DocumentFavorites.AsNoTracking()
            join document in db.Documents.AsNoTracking() on favorite.DocumentId equals document.Id
            where favorite.UserId == userId
            select new { favorite, document };

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.document.Title.Contains(keyword));
        }

        return source
            .OrderByDescending(x => x.favorite.CreatedAt)
            .Select(x => new FavoriteDocumentDto(x.document.Id, x.document.Title, x.favorite.CreatedAt))
            .ToPageResultAsync(query.NormalizedPage, query.NormalizedPageSize, ct);
    }

    public async Task TrackRecentAsync(Guid userId, Guid documentId, CancellationToken ct)
    {
        var entity = await db.DocumentRecentViews.FirstOrDefaultAsync(
            x => x.UserId == userId && x.DocumentId == documentId, ct);

        if (entity is null) db.DocumentRecentViews.Add(new DocumentRecentView(userId, documentId));
        else entity.Touch();

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// 分页获取最近浏览记录，并支持按文档标题筛选。
    /// </summary>
    public Task<PageResult<RecentDocumentDto>> GetRecentPageAsync(Guid userId, PageQuery query, CancellationToken ct)
    {
        var source =
            from recent in db.DocumentRecentViews.AsNoTracking()
            join document in db.Documents.AsNoTracking() on recent.DocumentId equals document.Id
            where recent.UserId == userId
            select new { recent, document };

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.document.Title.Contains(keyword));
        }

        return source
            .OrderByDescending(x => x.recent.LastViewedAt)
            .Select(x => new RecentDocumentDto(x.document.Id, x.document.Title, x.recent.LastViewedAt, x.recent.ViewCount))
            .ToPageResultAsync(query.NormalizedPage, query.NormalizedPageSize, ct);
    }

    public async Task<VersionDto?> CreateVersionAsync(Guid documentId, Guid? editorId, string changeNote, CancellationToken ct)
    {
        var document = await db.Documents.FirstOrDefaultAsync(x => x.Id == documentId, ct);
        if (document is null) return null;

        var markdown = await db.DocumentContents
            .Where(x => x.DocumentId == documentId)
            .Select(x => x.Markdown)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var versionNumber = (await db.DocumentVersions
            .Where(x => x.DocumentId == documentId)
            .MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0) + 1;

        var version = new DocumentVersion(
            documentId,
            versionNumber,
            document.Title,
            document.Slug,
            markdown,
            editorId,
            changeNote);

        db.DocumentVersions.Add(version);
        await db.SaveChangesAsync(ct);
        return new(version.Id, version.DocumentId, version.VersionNumber, version.Title, version.ChangeNote, version.CreatedAt);
    }

    public async Task<IReadOnlyList<VersionDto>> GetVersionsAsync(Guid documentId, CancellationToken ct)
        => await db.DocumentVersions.AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new VersionDto(x.Id, x.DocumentId, x.VersionNumber, x.Title, x.ChangeNote, x.CreatedAt))
            .ToListAsync(ct);

    public async Task<VersionDetailDto?> GetVersionAsync(Guid versionId, CancellationToken ct)
        => await db.DocumentVersions.AsNoTracking()
            .Where(x => x.Id == versionId)
            .Select(x => new VersionDetailDto(
                x.Id,
                x.DocumentId,
                x.VersionNumber,
                x.Title,
                x.Slug,
                x.Markdown,
                x.ChangeNote,
                x.CreatedAt))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> RestoreVersionAsync(Guid versionId, Guid? editorId, CancellationToken ct)
    {
        var version = await db.DocumentVersions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == versionId, ct);
        if (version is null) return false;

        var document = await db.Documents.FirstOrDefaultAsync(x => x.Id == version.DocumentId, ct);
        if (document is null) return false;

        var content = await db.DocumentContents.FirstOrDefaultAsync(x => x.DocumentId == document.Id, ct);
        if (content is null) return false;

        await CreateVersionAsync(document.Id, editorId, $"恢复前自动备份 v{version.VersionNumber}", ct);
        document.UpdateMetadata(version.Title, version.Slug, document.ParentId);
        content.Update(version.Markdown);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// 基础全文搜索。超级管理员可检索全部知识库；普通用户仅检索自己作为成员可访问的知识库。
    /// </summary>
    public async Task<PageResult<SearchResultDto>> SearchPageAsync(
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

        var items = rows
            .Select(x => new SearchResultDto(
                x.Id,
                x.KnowledgeBaseId,
                x.Title,
                BuildSnippet(x.Markdown, keyword),
                x.UpdatedAt))
            .ToList();

        return new PageResult<SearchResultDto>(items, total, query.NormalizedPage, query.NormalizedPageSize);
    }

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
