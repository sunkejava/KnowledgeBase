namespace KnowledgeBase.Contracts.Knowledge;

public sealed record ExportDocumentDto(Guid Id,string Title,string Slug,string Markdown,string FileName);
public sealed record ImportMarkdownResultDto(Guid DocumentId,string Title,string Slug);
public sealed record ImportZipResultDto(int TotalEntries,int ImportedCount,int SkippedCount,IReadOnlyList<ImportMarkdownResultDto> Documents);
public sealed record DiffLineDto(string Type,int OldLine,int NewLine,string Text);
public sealed record DocumentDiffDto(Guid DocumentId,int? VersionNumber,IReadOnlyList<DiffLineDto> Lines);

public sealed record ShareLinkDto(
    Guid Id,
    Guid DocumentId,
    string Token,
    DateTimeOffset? ExpiresAt,
    bool Enabled,
    bool PasswordProtected,
    long AccessCount,
    DateTimeOffset? LastAccessAt,
    DateTimeOffset CreatedAt);

public sealed record CreateShareLinkRequest(DateTimeOffset? ExpiresAt,string? Password);
public sealed record PublicShareAccessRequest(string? Password);
public sealed record SharedDocumentDto(Guid DocumentId,string Title,string Markdown,DateTimeOffset? ExpiresAt);
public sealed record ShareAccessLogDto(Guid Id,Guid ShareLinkId,string IpAddress,string UserAgent,bool Success,string Message,DateTimeOffset AccessedAt);

public sealed record ExportTaskDto(
    Guid Id,
    Guid KnowledgeBaseId,
    string Name,
    string Format,
    string Status,
    string? FileName,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
public sealed record CreateExportTaskRequest(Guid KnowledgeBaseId,string Format = "zip");
