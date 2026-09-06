using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Collaboration;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Common;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 文档协作与通知服务实现。评论和通知统一持久化，后续实时推送只消费通知表即可。
/// </summary>
public sealed class CollaborationService(KnowledgeDbContext db) : ICollaborationService
{
    /// <summary>分页获取文档评论，并补充评论用户与 @成员信息。SQLite 下 CreatedAt 排序使用统一安全分页扩展。</summary>
    public async Task<PageResult<DocumentCommentDto>> GetCommentsAsync(Guid documentId, PageQuery query, CancellationToken ct)
    {
        var source = db.DocumentComments.AsNoTracking().Where(x => x.DocumentId == documentId);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.Content.Contains(keyword));
        }

        var page = await source.ToSqliteSafeDateTimeOffsetPageAsync(
            x => x.CreatedAt,
            descending: true,
            query.NormalizedPage,
            query.NormalizedPageSize,
            ct);
        var comments = page.Items.ToList();

        if (comments.Count == 0)
            return new PageResult<DocumentCommentDto>([], page.Total, page.Page, page.PageSize);

        var userIds = comments.Select(x => x.UserId).Distinct().ToArray();
        var users = await db.Users.AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.UserName, x.DisplayName })
            .ToDictionaryAsync(x => x.Id, ct);

        var commentIds = comments.Select(x => x.Id).ToArray();
        var mentions = await db.DocumentCommentMentions.AsNoTracking()
            .Where(x => commentIds.Contains(x.CommentId))
            .ToListAsync(ct);
        var mentionMap = mentions.GroupBy(x => x.CommentId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<Guid>)x.Select(m => m.UserId).ToList());

        var items = comments.Select(x =>
        {
            users.TryGetValue(x.UserId, out var user);
            return new DocumentCommentDto(
                x.Id,
                x.DocumentId,
                x.UserId,
                user?.UserName ?? "unknown",
                user?.DisplayName ?? "未知用户",
                x.ParentId,
                x.Content,
                mentionMap.TryGetValue(x.Id, out var mentionIds) ? mentionIds : [],
                x.CreatedAt,
                x.UpdatedAt);
        }).ToList();

        return new PageResult<DocumentCommentDto>(items, page.Total, page.Page, page.PageSize);
    }

    /// <summary>获取文档所在知识库的成员候选，只返回评论 @成员需要的最小用户信息。</summary>
    public async Task<IReadOnlyList<MentionUserDto>> GetMentionUsersAsync(Guid documentId, string? keyword, int take, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 50);
        var knowledgeBaseId = await db.Documents.AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => (Guid?)x.KnowledgeBaseId)
            .FirstOrDefaultAsync(ct);
        if (!knowledgeBaseId.HasValue) return [];

        var userQuery =
            from member in db.KnowledgeBaseMembers.AsNoTracking()
            join user in db.Users.AsNoTracking() on member.UserId equals user.Id
            where member.KnowledgeBaseId == knowledgeBaseId.Value && user.Enabled
            select user;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            userQuery = userQuery.Where(x => x.UserName.Contains(value) || x.DisplayName.Contains(value));
        }

        return await userQuery.OrderBy(x => x.UserName)
            .Take(take)
            .Select(x => new MentionUserDto(x.Id, x.UserName, x.DisplayName))
            .ToListAsync(ct);
    }

    /// <summary>创建评论，同时为被 @ 的知识库成员生成站内通知。</summary>
    public async Task<DocumentCommentDto> CreateCommentAsync(Guid documentId, Guid userId, CreateCommentRequest request, CancellationToken ct)
    {
        var documentInfo = await db.Documents.AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => new { x.KnowledgeBaseId, x.Title })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("文档不存在。");

        var comment = new DocumentComment(documentId, userId, request.Content, request.ParentId);
        db.DocumentComments.Add(comment);
        var mentions = await ReplaceMentionsAsync(comment.Id, documentId, userId, request.MentionUserIds, ct);

        var author = await db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.UserName, x.DisplayName })
            .FirstOrDefaultAsync(ct);

        foreach (var mentionedUserId in mentions)
        {
            db.UserNotifications.Add(new UserNotification(
                mentionedUserId,
                "Mention",
                $"{author?.DisplayName ?? author?.UserName ?? "用户"} 在评论中提到了你",
                $"文档《{documentInfo.Title}》：{BuildPreview(comment.Content)}",
                $"/knowledge-bases/{documentInfo.KnowledgeBaseId:D}?document={documentId:D}"));
        }

        await db.SaveChangesAsync(ct);
        return new DocumentCommentDto(
            comment.Id,
            comment.DocumentId,
            comment.UserId,
            author?.UserName ?? "unknown",
            author?.DisplayName ?? "未知用户",
            comment.ParentId,
            comment.Content,
            mentions,
            comment.CreatedAt,
            comment.UpdatedAt);
    }

    /// <summary>更新评论正文与 @成员关系。</summary>
    public async Task<DocumentCommentDto?> UpdateCommentAsync(Guid commentId, Guid userId, bool isSuperAdmin, UpdateCommentRequest request, CancellationToken ct)
    {
        var comment = await db.DocumentComments.FirstOrDefaultAsync(x => x.Id == commentId, ct);
        if (comment is null || (!isSuperAdmin && comment.UserId != userId)) return null;
        comment.Update(request.Content);
        var mentions = await ReplaceMentionsAsync(comment.Id, comment.DocumentId, comment.UserId, request.MentionUserIds, ct);
        await db.SaveChangesAsync(ct);

        var author = await db.Users.AsNoTracking()
            .Where(x => x.Id == comment.UserId)
            .Select(x => new { x.UserName, x.DisplayName })
            .FirstOrDefaultAsync(ct);
        return new DocumentCommentDto(
            comment.Id,
            comment.DocumentId,
            comment.UserId,
            author?.UserName ?? "unknown",
            author?.DisplayName ?? "未知用户",
            comment.ParentId,
            comment.Content,
            mentions,
            comment.CreatedAt,
            comment.UpdatedAt);
    }

    /// <summary>删除评论，同时删除其 @成员关系。</summary>
    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, bool isSuperAdmin, CancellationToken ct)
    {
        var comment = await db.DocumentComments.FirstOrDefaultAsync(x => x.Id == commentId, ct);
        if (comment is null || (!isSuperAdmin && comment.UserId != userId)) return false;
        await db.DocumentCommentMentions.Where(x => x.CommentId == commentId).ExecuteDeleteAsync(ct);
        db.DocumentComments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>分页获取当前用户通知。SQLite 下 CreatedAt 排序使用统一安全分页扩展。</summary>
    public Task<PageResult<NotificationDto>> GetNotificationsAsync(Guid userId, PageQuery query, CancellationToken ct)
    {
        var source = db.UserNotifications.AsNoTracking().Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.Title.Contains(keyword) || x.Content.Contains(keyword));
        }

        return source
            .Select(x => new NotificationDto(x.Id, x.Type, x.Title, x.Content, x.TargetUrl, x.IsRead, x.CreatedAt, x.ReadAt))
            .ToSqliteSafeDateTimeOffsetPageAsync(
                x => x.CreatedAt,
                descending: true,
                query.NormalizedPage,
                query.NormalizedPageSize,
                ct);
    }

    public async Task<NotificationSummaryDto> GetNotificationSummaryAsync(Guid userId, CancellationToken ct)
        => new(await db.UserNotifications.AsNoTracking().LongCountAsync(x => x.UserId == userId && !x.IsRead, ct));

    public async Task<bool> MarkNotificationReadAsync(Guid notificationId, Guid userId, CancellationToken ct)
    {
        var notification = await db.UserNotifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, ct);
        if (notification is null) return false;
        notification.MarkRead();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> MarkAllNotificationsReadAsync(Guid userId, CancellationToken ct)
    {
        var rows = await db.UserNotifications.Where(x => x.UserId == userId && !x.IsRead).ToListAsync(ct);
        foreach (var row in rows) row.MarkRead();
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    private async Task<IReadOnlyList<Guid>> ReplaceMentionsAsync(Guid commentId, Guid documentId, Guid authorId, IReadOnlyList<Guid>? mentionUserIds, CancellationToken ct)
    {
        await db.DocumentCommentMentions.Where(x => x.CommentId == commentId).ExecuteDeleteAsync(ct);
        var requested = (mentionUserIds ?? []).Where(x => x != authorId).Distinct().Take(50).ToArray();
        if (requested.Length == 0) return [];

        var knowledgeBaseId = await db.Documents.AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => (Guid?)x.KnowledgeBaseId)
            .FirstOrDefaultAsync(ct);
        if (!knowledgeBaseId.HasValue) return [];

        var valid = await (
            from member in db.KnowledgeBaseMembers.AsNoTracking()
            join user in db.Users.AsNoTracking() on member.UserId equals user.Id
            where member.KnowledgeBaseId == knowledgeBaseId.Value
                  && requested.Contains(user.Id)
                  && user.Enabled
            select user.Id)
            .Distinct()
            .ToListAsync(ct);

        foreach (var mentionedUserId in valid)
            db.DocumentCommentMentions.Add(new DocumentCommentMention(commentId, mentionedUserId));
        return valid;
    }

    private static string BuildPreview(string value)
        => value.Length <= 120 ? value : value[..120] + "…";
}
