using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 知识库异步导入任务服务。上传文件统一写入 storage/imports，再由后台 Worker 顺序处理。
/// </summary>
public sealed class ImportTaskService(KnowledgeDbContext db, IContentExchangeService contentExchange) : IImportTaskService
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "storage", "imports");

    /// <summary>创建 Markdown ZIP 异步导入任务。</summary>
    public async Task<ImportTaskDto> CreateZipAsync(Guid userId, Guid knowledgeBaseId, string fileName, Stream stream, CancellationToken ct)
    {
        Directory.CreateDirectory(Root);
        var taskId = Guid.NewGuid();
        var stored = $"{taskId:N}.zip";
        var full = Path.Combine(Root, stored);
        await using (var output = File.Create(full))
        {
            await stream.CopyToAsync(output, ct);
        }

        var entity = new ImportTask(userId, knowledgeBaseId, fileName, Path.Combine("storage", "imports", stored));
        db.ImportTasks.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<PageResult<ImportTaskDto>> GetPageAsync(Guid userId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.ImportTasks.AsNoTracking().Where(x => x.UserId == userId);
        var total = await query.LongCountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ImportTaskDto(
                x.Id, x.KnowledgeBaseId, x.Name, x.SourceType, x.Status, x.SourceFileName,
                x.TotalCount, x.ProcessedCount, x.ImportedCount, x.SkippedCount,
                x.ErrorMessage, x.CreatedAt, x.StartedAt, x.CompletedAt))
            .ToListAsync(ct);
        return new(items, total, page, pageSize);
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
        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, task.RelativePath)))
        {
            task.Fail("原始导入文件已不存在，无法重试。");
            await db.SaveChangesAsync(ct);
            return false;
        }
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>清理指定保留天数之前的已结束导入任务和上传源文件。</summary>
    public async Task<int> CleanupAsync(Guid userId, bool isSuperAdmin, int olderThanDays, CancellationToken ct)
    {
        olderThanDays = Math.Clamp(olderThanDays, 1, 3650);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-olderThanDays);
        var query = db.ImportTasks.Where(x => x.CreatedAt < cutoff && x.Status != "Pending" && x.Status != "Running");
        if (!isSuperAdmin) query = query.Where(x => x.UserId == userId);
        var rows = await query.ToListAsync(ct);
        foreach (var row in rows)
        {
            var full = Path.Combine(AppContext.BaseDirectory, row.RelativePath);
            if (File.Exists(full)) File.Delete(full);
        }
        db.ImportTasks.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    /// <summary>处理队列中最早的待执行任务。</summary>
    public async Task ProcessNextPendingAsync(CancellationToken ct)
    {
        var task = await db.ImportTasks.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Status == "Pending", ct);
        if (task is null) return;

        task.Start();
        await db.SaveChangesAsync(ct);
        try
        {
            var full = Path.Combine(AppContext.BaseDirectory, task.RelativePath);
            if (!File.Exists(full)) throw new FileNotFoundException("导入源文件不存在。", full);

            await using var stream = File.OpenRead(full);
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

    private static ImportTaskDto Map(ImportTask x)
        => new(
            x.Id, x.KnowledgeBaseId, x.Name, x.SourceType, x.Status, x.SourceFileName,
            x.TotalCount, x.ProcessedCount, x.ImportedCount, x.SkippedCount,
            x.ErrorMessage, x.CreatedAt, x.StartedAt, x.CompletedAt);
}
