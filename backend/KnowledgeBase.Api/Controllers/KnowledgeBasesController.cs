using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Contracts.KnowledgeBases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 知识库管理接口。
/// </summary>
[ApiController]
[Authorize]
[Route("api/knowledge-bases")]
public sealed class KnowledgeBasesController(
    IKnowledgeBaseService service,
    IAccessControlService accessControl) : ControllerBase
{
    /// <summary>
    /// 分页获取当前用户有权访问的知识库。
    /// </summary>
    [HttpGet]
    public Task<PageResult<KnowledgeBaseDto>> GetList([FromQuery] PageQuery query, CancellationToken cancellationToken)
        => service.GetPageAsync(CurrentUserId, User.IsInRole("SUPER_ADMIN"), query, cancellationToken);

    /// <summary>
    /// 获取知识库详情。
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<KnowledgeBaseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanViewAsync(id, cancellationToken)) return Forbid();
        return await service.GetAsync(id, cancellationToken) is { } item ? Ok(item) : NotFound();
    }

    /// <summary>
    /// 创建知识库，并自动将创建人设置为 Manager。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<KnowledgeBaseDto>> Create(SaveKnowledgeBaseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "知识库名称不能为空" });

        var item = await service.CreateAsync(request, cancellationToken);
        await accessControl.SetKnowledgeBaseMemberAsync(
            item.Id,
            new SetKnowledgeBaseMemberRequest(CurrentUserId, "Manager"),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    /// <summary>
    /// 更新知识库基础信息，仅 Manager 或超级管理员可操作。
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<KnowledgeBaseDto>> Update(Guid id, SaveKnowledgeBaseRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(id, cancellationToken)) return Forbid();
        return await service.UpdateAsync(id, request, cancellationToken) is { } item ? Ok(item) : NotFound();
    }

    /// <summary>
    /// 删除知识库，仅 Manager 或超级管理员可操作。
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(id, cancellationToken)) return Forbid();
        return await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<bool> CanViewAsync(Guid id, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : accessControl.CanViewKnowledgeBaseAsync(id, CurrentUserId, ct);

    private Task<bool> CanManageAsync(Guid id, CancellationToken ct)
        => User.IsInRole("SUPER_ADMIN")
            ? Task.FromResult(true)
            : accessControl.CanManageKnowledgeBaseAsync(id, CurrentUserId, ct);
}
