namespace KnowledgeBase.Contracts.Knowledge;

/// <summary>搜索引擎状态。</summary>
public sealed record SearchProviderStatusDto(string Provider, bool External, string? Endpoint);

/// <summary>索引重建任务。</summary>
public sealed record SearchIndexTaskDto(
    Guid Id,
    string Provider,
    string Status,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
