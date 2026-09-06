using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 知识库与文档资源权限服务。
/// </summary>
public interface IAccessControlService
{
    /// <summary>获取权限面板可选用户。</summary>
    Task<IReadOnlyList<PermissionUserLookupDto>> SearchUsersAsync(string? keyword, int take, CancellationToken ct);
    /// <summary>获取知识库成员列表。</summary>
    Task<IReadOnlyList<KnowledgeBaseMemberDto>> GetKnowledgeBaseMembersAsync(Guid knowledgeBaseId, CancellationToken ct);
    /// <summary>新增或更新知识库成员。</summary>
    Task<KnowledgeBaseMemberDto> SetKnowledgeBaseMemberAsync(Guid knowledgeBaseId, SetKnowledgeBaseMemberRequest request, CancellationToken ct);
    /// <summary>移除知识库成员。</summary>
    Task<bool> RemoveKnowledgeBaseMemberAsync(Guid knowledgeBaseId, Guid userId, CancellationToken ct);
    /// <summary>获取文档用户级权限列表。</summary>
    Task<IReadOnlyList<DocumentPermissionDto>> GetDocumentPermissionsAsync(Guid documentId, CancellationToken ct);
    /// <summary>新增或更新文档用户级权限。</summary>
    Task<DocumentPermissionDto> SetDocumentPermissionAsync(Guid documentId, SetDocumentPermissionRequest request, CancellationToken ct);
    /// <summary>移除文档用户级权限。</summary>
    Task<bool> RemoveDocumentPermissionAsync(Guid documentId, Guid userId, CancellationToken ct);

    /// <summary>判断用户是否可以查看知识库。</summary>
    Task<bool> CanViewKnowledgeBaseAsync(Guid knowledgeBaseId, Guid userId, CancellationToken ct);
    /// <summary>判断用户是否可以编辑知识库内容。</summary>
    Task<bool> CanEditKnowledgeBaseAsync(Guid knowledgeBaseId, Guid userId, CancellationToken ct);
    /// <summary>判断用户是否可以管理知识库及其成员权限。</summary>
    Task<bool> CanManageKnowledgeBaseAsync(Guid knowledgeBaseId, Guid userId, CancellationToken ct);
    /// <summary>判断用户是否可以查看文档。文档显式权限优先于知识库成员权限。</summary>
    Task<bool> CanViewDocumentAsync(Guid documentId, Guid userId, CancellationToken ct);
    /// <summary>判断用户是否可以编辑文档。文档显式权限优先于知识库成员权限。</summary>
    Task<bool> CanEditDocumentAsync(Guid documentId, Guid userId, CancellationToken ct);
    /// <summary>判断用户是否可以管理文档权限、附件、分享等高级操作。</summary>
    Task<bool> CanManageDocumentAsync(Guid documentId, Guid userId, CancellationToken ct);
}
