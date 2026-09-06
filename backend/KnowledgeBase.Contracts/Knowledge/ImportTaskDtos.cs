namespace KnowledgeBase.Contracts.Knowledge;

/// <summary>知识库异步导入任务。</summary>
public sealed record ImportTaskDto(
    Guid Id,
    Guid KnowledgeBaseId,
    string Name,
    string SourceType,
    string Status,
    string SourceFileName,
    int TotalCount,
    int ProcessedCount,
    int ImportedCount,
    int SkippedCount,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
