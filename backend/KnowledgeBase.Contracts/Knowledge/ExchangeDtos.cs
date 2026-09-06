namespace KnowledgeBase.Contracts.Knowledge;

public sealed record ExportDocumentDto(Guid Id,string Title,string Slug,string Markdown,string FileName);
public sealed record ImportMarkdownResultDto(Guid DocumentId,string Title,string Slug);
public sealed record DiffLineDto(string Type,int OldLine,int NewLine,string Text);
public sealed record DocumentDiffDto(Guid DocumentId,int? VersionNumber,IReadOnlyList<DiffLineDto> Lines);
public sealed record ShareLinkDto(Guid Id,Guid DocumentId,string Token,DateTimeOffset? ExpiresAt,bool Enabled,DateTimeOffset CreatedAt);
public sealed record CreateShareLinkRequest(DateTimeOffset? ExpiresAt);
public sealed record SharedDocumentDto(Guid DocumentId,string Title,string Markdown,DateTimeOffset? ExpiresAt);
