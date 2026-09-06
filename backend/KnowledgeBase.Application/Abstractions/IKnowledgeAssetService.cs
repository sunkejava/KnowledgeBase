using KnowledgeBase.Contracts.Knowledge;
namespace KnowledgeBase.Application.Abstractions;
public interface IKnowledgeAssetService
{
    Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken ct); Task<TagDto> SaveTagAsync(Guid? id,SaveTagRequest request,CancellationToken ct); Task<bool> DeleteTagAsync(Guid id,CancellationToken ct);
    Task SetDocumentTagsAsync(Guid documentId,IReadOnlyList<Guid> tagIds,CancellationToken ct); Task<IReadOnlyList<TagDto>> GetDocumentTagsAsync(Guid documentId,CancellationToken ct);
    Task<bool> ToggleFavoriteAsync(Guid userId,Guid documentId,CancellationToken ct); Task<IReadOnlyList<FavoriteDocumentDto>> GetFavoritesAsync(Guid userId,CancellationToken ct);
    Task TrackRecentAsync(Guid userId,Guid documentId,CancellationToken ct); Task<IReadOnlyList<RecentDocumentDto>> GetRecentAsync(Guid userId,int take,CancellationToken ct);
    Task<VersionDto?> CreateVersionAsync(Guid documentId,Guid? editorId,string changeNote,CancellationToken ct); Task<IReadOnlyList<VersionDto>> GetVersionsAsync(Guid documentId,CancellationToken ct); Task<VersionDetailDto?> GetVersionAsync(Guid versionId,CancellationToken ct); Task<bool> RestoreVersionAsync(Guid versionId,Guid? editorId,CancellationToken ct);
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(string keyword,Guid? knowledgeBaseId,int take,CancellationToken ct);
}
