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
    /// <summary>分页获取文档评论，并补充评论用户与 @成员信息。</summary>
    public async Task<PageResult<DocumentCommentDto>> GetCommentsAsync(Guid documentId, PageQuery query, CancellationToken ct)
    {
        var source = db.DocumentComments.AsNoTracking().Where(x => x.DocumentId == documentId);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.Content.Contains(keyword));
        }

        var total = await source.LongCountAsync(ct);
        var comments = await source.OrderByDescending(x => x.CreatedAt)
            .Skip((query.NormalizedPage - 1) * query.NormalizedPageSize)
            .Take(query.NormalizedPageSize)
            .ToListAsync(ct);

        if (comments.Count == 0)
            return new PageResult<DocumentCommentDto>([], total, query.NormalizedPage, query.NormalizedPageSize);

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

        return new PageResult<DocumentCommentDto>(items, total, query.NormalizedPage, query.NormalizedPageSize);
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

        var query =
            from member in db.KnowledgeBaseMembers.AsNoTracking()
            join user in db.Users.AsNoTracking() on member.UserId equals user.Id
            where member.KnowledgeBaseId == knowledgeBaseId.Value && user.Enabled
            select user;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.UserName.Contains(value) || x.DisplayName.Contains(value));
        }

        return await query.OrderBy(x => x.UserName)
            .Take(take)
            .Select(x => new MentionUserDto(x.Id, x.UserName, x.DisplayName))
            .ToListAsync(ct);
    }

    /// <summary>创建评论，同时为被 @ 的用户生成站内通知。</summary>
    public async Task<DocumentCommentDto> CreateCommentAsync(Guid documentId, Guid userId, CreateCommentRequest request, CancellationToken ct)
    {
        var comment = new DocumentComment(documentId, userId, request.Content, request.ParentId);
        db.DocumentComments.Add(comment);
        var mentions = await ReplaceMentionsAsync(comment.Id, userId, request.MentionUserIds, ct);

        var author = await db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.UserName, x.DisplayName })
            .FirstOrDefaultAsync(ct);
        var documentTitle = await db.Documents.AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => x.Title)
            .FirstOrDefaultAsync(ct) ?? "文档";

        foreach (var mentionedUserId in mentions)
        {
            db.UserNotifications.Add(new UserNotification(
                mentionedUserId,
                "Mention",
                $"{author?.DisplayName ?? author?.UserName ?? "用户"} 在评论中提到了你",
                $"文档《{documentTitle}》：{BuildPreview(comment.Content)}",
                $"/knowledge-center?documentId={documentId:D}"));
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
        var mentions = await ReplaceMentionsAsync(comment.Id, comment.UserId, request.MentionUserIds, ct);
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
        db.DocumentCommentMentions.RemoveRange(db.DocumentCommentMentions.Where(x => x.CommentId == commentId));
        db.DocumentComments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<PageResult<NotificationDto>> GetNotificationsAsync(Guid userId, PageQuery query, CancellationToken ct)
    {
        var source = db.UserNotifications.AsNoTracking().Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.Title.Contains(keyword) || x.Content.Contains(keyword));
        }

        return source.OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationDto(x.Id, x.Type, x.Title, x.Content, x.TargetUrl, x.IsRead, x.CreatedAt, x.ReadAt))
            .ToPageResultAsync(query.NormalizedPage, query.NormalizedPageSize, ct);
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

    private async Task<IReadOnlyList<Guid>> ReplaceMentionsAsync(Guid commentId, Guid authorId, IReadOnlyList<Guid>? mentionUserIds, CancellationToken ct)
    {
        db.DocumentCommentMentions.RemoveRange(db.DocumentCommentMentions.Where(x => x.CommentId == commentId));
        var requested = (mentionUserIds ?? []).Where(x => x != authorId).Distinct().Take(50).ToArray();
        if (requested.Length == 0) return [];

        var valid = await db.Users.AsNoTracking()
            .Where(x => requested.Contains(x.Id) && x.Enabled)
            .Select(x => x.Id)
            .ToListAsync(ct);
        foreach (var userId in valid) db.DocumentCommentMentions.Add(new DocumentCommentMention(commentId, userId));
        return valid;
    }

    private static string BuildPreview(string value)
        => value.Length <= 120 ? value : value[..120] + "…";
}
