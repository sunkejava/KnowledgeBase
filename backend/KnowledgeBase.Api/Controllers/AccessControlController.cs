using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Authorize(Roles="SUPER_ADMIN")]
[Route("api/access")]
public sealed class AccessControlController(IAccessControlService service) : ControllerBase
{
    [HttpGet("knowledge-bases/{knowledgeBaseId:guid}/members")]
    public Task<IReadOnlyList<KnowledgeBaseMemberDto>> Members(Guid knowledgeBaseId,CancellationToken ct)
        => service.GetKnowledgeBaseMembersAsync(knowledgeBaseId,ct);

    [HttpPut("knowledge-bases/{knowledgeBaseId:guid}/members")]
    public Task<KnowledgeBaseMemberDto> SetMember(Guid knowledgeBaseId,SetKnowledgeBaseMemberRequest request,CancellationToken ct)
        => service.SetKnowledgeBaseMemberAsync(knowledgeBaseId,request,ct);

    [HttpDelete("knowledge-bases/{knowledgeBaseId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid knowledgeBaseId,Guid userId,CancellationToken ct)
        => await service.RemoveKnowledgeBaseMemberAsync(knowledgeBaseId,userId,ct)?NoContent():NotFound();

    [HttpGet("documents/{documentId:guid}/permissions")]
    public Task<IReadOnlyList<DocumentPermissionDto>> DocumentPermissions(Guid documentId,CancellationToken ct)
        => service.GetDocumentPermissionsAsync(documentId,ct);

    [HttpPut("documents/{documentId:guid}/permissions")]
    public Task<DocumentPermissionDto> SetDocumentPermission(Guid documentId,SetDocumentPermissionRequest request,CancellationToken ct)
        => service.SetDocumentPermissionAsync(documentId,request,ct);

    [HttpDelete("documents/{documentId:guid}/permissions/{userId:guid}")]
    public async Task<IActionResult> RemoveDocumentPermission(Guid documentId,Guid userId,CancellationToken ct)
        => await service.RemoveDocumentPermissionAsync(documentId,userId,ct)?NoContent():NotFound();
}
