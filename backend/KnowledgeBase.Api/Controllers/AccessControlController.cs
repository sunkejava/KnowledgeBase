using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 资源访问权限管理接口，用于维护知识库成员权限和单文档用户权限。
/// 当前接口仅允许超级管理员调用，后续可继续下沉到知识库管理员角色。
/// </summary>
[ApiController]
[Authorize(Roles = "SUPER_ADMIN")]
[Route("api/access")]
public sealed class AccessControlController(IAccessControlService service) : ControllerBase
{
    /// <summary>
    /// 获取指定知识库的成员权限列表。
    /// </summary>
    [HttpGet("knowledge-bases/{knowledgeBaseId:guid}/members")]
    public Task<IReadOnlyList<KnowledgeBaseMemberDto>> Members(Guid knowledgeBaseId, CancellationToken cancellationToken)
        => service.GetKnowledgeBaseMembersAsync(knowledgeBaseId, cancellationToken);

    /// <summary>
    /// 新增或更新指定知识库的成员权限。
    /// </summary>
    [HttpPut("knowledge-bases/{knowledgeBaseId:guid}/members")]
    public Task<KnowledgeBaseMemberDto> SetMember(
        Guid knowledgeBaseId,
        SetKnowledgeBaseMemberRequest request,
        CancellationToken cancellationToken)
        => service.SetKnowledgeBaseMemberAsync(knowledgeBaseId, request, cancellationToken);

    /// <summary>
    /// 移除指定知识库的成员权限。
    /// </summary>
    [HttpDelete("knowledge-bases/{knowledgeBaseId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid knowledgeBaseId, Guid userId, CancellationToken cancellationToken)
        => await service.RemoveKnowledgeBaseMemberAsync(knowledgeBaseId, userId, cancellationToken)
            ? NoContent()
            : NotFound();

    /// <summary>
    /// 获取指定文档的用户权限列表。
    /// </summary>
    [HttpGet("documents/{documentId:guid}/permissions")]
    public Task<IReadOnlyList<DocumentPermissionDto>> DocumentPermissions(Guid documentId, CancellationToken cancellationToken)
        => service.GetDocumentPermissionsAsync(documentId, cancellationToken);

    /// <summary>
    /// 新增或更新指定文档的用户权限。
    /// </summary>
    [HttpPut("documents/{documentId:guid}/permissions")]
    public Task<DocumentPermissionDto> SetDocumentPermission(
        Guid documentId,
        SetDocumentPermissionRequest request,
        CancellationToken cancellationToken)
        => service.SetDocumentPermissionAsync(documentId, request, cancellationToken);

    /// <summary>
    /// 移除指定文档的用户权限。
    /// </summary>
    [HttpDelete("documents/{documentId:guid}/permissions/{userId:guid}")]
    public async Task<IActionResult> RemoveDocumentPermission(Guid documentId, Guid userId, CancellationToken cancellationToken)
        => await service.RemoveDocumentPermissionAsync(documentId, userId, cancellationToken)
            ? NoContent()
            : NotFound();
}
