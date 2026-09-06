using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 文档附件服务，负责附件元数据维护；实际文件读写统一委托给 IFileStorage。
/// </summary>
public sealed class AttachmentService(KnowledgeDbContext db, IFileStorage storage) : IAttachmentService
{
    /// <summary>保存附件文件并写入附件元数据。</summary>
    public async Task<AttachmentDto> SaveAsync(Guid documentId, string fileName, string contentType, long size, Stream stream, Guid? uploaderId, CancellationToken ct)
    {
        var stored = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var objectKey = $"attachments/{stored}";
        await using (var output = await storage.CreateWriteAsync(objectKey, ct))
        {
            await stream.CopyToAsync(output, ct);
        }

        var entity = new DocumentAttachment(documentId, fileName, stored, contentType, size, objectKey, uploaderId);
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

    /// <summary>打开附件读取流，不向 Controller 暴露底层物理路径。</summary>
    public async Task<(Guid DocumentId, Stream Stream, string FileName, string ContentType)?> OpenReadAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.DocumentAttachments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;
        var stream = await storage.OpenReadAsync(NormalizeLegacyKey(entity.RelativePath), ct);
        return stream is null ? null : (entity.DocumentId, stream, entity.FileName, entity.ContentType);
    }

    /// <summary>获取附件所属文档 ID，用于删除前执行资源权限判断。</summary>
    public Task<Guid?> GetDocumentIdAsync(Guid id, CancellationToken ct)
        => db.DocumentAttachments.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => (Guid?)x.DocumentId)
            .FirstOrDefaultAsync(ct);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.DocumentAttachments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;
        await storage.DeleteAsync(NormalizeLegacyKey(entity.RelativePath), ct);
        db.DocumentAttachments.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string NormalizeLegacyKey(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        return normalized.StartsWith("storage/", StringComparison.OrdinalIgnoreCase)
            ? normalized["storage/".Length..]
            : normalized;
    }

    private static AttachmentDto Map(DocumentAttachment entity)
        => new(entity.Id, entity.DocumentId, entity.FileName, entity.ContentType, entity.Size, $"/api/attachments/{entity.Id}/download", entity.CreatedAt);
}
