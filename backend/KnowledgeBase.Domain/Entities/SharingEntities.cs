namespace KnowledgeBase.Domain.Entities;

/// <summary>文档公开分享链接。</summary>
public sealed class DocumentShareLink
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DocumentId { get; private set; }
    public string Token { get; private set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool Enabled { get; private set; } = true;
    public Guid? CreatedBy { get; private set; }
    public long AccessCount { get; private set; }
    public DateTimeOffset? LastAccessAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private DocumentShareLink() { }

    public DocumentShareLink(Guid documentId, Guid? createdBy, DateTimeOffset? expiresAt, string? passwordHash = null)
    {
        DocumentId = documentId;
        CreatedBy = createdBy;
        ExpiresAt = expiresAt;
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? null : passwordHash;
    }

    /// <summary>停用当前分享链接。</summary>
    public void Disable() => Enabled = false;

    /// <summary>记录一次成功的公开访问。</summary>
    public void RecordAccess()
    {
        AccessCount++;
        LastAccessAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// 分享链接访问日志。成功和失败访问都会落库，便于审计与安全分析。
/// </summary>
public sealed class ShareAccessLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ShareLinkId { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset AccessedAt { get; private set; } = DateTimeOffset.UtcNow;

    private ShareAccessLog() { }

    public ShareAccessLog(Guid shareLinkId, string? ipAddress, string? userAgent, bool success, string message)
    {
        ShareLinkId = shareLinkId;
        IpAddress = (ipAddress ?? string.Empty)[..Math.Min((ipAddress ?? string.Empty).Length, 64)];
        UserAgent = (userAgent ?? string.Empty)[..Math.Min((userAgent ?? string.Empty).Length, 500)];
        Success = success;
        Message = (message ?? string.Empty)[..Math.Min((message ?? string.Empty).Length, 200)];
    }
}
