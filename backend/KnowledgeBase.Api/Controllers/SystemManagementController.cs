using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

/// <summary>
/// 系统管理接口，统一提供用户、角色、组织机构、菜单、权限画像和审计日志等后台管理能力。
/// </summary>
[ApiController]
[Authorize]
[Route("api/system")]
public sealed class SystemManagementController(ISystemManagementService service) : ControllerBase
{
    /// <summary>获取当前登录用户的角色、权限和菜单画像。</summary>
    [HttpGet("profile")]
    public Task<PermissionProfileDto> Profile(CancellationToken ct) =>
        service.GetPermissionProfileAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);

    /// <summary>分页查询用户。</summary>
    [HttpGet("users")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<PageResult<UserDto>> Users([FromQuery] PageQuery query, CancellationToken ct) =>
        service.GetUsersAsync(query, query.Keyword, ct);

    /// <summary>创建用户。</summary>
    [HttpPost("users")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<UserDto> CreateUser(SaveUserRequest request, CancellationToken ct) =>
        service.SaveUserAsync(null, request, ct);

    /// <summary>更新用户。</summary>
    [HttpPut("users/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<UserDto> UpdateUser(Guid id, SaveUserRequest request, CancellationToken ct) =>
        service.SaveUserAsync(id, request, ct);

    /// <summary>删除用户。</summary>
    [HttpDelete("users/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct) =>
        await service.DeleteUserAsync(id, ct) ? NoContent() : BadRequest();

    /// <summary>获取全部角色。</summary>
    [HttpGet("roles")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<IReadOnlyList<RoleDto>> Roles(CancellationToken ct) => service.GetRolesAsync(ct);

    /// <summary>创建角色。</summary>
    [HttpPost("roles")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<RoleDto> CreateRole(SaveRoleRequest request, CancellationToken ct) =>
        service.SaveRoleAsync(null, request, ct);

    /// <summary>更新角色。</summary>
    [HttpPut("roles/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<RoleDto> UpdateRole(Guid id, SaveRoleRequest request, CancellationToken ct) =>
        service.SaveRoleAsync(id, request, ct);

    /// <summary>删除角色。</summary>
    [HttpDelete("roles/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken ct) =>
        await service.DeleteRoleAsync(id, ct) ? NoContent() : BadRequest();

    /// <summary>获取部门树数据。</summary>
    [HttpGet("departments")]
    public Task<IReadOnlyList<TreeNodeDto>> Departments(CancellationToken ct) => service.GetDepartmentsAsync(ct);

    /// <summary>创建部门。</summary>
    [HttpPost("departments")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<TreeNodeDto> CreateDepartment(SaveDepartmentRequest request, CancellationToken ct) =>
        service.SaveDepartmentAsync(null, request, ct);

    /// <summary>更新部门。</summary>
    [HttpPut("departments/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<TreeNodeDto> UpdateDepartment(Guid id, SaveDepartmentRequest request, CancellationToken ct) =>
        service.SaveDepartmentAsync(id, request, ct);

    /// <summary>删除部门。</summary>
    [HttpDelete("departments/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken ct) =>
        await service.DeleteDepartmentAsync(id, ct) ? NoContent() : NotFound();

    /// <summary>获取组织机构树数据。</summary>
    [HttpGet("organizations")]
    public Task<IReadOnlyList<TreeNodeDto>> Organizations(CancellationToken ct) => service.GetOrganizationsAsync(ct);

    /// <summary>创建组织机构。</summary>
    [HttpPost("organizations")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<TreeNodeDto> CreateOrganization(SaveOrganizationRequest request, CancellationToken ct) =>
        service.SaveOrganizationAsync(null, request, ct);

    /// <summary>更新组织机构。</summary>
    [HttpPut("organizations/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<TreeNodeDto> UpdateOrganization(Guid id, SaveOrganizationRequest request, CancellationToken ct) =>
        service.SaveOrganizationAsync(id, request, ct);

    /// <summary>删除组织机构。</summary>
    [HttpDelete("organizations/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public async Task<IActionResult> DeleteOrganization(Guid id, CancellationToken ct) =>
        await service.DeleteOrganizationAsync(id, ct) ? NoContent() : NotFound();

    /// <summary>获取菜单和按钮权限节点。</summary>
    [HttpGet("menus")]
    public Task<IReadOnlyList<MenuDto>> Menus(CancellationToken ct) => service.GetMenusAsync(ct);

    /// <summary>创建菜单节点。</summary>
    [HttpPost("menus")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<MenuDto> CreateMenu(SaveMenuRequest request, CancellationToken ct) =>
        service.SaveMenuAsync(null, request, ct);

    /// <summary>更新菜单节点。</summary>
    [HttpPut("menus/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<MenuDto> UpdateMenu(Guid id, SaveMenuRequest request, CancellationToken ct) =>
        service.SaveMenuAsync(id, request, ct);

    /// <summary>删除菜单节点。</summary>
    [HttpDelete("menus/{id:guid}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public async Task<IActionResult> DeleteMenu(Guid id, CancellationToken ct) =>
        await service.DeleteMenuAsync(id, ct) ? NoContent() : NotFound();

    /// <summary>修改当前登录用户密码。</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct) =>
        await service.ChangePasswordAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), request, ct)
            ? NoContent()
            : BadRequest(new { message = "原密码不正确" });

    /// <summary>分页查询审计日志。</summary>
    [HttpGet("audit-logs")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public Task<PageResult<AuditLogDto>> AuditLogs([FromQuery] PageQuery query, CancellationToken ct) =>
        service.GetAuditLogsAsync(query, query.Keyword, ct);
}
