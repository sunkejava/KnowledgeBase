using KnowledgeBase.Contracts.Common;

namespace KnowledgeBase.Contracts.Collaboration;

/// <summary>创建文档评论请求。</summary>
public sealed record CreateCommentRequest(string Content, Guid? ParentId, IReadOnlyList<Guid>? MentionUserIds);

/// <summary>修改评论请求。</summary>
public sealed record UpdateCommentRequest(string Content, IReadOnlyList<Guid>? MentionUserIds);

/// <summary>文档评论 DTO。</summary>
public sealed record DocumentCommentDto(
    Guid Id,
    Guid DocumentId,
    Guid UserId,
    string UserName,
    string DisplayName,
    Guid? ParentId,
    string Content,
    IReadOnlyList<Guid> MentionUserIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>通知 DTO。</summary>
public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Content,
    string? TargetUrl,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

/// <summary>通知统计。</summary>
public sealed record NotificationSummaryDto(long UnreadCount);
