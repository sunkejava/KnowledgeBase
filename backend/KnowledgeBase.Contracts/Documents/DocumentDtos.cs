namespace KnowledgeBase.Contracts.Documents;

public sealed record DocumentListItemDto(Guid Id, string Title, string Status, DateTimeOffset UpdatedAt);
public sealed record CreateDocumentRequest(Guid KnowledgeBaseId, Guid? ParentId, string Title, string Slug, string Markdown);
public sealed record DocumentDetailDto(Guid Id, string Title, string Slug, string Status, string Markdown, DateTimeOffset UpdatedAt);
