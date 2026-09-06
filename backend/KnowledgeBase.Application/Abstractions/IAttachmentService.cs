using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 文档附件服务契约。附件文件通过 IFileStorage 访问，不向上层暴露物理路径。
/// </summary>
public interface IAttachmentService
{
    Task<AttachmentDto> SaveAsync(Guid documentId, string fileName, string contentType, long size, Stream stream, Guid? uploaderId, CancellationToken ct);
    Task<IReadOnlyList<AttachmentDto>> GetListAsync(Guid documentId, CancellationToken ct);
    Task<(Guid DocumentId, Stream Stream, string FileName, string ContentType)?> OpenReadAsync(Guid id, CancellationToken ct);
    Task<Guid?> GetDocumentIdAsync(Guid id, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
