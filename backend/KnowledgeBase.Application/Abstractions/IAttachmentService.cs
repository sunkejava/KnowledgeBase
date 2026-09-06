using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 文档附件服务契约。
/// </summary>
public interface IAttachmentService
{
    Task<AttachmentDto> SaveAsync(Guid documentId, string fileName, string contentType, long size, Stream stream, Guid? uploaderId, CancellationToken ct);
    Task<IReadOnlyList<AttachmentDto>> GetListAsync(Guid documentId, CancellationToken ct);
    Task<(Guid DocumentId, string Path, string FileName, string ContentType)?> GetAsync(Guid id, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
