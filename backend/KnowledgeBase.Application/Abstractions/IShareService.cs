using KnowledgeBase.Contracts.Knowledge;
namespace KnowledgeBase.Application.Abstractions;
public interface IShareService
{
    Task<ShareLinkDto?> CreateAsync(Guid documentId,Guid? userId,DateTimeOffset? expiresAt,CancellationToken ct);
    Task<IReadOnlyList<ShareLinkDto>> GetDocumentLinksAsync(Guid documentId,CancellationToken ct);
    Task<bool> DisableAsync(Guid id,CancellationToken ct);
    Task<SharedDocumentDto?> GetSharedDocumentAsync(string token,CancellationToken ct);
}
