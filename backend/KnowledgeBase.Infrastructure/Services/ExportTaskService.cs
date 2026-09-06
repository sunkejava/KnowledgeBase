using System.IO.Compression;
using System.Text;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 知识库服务端导出任务实现。导出文件统一写入 storage/exports。
/// </summary>
public sealed class ExportTaskService(KnowledgeDbContext db) : IExportTaskService
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "storage", "exports");

    public async Task<ExportTaskDto> CreateAsync(Guid userId, Guid knowledgeBaseId, string format, CancellationToken ct)
    {
        var name = await db.KnowledgeBases.AsNoTracking().Where(x => x.Id == knowledgeBaseId).Select(x => x.Name).FirstAsync(ct);
        var normalized = string.IsNullOrWhiteSpace(format) ? "zip" : format.Trim().ToLowerInvariant();
        if (normalized != "zip") throw new ArgumentOutOfRangeException(nameof(format), "当前仅支持 ZIP 导出。");

        var entity = new ExportTask(userId, knowledgeBaseId, $"{name} - Markdown ZIP", normalized);
        db.ExportTasks.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<PageResult<ExportTaskDto>> GetPageAsync(Guid userId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.ExportTasks.AsNoTracking().Where(x => x.UserId == userId);
        var total = await query.LongCountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ExportTaskDto(x.Id, x.KnowledgeBaseId, x.Name, x.Format, x.Status, x.FileName, x.ErrorMessage, x.CreatedAt, x.StartedAt, x.CompletedAt))
            .ToListAsync(ct);
        return new(items, total, page, pageSize);
    }

    public async Task<(string Path, string FileName)?> GetFileAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct)
    {
        var row = await db.ExportTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == taskId && (isSuperAdmin || x.UserId == userId), ct);
        if (row is null || row.Status != "Completed" || string.IsNullOrWhiteSpace(row.RelativePath) || string.IsNullOrWhiteSpace(row.FileName)) return null;
        return (Path.Combine(AppContext.BaseDirectory, row.RelativePath), row.FileName);
    }

    /// <summary>取消尚未开始的导出任务。</summary>
    public async Task<bool> CancelAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct)
    {
        var task = await FindOwnedAsync(taskId, userId, isSuperAdmin, ct);
        if (task is null || !task.Cancel()) return false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>重试失败或已取消的导出任务。</summary>
    public async Task<bool> RetryAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct)
    {
        var task = await FindOwnedAsync(taskId, userId, isSuperAdmin, ct);
        if (task is null || !task.Retry()) return false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>清理指定保留天数之前的已结束任务及导出文件。</summary>
    public async Task<int> CleanupAsync(Guid userId, bool isSuperAdmin, int olderThanDays, CancellationToken ct)
    {
        olderThanDays = Math.Clamp(olderThanDays, 1, 3650);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-olderThanDays);
        var query = db.ExportTasks.Where(x => x.CreatedAt < cutoff && x.Status != "Pending" && x.Status != "Running");
        if (!isSuperAdmin) query = query.Where(x => x.UserId == userId);
        var rows = await query.ToListAsync(ct);
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.RelativePath))
            {
                var full = Path.Combine(AppContext.BaseDirectory, row.RelativePath);
                if (File.Exists(full)) File.Delete(full);
            }
        }
        db.ExportTasks.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    public async Task ProcessNextPendingAsync(CancellationToken ct)
    {
        var task = await db.ExportTasks.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Status == "Pending", ct);
        if (task is null) return;

        task.Start();
        await db.SaveChangesAsync(ct);
        try
        {
            Directory.CreateDirectory(Root);
            var kbName = await db.KnowledgeBases.AsNoTracking().Where(x => x.Id == task.KnowledgeBaseId).Select(x => x.Name).FirstOrDefaultAsync(ct) ?? "knowledge-base";
            var rows = await (
                from d in db.Documents.AsNoTracking()
                join c in db.DocumentContents.AsNoTracking() on d.Id equals c.DocumentId
                where d.KnowledgeBaseId == task.KnowledgeBaseId
                select new { d.Id, d.ParentId, d.Title, c.Markdown }
            ).ToListAsync(ct);

            var map = rows.ToDictionary(x => x.Id);
            var fileName = $"{Sanitize(kbName)}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip";
            var stored = $"{task.Id:N}.zip";
            var full = Path.Combine(Root, stored);

            await using var stream = File.Create(full);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8);
            foreach (var row in rows.OrderBy(x => BuildEntryPath(x.Id, map), StringComparer.OrdinalIgnoreCase))
            {
                ct.ThrowIfCancellationRequested();
                var entry = archive.CreateEntry(BuildEntryPath(row.Id, map), CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await using var writer = new StreamWriter(entryStream, new UTF8Encoding(false));
                await writer.WriteAsync(row.Markdown.AsMemory(), ct);
            }

            task.Complete(fileName, Path.Combine("storage", "exports", stored));
        }
        catch (Exception ex)
        {
            task.Fail(ex.Message);
        }

        await db.SaveChangesAsync(ct);
    }

    private Task<ExportTask?> FindOwnedAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct)
        => db.ExportTasks.FirstOrDefaultAsync(x => x.Id == taskId && (isSuperAdmin || x.UserId == userId), ct);

    private static string BuildEntryPath(Guid id, IReadOnlyDictionary<Guid, dynamic> map)
    {
        var segments = new Stack<string>();
        var currentId = id;
        var guard = 0;
        while (map.TryGetValue(currentId, out var current) && guard++ < 100)
        {
            segments.Push($"{Sanitize((string)current.Title)}-{current.Id:N}");
            if (current.ParentId is not Guid parentId) break;
            currentId = parentId;
        }

        var items = segments.ToArray();
        if (items.Length == 1) return $"{items[0]}.md";
        return string.Join('/', items[..^1]) + "/" + items[^1] + ".md";
    }

    private static ExportTaskDto Map(ExportTask x)
        => new(x.Id, x.KnowledgeBaseId, x.Name, x.Format, x.Status, x.FileName, x.ErrorMessage, x.CreatedAt, x.StartedAt, x.CompletedAt);

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "knowledge-base" : name;
    }
}
