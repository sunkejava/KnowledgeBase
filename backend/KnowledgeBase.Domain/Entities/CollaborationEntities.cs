namespace KnowledgeBase.Domain.Entities;

/// <summary>
/// 文档评论实体。
/// </summary>
public sealed class DocumentComment
{
    private DocumentComment() { }

    public DocumentComment(Guid documentId, Guid userId, string content, Guid? parentId = null)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        UserId = userId;
        ParentId = parentId;
        Content = NormalizeContent(content);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>修改评论正文。</summary>
    public void Update(string content)
    {
        Content = NormalizeContent(content);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string NormalizeContent(string content)
    {
        var value = (content ?? string.Empty).Trim();
        if (value.Length == 0) throw new ArgumentException("评论内容不能为空。", nameof(content));
        if (value.Length > 4000) throw new ArgumentException("评论内容不能超过 4000 个字符。", nameof(content));
        return value;
    }
}

/// <summary>
/// 评论中被 @ 的用户关系。
/// </summary>
public sealed class DocumentCommentMention
{
    private DocumentCommentMention() { }

    public DocumentCommentMention(Guid commentId, Guid userId)
    {
        CommentId = commentId;
        UserId = userId;
    }

    public Guid CommentId { get; private set; }
    public Guid UserId { get; private set; }
}

/// <summary>
/// 站内通知实体。通知先持久化，后续可由 SignalR、邮件或其他通道消费。
/// </summary>
public sealed class UserNotification
{
    private UserNotification() { }

    public UserNotification(Guid userId, string type, string title, string content, string? targetUrl)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = string.IsNullOrWhiteSpace(type) ? "System" : type.Trim();
        Title = (title ?? string.Empty).Trim();
        Content = (content ?? string.Empty).Trim();
        TargetUrl = string.IsNullOrWhiteSpace(targetUrl) ? null : targetUrl.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? TargetUrl { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>标记通知为已读。</summary>
    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
    }
}
