using KnowledgeBase.Contracts.Knowledge;
namespace KnowledgeBase.Application.Abstractions;
public interface IAttachmentService
{
    Task<AttachmentDto> SaveAsync(Guid documentId,string fileName,string contentType,long size,Stream stream,Guid? uploaderId,CancellationToken ct);
    Task<IReadOnlyList<AttachmentDto>> GetListAsync(Guid documentId,CancellationToken ct);
    Task<(string Path,string FileName,string ContentType)?> GetAsync(Guid id,CancellationToken ct);
    Task<bool> DeleteAsync(Guid id,CancellationToken ct);
}
