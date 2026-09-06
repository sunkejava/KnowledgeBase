namespace KnowledgeBase.Domain.Entities;

/// <summary>
/// 知识文档聚合根。列表查询不加载正文，避免知识库文档量增加后拖慢页面。
/// </summary>
public sealed class Document
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid KnowledgeBaseId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; } = DocumentStatus.Draft;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Document() { }

    public Document(Guid knowledgeBaseId, string title, string slug, Guid? parentId = null)
    {
        KnowledgeBaseId = knowledgeBaseId;
        Title = title.Trim();
        Slug = slug.Trim();
        ParentId = parentId;
    }

    public void Rename(string title)
    {
        Title = title.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum DocumentStatus
{
    Draft = 0,
    Reviewing = 1,
    Published = 2,
    Archived = 3
}
