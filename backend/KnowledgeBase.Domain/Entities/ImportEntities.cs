namespace KnowledgeBase.Domain.Entities;

/// <summary>
/// 知识库导入任务。上传文件先保存到临时目录，再由后台 Worker 异步处理。
/// </summary>
public sealed class ImportTask
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid KnowledgeBaseId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string SourceType { get; private set; } = "zip";
    public string Status { get; private set; } = "Pending";
    public string SourceFileName { get; private set; } = string.Empty;
    public string RelativePath { get; private set; } = string.Empty;
    public int TotalCount { get; private set; }
    public int ProcessedCount { get; private set; }
    public int ImportedCount { get; private set; }
    public int SkippedCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private ImportTask() { }

    public ImportTask(Guid userId, Guid knowledgeBaseId, string sourceFileName, string relativePath)
    {
        UserId = userId;
        KnowledgeBaseId = knowledgeBaseId;
        SourceFileName = sourceFileName;
        RelativePath = relativePath;
        Name = $"{sourceFileName} - Markdown ZIP 导入";
    }

    /// <summary>将任务标记为正在处理。</summary>
    public void Start()
    {
        Status = "Running";
        StartedAt = DateTimeOffset.UtcNow;
        CompletedAt = null;
        ErrorMessage = null;
        TotalCount = 0;
        ProcessedCount = 0;
        ImportedCount = 0;
        SkippedCount = 0;
    }

    /// <summary>更新任务处理进度。</summary>
    public void UpdateProgress(int totalCount, int processedCount, int importedCount, int skippedCount)
    {
        TotalCount = Math.Max(totalCount, 0);
        ProcessedCount = Math.Max(processedCount, 0);
        ImportedCount = Math.Max(importedCount, 0);
        SkippedCount = Math.Max(skippedCount, 0);
    }

    /// <summary>将任务标记为完成。</summary>
    public void Complete(int totalCount, int importedCount, int skippedCount)
    {
        TotalCount = totalCount;
        ProcessedCount = totalCount;
        ImportedCount = importedCount;
        SkippedCount = skippedCount;
        Status = "Completed";
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>将任务标记为失败。</summary>
    public void Fail(string error)
    {
        Status = "Failed";
        ErrorMessage = string.IsNullOrWhiteSpace(error) ? "导入任务执行失败" : error[..Math.Min(error.Length, 1000)];
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>取消尚未开始的任务。</summary>
    public bool Cancel()
    {
        if (Status != "Pending") return false;
        Status = "Cancelled";
        CompletedAt = DateTimeOffset.UtcNow;
        return true;
    }

    /// <summary>将失败或已取消任务重新放回队列。</summary>
    public bool Retry()
    {
        if (Status is not ("Failed" or "Cancelled")) return false;
        Status = "Pending";
        StartedAt = null;
        CompletedAt = null;
        ErrorMessage = null;
        TotalCount = 0;
        ProcessedCount = 0;
        ImportedCount = 0;
        SkippedCount = 0;
        return true;
    }
}
