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
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private DocumentShareLink() { }
    public DocumentShareLink(Guid documentId,Guid? createdBy,DateTimeOffset? expiresAt){DocumentId=documentId;CreatedBy=createdBy;ExpiresAt=expiresAt;}
    public void Disable()=>Enabled=false;
}
