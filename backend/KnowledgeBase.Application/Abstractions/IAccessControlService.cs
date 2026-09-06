using KnowledgeBase.Contracts.Knowledge;
namespace KnowledgeBase.Application.Abstractions;
public interface IAccessControlService
{
    Task<IReadOnlyList<KnowledgeBaseMemberDto>> GetKnowledgeBaseMembersAsync(Guid knowledgeBaseId,CancellationToken ct);
    Task<KnowledgeBaseMemberDto> SetKnowledgeBaseMemberAsync(Guid knowledgeBaseId,SetKnowledgeBaseMemberRequest request,CancellationToken ct);
    Task<bool> RemoveKnowledgeBaseMemberAsync(Guid knowledgeBaseId,Guid userId,CancellationToken ct);
    Task<IReadOnlyList<DocumentPermissionDto>> GetDocumentPermissionsAsync(Guid documentId,CancellationToken ct);
    Task<DocumentPermissionDto> SetDocumentPermissionAsync(Guid documentId,SetDocumentPermissionRequest request,CancellationToken ct);
    Task<bool> RemoveDocumentPermissionAsync(Guid documentId,Guid userId,CancellationToken ct);
}
