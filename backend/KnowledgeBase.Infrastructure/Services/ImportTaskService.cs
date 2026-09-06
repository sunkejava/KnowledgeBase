using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Common;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 知识库异步导入任务服务。上传源文件通过 IFileStorage 统一保存，再由后台 Worker 顺序处理。
/// </summary>
public sealed class ImportTaskService(
    KnowledgeDbContext db,
    IContentExchangeService contentExchange,
    IFileStorage storage) : IImportTaskService
{
    /// <summary>创建 Markdown ZIP 异步导入任务。</summary>
    public async Task<ImportTaskDto> CreateZipAsync(Guid userId, Guid knowledgeBaseId, string fileName, Stream stream, CancellationToken ct)
    {
        var taskId = Guid.NewGuid();
        var objectKey = $"imports/{taskId:N}.zip";
        await using (var output = await storage.CreateWriteAsync(objectKey, ct))
        {
            await stream.CopyToAsync(output, ct);
        }

        var entity = new ImportTask(userId, knowledgeBaseId, fileName, objectKey);
        db.ImportTasks.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    /// <summary>分页获取当前用户导入任务。SQLite 下 CreatedAt 排序使用统一安全分页扩展。</summary>
    public Task<PageResult<ImportTaskDto>> GetPageAsync(Guid userId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return db.ImportTasks.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new ImportTaskDto(
                x.Id, x.KnowledgeBaseId, x.Name, x.SourceType, x.Status, x.SourceFileName,
                x.TotalCount, x.ProcessedCount, x.ImportedCount, x.SkippedCount,
                x.ErrorMessage, x.CreatedAt, x.StartedAt, x.CompletedAt))
            .ToSqliteSafeDateTimeOffsetPageAsync(x => x.CreatedAt, descending: true, page, pageSize, ct);
    }

    /// <summary>取消尚未开始的导入任务。</summary>
    public async Task<bool> CancelAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct)
    {
        var task = await FindOwnedAsync(taskId, userId, isSuperAdmin, ct);
        if (task is null || !task.Cancel()) return false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>重试失败或已取消的导入任务。</summary>
    public async Task<bool> RetryAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct)
    {
        var task = await FindOwnedAsync(taskId, userId, isSuperAdmin, ct);
        if (task is null || !task.Retry()) return false;
        if (!await storage.ExistsAsync(NormalizeLegacyKey(task.RelativePath), ct))
        {
            task.Fail("原始导入文件已不存在，无法重试。");
            await db.SaveChangesAsync(ct);
            return false;
        }
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// 清理指定保留天数之前的已结束导入任务和上传源文件。
    /// SQLite 不直接对 DateTimeOffset 执行范围比较，因此数据库仅做状态/用户过滤，时间阈值在内存中判断。
    /// </summary>
    public async Task<int> CleanupAsync(Guid userId, bool isSuperAdmin, int olderThanDays, CancellationToken ct)
    {
        olderThanDays = Math.Clamp(olderThanDays, 1, 3650);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-olderThanDays);
        var query = db.ImportTasks.Where(x => x.Status != "Pending" && x.Status != "Running");
        if (!isSuperAdmin) query = query.Where(x => x.UserId == userId);
        var candidates = await query.ToListAsync(ct);
        var rows = candidates.Where(x => x.CreatedAt < cutoff).ToList();

        foreach (var row in rows)
            await storage.DeleteAsync(NormalizeLegacyKey(row.RelativePath), ct);
        db.ImportTasks.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    /// <summary>处理队列中最早的待执行任务。SQLite 下先过滤 Pending，再在内存中按 CreatedAt 排序。</summary>
    public async Task ProcessNextPendingAsync(CancellationToken ct)
    {
        var task = await db.ImportTasks
            .Where(x => x.Status == "Pending")
            .FirstOrDefaultSqliteSafeDateTimeOffsetAsync(x => x.CreatedAt, descending: false, ct);
        if (task is null) return;

        task.Start();
        await db.SaveChangesAsync(ct);
        try
        {
            await using var stream = await storage.OpenReadAsync(NormalizeLegacyKey(task.RelativePath), ct)
                ?? throw new FileNotFoundException("导入源文件不存在。");

            var result = await contentExchange.ImportMarkdownZipAsync(
                task.KnowledgeBaseId,
                stream,
                async (total, processed, imported, skipped, token) =>
                {
                    task.UpdateProgress(total, processed, imported, skipped);
                    await db.SaveChangesAsync(token);
                },
                ct);

            task.Complete(result.TotalEntries, result.ImportedCount, result.SkippedCount);
        }
        catch (Exception ex)
        {
            task.Fail(ex.Message);
        }

        await db.SaveChangesAsync(ct);
    }

    private Task<ImportTask?> FindOwnedAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct)
        => db.ImportTasks.FirstOrDefaultAsync(x => x.Id == taskId && (isSuperAdmin || x.UserId == userId), ct);

    private static string NormalizeLegacyKey(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        return normalized.StartsWith("storage/", StringComparison.OrdinalIgnoreCase)
            ? normalized["storage/".Length..]
            : normalized;
    }

    private static ImportTaskDto Map(ImportTask x)
        => new(
            x.Id, x.KnowledgeBaseId, x.Name, x.SourceType, x.Status, x.SourceFileName,
            x.TotalCount, x.ProcessedCount, x.ImportedCount, x.SkippedCount,
            x.ErrorMessage, x.CreatedAt, x.StartedAt, x.CompletedAt);
}
