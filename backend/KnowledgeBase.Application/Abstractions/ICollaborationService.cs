using KnowledgeBase.Contracts.Collaboration;
using KnowledgeBase.Contracts.Common;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 文档协作与通知服务契约。
/// </summary>
public interface ICollaborationService
{
    /// <summary>分页获取文档评论。</summary>
    Task<PageResult<DocumentCommentDto>> GetCommentsAsync(Guid documentId, PageQuery query, CancellationToken ct);

    /// <summary>获取当前文档所在知识库的可 @成员候选。</summary>
    Task<IReadOnlyList<MentionUserDto>> GetMentionUsersAsync(Guid documentId, string? keyword, int take, CancellationToken ct);

    /// <summary>创建评论并为被 @ 的用户生成通知。</summary>
    Task<DocumentCommentDto> CreateCommentAsync(Guid documentId, Guid userId, CreateCommentRequest request, CancellationToken ct);

    /// <summary>修改本人评论或管理员代为修正评论。</summary>
    Task<DocumentCommentDto?> UpdateCommentAsync(Guid commentId, Guid userId, bool isSuperAdmin, UpdateCommentRequest request, CancellationToken ct);

    /// <summary>删除本人评论或管理员删除评论。</summary>
    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, bool isSuperAdmin, CancellationToken ct);

    /// <summary>分页获取当前用户通知。</summary>
    Task<PageResult<NotificationDto>> GetNotificationsAsync(Guid userId, PageQuery query, CancellationToken ct);

    /// <summary>获取当前用户未读数量。</summary>
    Task<NotificationSummaryDto> GetNotificationSummaryAsync(Guid userId, CancellationToken ct);

    /// <summary>标记单条通知为已读。</summary>
    Task<bool> MarkNotificationReadAsync(Guid notificationId, Guid userId, CancellationToken ct);

    /// <summary>标记当前用户全部通知为已读。</summary>
    Task<int> MarkAllNotificationsReadAsync(Guid userId, CancellationToken ct);
}
