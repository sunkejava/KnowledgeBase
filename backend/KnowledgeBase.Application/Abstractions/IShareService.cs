using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 文档分享服务契约。
/// </summary>
public interface IShareService
{
    Task<ShareLinkDto?> CreateAsync(Guid documentId, Guid? userId, DateTimeOffset? expiresAt, CancellationToken ct);
    Task<IReadOnlyList<ShareLinkDto>> GetDocumentLinksAsync(Guid documentId, CancellationToken ct);
    Task<Guid?> GetDocumentIdByLinkAsync(Guid id, CancellationToken ct);
    Task<bool> DisableAsync(Guid id, CancellationToken ct);
    Task<SharedDocumentDto?> GetSharedDocumentAsync(string token, CancellationToken ct);
}
