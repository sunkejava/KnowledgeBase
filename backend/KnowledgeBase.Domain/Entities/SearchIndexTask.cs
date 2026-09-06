namespace KnowledgeBase.Domain.Entities;

/// <summary>
/// 搜索索引重建任务。用于持久化记录索引重建状态，避免管理员请求线程长期阻塞。
/// </summary>
public sealed class SearchIndexTask
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private SearchIndexTask() { }

    public SearchIndexTask(Guid userId, string provider)
    {
        UserId = userId;
        Provider = string.IsNullOrWhiteSpace(provider) ? "sqlite" : provider.Trim().ToLowerInvariant();
    }

    /// <summary>标记任务开始执行。</summary>
    public void Start()
    {
        Status = "Running";
        StartedAt = DateTimeOffset.UtcNow;
        CompletedAt = null;
        ErrorMessage = null;
    }

    /// <summary>标记任务执行完成。</summary>
    public void Complete()
    {
        Status = "Completed";
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = null;
    }

    /// <summary>标记任务执行失败。</summary>
    public void Fail(string message)
    {
        Status = "Failed";
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = string.IsNullOrWhiteSpace(message)
            ? "索引重建失败"
            : message[..Math.Min(message.Length, 1000)];
    }

    /// <summary>将失败任务重新置为待处理。</summary>
    public bool Retry()
    {
        if (Status != "Failed") return false;
        Status = "Pending";
        StartedAt = null;
        CompletedAt = null;
        ErrorMessage = null;
        return true;
    }
}
