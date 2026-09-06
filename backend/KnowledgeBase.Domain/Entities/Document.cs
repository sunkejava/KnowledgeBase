namespace KnowledgeBase.Domain.Entities;

/// <summary>知识文档聚合根。正文独立存储，列表查询不加载大字段。</summary>
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
        UpdateMetadata(title, slug, parentId);
    }

    public void UpdateMetadata(string title, string slug, Guid? parentId)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("文档标题不能为空", nameof(title));
        Title = title.Trim()[..Math.Min(title.Trim().Length, 200)];
        Slug = string.IsNullOrWhiteSpace(slug) ? Id.ToString("N") : slug.Trim()[..Math.Min(slug.Trim().Length, 220)];
        ParentId = parentId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Publish() { Status = DocumentStatus.Published; UpdatedAt = DateTimeOffset.UtcNow; }
}

public sealed class DocumentContent
{
    public Guid DocumentId { get; private set; }
    public string Markdown { get; private set; } = string.Empty;
    public string PlainText { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private DocumentContent() { }
    public DocumentContent(Guid documentId, string markdown) { DocumentId = documentId; Update(markdown); }
    public void Update(string markdown)
    {
        Markdown = markdown ?? string.Empty;
        PlainText = Markdown.Replace("#", string.Empty).Replace("*", string.Empty).Replace("`", string.Empty);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum DocumentStatus { Draft = 0, Reviewing = 1, Published = 2, Archived = 3 }
