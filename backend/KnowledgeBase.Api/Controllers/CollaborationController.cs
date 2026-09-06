using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Collaboration;
using KnowledgeBase.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 文档评论与站内通知接口。
/// </summary>
[ApiController]
[Authorize]
[Route("api/collaboration")]
public sealed class CollaborationController(
    ICollaborationService service,
    IAccessControlService access) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>分页获取文档评论。</summary>
    [HttpGet("documents/{documentId:guid}/comments")]
    public async Task<ActionResult<PageResult<DocumentCommentDto>>> Comments(Guid documentId, [FromQuery] PageQuery query, CancellationToken ct)
    {
        if (!await CanViewDocumentAsync(documentId, ct)) return Forbid();
        return Ok(await service.GetCommentsAsync(documentId, query, ct));
    }

    /// <summary>创建文档评论。</summary>
    [HttpPost("documents/{documentId:guid}/comments")]
    public async Task<ActionResult<DocumentCommentDto>> CreateComment(Guid documentId, CreateCommentRequest request, CancellationToken ct)
    {
        if (!await CanViewDocumentAsync(documentId, ct)) return Forbid();
        return Ok(await service.CreateCommentAsync(documentId, UserId, request, ct));
    }

    /// <summary>修改本人评论；超级管理员可修改任意评论。</summary>
    [HttpPut("comments/{commentId:guid}")]
    public async Task<ActionResult<DocumentCommentDto>> UpdateComment(Guid commentId, UpdateCommentRequest request, CancellationToken ct)
    {
        var item = await service.UpdateCommentAsync(commentId, UserId, User.IsInRole("SUPER_ADMIN"), request, ct);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>删除本人评论；超级管理员可删除任意评论。</summary>
    [HttpDelete("comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken ct)
        => await service.DeleteCommentAsync(commentId, UserId, User.IsInRole("SUPER_ADMIN"), ct)
            ? NoContent()
            : NotFound();

    /// <summary>分页获取当前用户通知。</summary>
    [HttpGet("notifications")]
    public Task<PageResult<NotificationDto>> Notifications([FromQuery] PageQuery query, CancellationToken ct)
        => service.GetNotificationsAsync(UserId, query, ct);

    /// <summary>获取未读通知数量。</summary>
    [HttpGet("notifications/summary")]
    public Task<NotificationSummaryDto> NotificationSummary(CancellationToken ct)
        => service.GetNotificationSummaryAsync(UserId, ct);

    /// <summary>标记单条通知为已读。</summary>
    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct)
        => await service.MarkNotificationReadAsync(id, UserId, ct) ? NoContent() : NotFound();

    /// <summary>标记全部通知为已读。</summary>
    [HttpPost("notifications/read-all")]
    public async Task<ActionResult<object>> ReadAll(CancellationToken ct)
        => Ok(new { updatedCount = await service.MarkAllNotificationsReadAsync(UserId, ct) });

    private Task<bool> CanViewDocumentAsync(Guid documentId, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : access.CanViewDocumentAsync(documentId, UserId, ct);
}
