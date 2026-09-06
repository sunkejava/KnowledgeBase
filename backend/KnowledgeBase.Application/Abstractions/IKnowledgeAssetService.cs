using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 知识资产应用服务契约。
/// </summary>
public interface IKnowledgeAssetService
{
    Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken ct);
    Task<TagDto> SaveTagAsync(Guid? id, SaveTagRequest request, CancellationToken ct);
    Task<bool> DeleteTagAsync(Guid id, CancellationToken ct);
    Task SetDocumentTagsAsync(Guid documentId, IReadOnlyList<Guid> tagIds, CancellationToken ct);
    Task<IReadOnlyList<TagDto>> GetDocumentTagsAsync(Guid documentId, CancellationToken ct);

    Task<bool> ToggleFavoriteAsync(Guid userId, Guid documentId, CancellationToken ct);
    Task<PageResult<FavoriteDocumentDto>> GetFavoritesPageAsync(Guid userId, PageQuery query, CancellationToken ct);
    Task TrackRecentAsync(Guid userId, Guid documentId, CancellationToken ct);
    Task<PageResult<RecentDocumentDto>> GetRecentPageAsync(Guid userId, PageQuery query, CancellationToken ct);

    Task<VersionDto?> CreateVersionAsync(Guid documentId, Guid? editorId, string changeNote, CancellationToken ct);
    Task<IReadOnlyList<VersionDto>> GetVersionsAsync(Guid documentId, CancellationToken ct);
    Task<VersionDetailDto?> GetVersionAsync(Guid versionId, CancellationToken ct);
    Task<bool> RestoreVersionAsync(Guid versionId, Guid? editorId, CancellationToken ct);

    /// <summary>
    /// 按当前用户资源权限执行全文搜索。普通用户只能搜索自己有权访问的知识库。
    /// </summary>
    Task<PageResult<SearchResultDto>> SearchPageAsync(
        string keyword,
        Guid userId,
        bool isSuperAdmin,
        Guid? knowledgeBaseId,
        PageQuery query,
        CancellationToken ct);
}
