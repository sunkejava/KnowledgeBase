using System.Security.Claims;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController,Authorize,Route("api/system")]
public sealed class SystemManagementController(ISystemManagementService service):ControllerBase
{
    [HttpGet("profile")] public Task<PermissionProfileDto> Profile(CancellationToken ct)=>service.GetPermissionProfileAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),ct);
    [HttpGet("users"),Authorize(Roles="SUPER_ADMIN")] public Task<IReadOnlyList<UserDto>> Users(CancellationToken ct)=>service.GetUsersAsync(ct);
    [HttpPost("users"),Authorize(Roles="SUPER_ADMIN")] public Task<UserDto> CreateUser(SaveUserRequest r,CancellationToken ct)=>service.SaveUserAsync(null,r,ct);
    [HttpPut("users/{id:guid}"),Authorize(Roles="SUPER_ADMIN")] public Task<UserDto> UpdateUser(Guid id,SaveUserRequest r,CancellationToken ct)=>service.SaveUserAsync(id,r,ct);
    [HttpDelete("users/{id:guid}"),Authorize(Roles="SUPER_ADMIN")] public async Task<IActionResult> DeleteUser(Guid id,CancellationToken ct)=>await service.DeleteUserAsync(id,ct)?NoContent():BadRequest();
    [HttpGet("roles"),Authorize(Roles="SUPER_ADMIN")] public Task<IReadOnlyList<RoleDto>> Roles(CancellationToken ct)=>service.GetRolesAsync(ct);
    [HttpPost("roles"),Authorize(Roles="SUPER_ADMIN")] public Task<RoleDto> CreateRole(SaveRoleRequest r,CancellationToken ct)=>service.SaveRoleAsync(null,r,ct);
    [HttpPut("roles/{id:guid}"),Authorize(Roles="SUPER_ADMIN")] public Task<RoleDto> UpdateRole(Guid id,SaveRoleRequest r,CancellationToken ct)=>service.SaveRoleAsync(id,r,ct);
    [HttpDelete("roles/{id:guid}"),Authorize(Roles="SUPER_ADMIN")] public async Task<IActionResult> DeleteRole(Guid id,CancellationToken ct)=>await service.DeleteRoleAsync(id,ct)?NoContent():BadRequest();
    [HttpGet("departments")] public Task<IReadOnlyList<TreeNodeDto>> Departments(CancellationToken ct)=>service.GetDepartmentsAsync(ct);
    [HttpPost("departments"),Authorize(Roles="SUPER_ADMIN")] public Task<TreeNodeDto> CreateDepartment(SaveDepartmentRequest r,CancellationToken ct)=>service.SaveDepartmentAsync(null,r,ct);
    [HttpPut("departments/{id:guid}"),Authorize(Roles="SUPER_ADMIN")] public Task<TreeNodeDto> UpdateDepartment(Guid id,SaveDepartmentRequest r,CancellationToken ct)=>service.SaveDepartmentAsync(id,r,ct);
    [HttpDelete("departments/{id:guid}"),Authorize(Roles="SUPER_ADMIN")] public async Task<IActionResult> DeleteDepartment(Guid id,CancellationToken ct)=>await service.DeleteDepartmentAsync(id,ct)?NoContent():NotFound();
    [HttpGet("organizations")] public Task<IReadOnlyList<TreeNodeDto>> Organizations(CancellationToken ct)=>service.GetOrganizationsAsync(ct);
    [HttpPost("organizations"),Authorize(Roles="SUPER_ADMIN")] public Task<TreeNodeDto> CreateOrganization(SaveOrganizationRequest r,CancellationToken ct)=>service.SaveOrganizationAsync(null,r,ct);
    [HttpPut("organizations/{id:guid}"),Authorize(Roles="SUPER_ADMIN")] public Task<TreeNodeDto> UpdateOrganization(Guid id,SaveOrganizationRequest r,CancellationToken ct)=>service.SaveOrganizationAsync(id,r,ct);
    [HttpDelete("organizations/{id:guid}"),Authorize(Roles="SUPER_ADMIN")] public async Task<IActionResult> DeleteOrganization(Guid id,CancellationToken ct)=>await service.DeleteOrganizationAsync(id,ct)?NoContent():NotFound();
    [HttpGet("menus")] public Task<IReadOnlyList<MenuDto>> Menus(CancellationToken ct)=>service.GetMenusAsync(ct);
    [HttpPost("menus"),Authorize(Roles="SUPER_ADMIN")] public Task<MenuDto> CreateMenu(SaveMenuRequest r,CancellationToken ct)=>service.SaveMenuAsync(null,r,ct);
    [HttpPut("menus/{id:guid}"),Authorize(Roles="SUPER_ADMIN")] public Task<MenuDto> UpdateMenu(Guid id,SaveMenuRequest r,CancellationToken ct)=>service.SaveMenuAsync(id,r,ct);
    [HttpDelete("menus/{id:guid}"),Authorize(Roles="SUPER_ADMIN")] public async Task<IActionResult> DeleteMenu(Guid id,CancellationToken ct)=>await service.DeleteMenuAsync(id,ct)?NoContent():NotFound();
    [HttpPost("change-password")] public async Task<IActionResult> ChangePassword(ChangePasswordRequest r,CancellationToken ct)=>await service.ChangePasswordAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),r,ct)?NoContent():BadRequest(new{message="原密码不正确"});
    [HttpGet("audit-logs"),Authorize(Roles="SUPER_ADMIN")] public Task<IReadOnlyList<AuditLogDto>> AuditLogs([FromQuery]int take=100,CancellationToken ct=default)=>service.GetAuditLogsAsync(take,ct);
}
