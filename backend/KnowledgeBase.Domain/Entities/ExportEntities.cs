namespace KnowledgeBase.Domain.Entities;

/// <summary>
/// 知识库导出任务。任务由接口创建，后台 Worker 异步生成导出文件。
/// </summary>
public sealed class ExportTask
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid KnowledgeBaseId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Format { get; private set; } = "zip";
    public string Status { get; private set; } = "Pending";
    public string? FileName { get; private set; }
    public string? RelativePath { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private ExportTask() { }

    public ExportTask(Guid userId, Guid knowledgeBaseId, string name, string format)
    {
        UserId = userId;
        KnowledgeBaseId = knowledgeBaseId;
        Name = string.IsNullOrWhiteSpace(name) ? "知识库导出" : name.Trim();
        Format = string.IsNullOrWhiteSpace(format) ? "zip" : format.Trim().ToLowerInvariant();
    }

    /// <summary>将任务标记为正在处理。</summary>
    public void Start()
    {
        Status = "Running";
        StartedAt = DateTimeOffset.UtcNow;
        ErrorMessage = null;
    }

    /// <summary>将任务标记为完成，并保存生成文件信息。</summary>
    public void Complete(string fileName, string relativePath)
    {
        Status = "Completed";
        FileName = fileName;
        RelativePath = relativePath;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>将任务标记为失败并记录失败原因。</summary>
    public void Fail(string error)
    {
        Status = "Failed";
        ErrorMessage = string.IsNullOrWhiteSpace(error) ? "导出任务执行失败" : error[..Math.Min(error.Length, 1000)];
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
