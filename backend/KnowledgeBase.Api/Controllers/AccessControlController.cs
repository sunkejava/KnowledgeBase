using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 资源访问权限管理接口。
/// 超级管理员拥有全部权限；普通用户必须具备对应知识库或文档的 Manager 权限。
/// </summary>
[ApiController]
[Authorize]
[Route("api/access")]
public sealed class AccessControlController(IAccessControlService service) : ControllerBase
{
    /// <summary>获取指定知识库的成员权限列表。</summary>
    [HttpGet("knowledge-bases/{knowledgeBaseId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<KnowledgeBaseMemberDto>>> Members(Guid knowledgeBaseId, CancellationToken cancellationToken)
    {
        if (!await CanManageKnowledgeBaseAsync(knowledgeBaseId, cancellationToken)) return Forbid();
        return Ok(await service.GetKnowledgeBaseMembersAsync(knowledgeBaseId, cancellationToken));
    }

    /// <summary>新增或更新指定知识库的成员权限。</summary>
    [HttpPut("knowledge-bases/{knowledgeBaseId:guid}/members")]
    public async Task<ActionResult<KnowledgeBaseMemberDto>> SetMember(
        Guid knowledgeBaseId,
        SetKnowledgeBaseMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanManageKnowledgeBaseAsync(knowledgeBaseId, cancellationToken)) return Forbid();
        return Ok(await service.SetKnowledgeBaseMemberAsync(knowledgeBaseId, request, cancellationToken));
    }

    /// <summary>移除指定知识库的成员权限。</summary>
    [HttpDelete("knowledge-bases/{knowledgeBaseId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid knowledgeBaseId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await CanManageKnowledgeBaseAsync(knowledgeBaseId, cancellationToken)) return Forbid();
        return await service.RemoveKnowledgeBaseMemberAsync(knowledgeBaseId, userId, cancellationToken) ? NoContent() : NotFound();
    }

    /// <summary>获取指定文档的用户权限列表。</summary>
    [HttpGet("documents/{documentId:guid}/permissions")]
    public async Task<ActionResult<IReadOnlyList<DocumentPermissionDto>>> DocumentPermissions(Guid documentId, CancellationToken cancellationToken)
    {
        if (!await CanManageDocumentAsync(documentId, cancellationToken)) return Forbid();
        return Ok(await service.GetDocumentPermissionsAsync(documentId, cancellationToken));
    }

    /// <summary>新增或更新指定文档的用户权限。</summary>
    [HttpPut("documents/{documentId:guid}/permissions")]
    public async Task<ActionResult<DocumentPermissionDto>> SetDocumentPermission(
        Guid documentId,
        SetDocumentPermissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanManageDocumentAsync(documentId, cancellationToken)) return Forbid();
        return Ok(await service.SetDocumentPermissionAsync(documentId, request, cancellationToken));
    }

    /// <summary>移除指定文档的用户权限。</summary>
    [HttpDelete("documents/{documentId:guid}/permissions/{userId:guid}")]
    public async Task<IActionResult> RemoveDocumentPermission(Guid documentId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await CanManageDocumentAsync(documentId, cancellationToken)) return Forbid();
        return await service.RemoveDocumentPermissionAsync(documentId, userId, cancellationToken) ? NoContent() : NotFound();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<bool> CanManageKnowledgeBaseAsync(Guid knowledgeBaseId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : service.CanManageKnowledgeBaseAsync(knowledgeBaseId, CurrentUserId, ct);

    private Task<bool> CanManageDocumentAsync(Guid documentId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : service.CanManageDocumentAsync(documentId, CurrentUserId, ct);
}
