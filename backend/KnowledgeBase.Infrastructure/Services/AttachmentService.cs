using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 文档附件服务，负责附件文件落盘与元数据维护。
/// </summary>
public sealed class AttachmentService(KnowledgeDbContext db) : IAttachmentService
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "storage", "attachments");

    public async Task<AttachmentDto> SaveAsync(Guid documentId, string fileName, string contentType, long size, Stream stream, Guid? uploaderId, CancellationToken ct)
    {
        Directory.CreateDirectory(Root);
        var stored = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var full = Path.Combine(Root, stored);
        await using (var fs = File.Create(full))
        {
            await stream.CopyToAsync(fs, ct);
        }

        var entity = new DocumentAttachment(documentId, fileName, stored, contentType, size, Path.Combine("storage", "attachments", stored), uploaderId);
        db.DocumentAttachments.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<IReadOnlyList<AttachmentDto>> GetListAsync(Guid documentId, CancellationToken ct)
        => await db.DocumentAttachments.AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AttachmentDto(x.Id, x.DocumentId, x.FileName, x.ContentType, x.Size, $"/api/attachments/{x.Id}/download", x.CreatedAt))
            .ToListAsync(ct);

    public async Task<(Guid DocumentId, string Path, string FileName, string ContentType)?> GetAsync(Guid id, CancellationToken ct)
    {
        var x = await db.DocumentAttachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (x is null) return null;
        return (x.DocumentId, Path.Combine(AppContext.BaseDirectory, x.RelativePath), x.FileName, x.ContentType);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var x = await db.DocumentAttachments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (x is null) return false;
        var full = Path.Combine(AppContext.BaseDirectory, x.RelativePath);
        if (File.Exists(full)) File.Delete(full);
        db.DocumentAttachments.Remove(x);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static AttachmentDto Map(DocumentAttachment x)
        => new(x.Id, x.DocumentId, x.FileName, x.ContentType, x.Size, $"/api/attachments/{x.Id}/download", x.CreatedAt);
}
