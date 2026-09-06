using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>文档公开分享服务。</summary>
public interface IShareService
{
    /// <summary>创建分享链接，可选设置访问密码和过期时间。</summary>
    Task<ShareLinkDto?> CreateAsync(Guid documentId,Guid? userId,DateTimeOffset? expiresAt,string? password,CancellationToken ct);
    /// <summary>获取文档的分享链接。</summary>
    Task<IReadOnlyList<ShareLinkDto>> GetDocumentLinksAsync(Guid documentId,CancellationToken ct);
    /// <summary>停用分享链接。</summary>
    Task<bool> DisableAsync(Guid id,CancellationToken ct);
    /// <summary>获取分享链接所属文档。</summary>
    Task<Guid?> GetDocumentIdByLinkAsync(Guid id,CancellationToken ct);
    /// <summary>验证分享密码并读取公开文档，同时记录访问日志。</summary>
    Task<SharedDocumentDto?> GetSharedDocumentAsync(string token,string? password,string? ipAddress,string? userAgent,CancellationToken ct);
    /// <summary>获取指定文档的分享访问日志。</summary>
    Task<IReadOnlyList<ShareAccessLogDto>> GetAccessLogsAsync(Guid documentId,int take,CancellationToken ct);
}
