namespace KnowledgeBase.Contracts.Documents;

public sealed record DocumentListItemDto(Guid Id, Guid? ParentId, string Title, string Status, DateTimeOffset UpdatedAt);
public sealed record CreateDocumentRequest(Guid KnowledgeBaseId, Guid? ParentId, string Title, string Slug, string Markdown);
public sealed record UpdateDocumentRequest(Guid? ParentId, string Title, string Slug, string Markdown);
public sealed record DocumentDetailDto(Guid Id, Guid KnowledgeBaseId, Guid? ParentId, string Title, string Slug, string Status, string Markdown, DateTimeOffset UpdatedAt);
