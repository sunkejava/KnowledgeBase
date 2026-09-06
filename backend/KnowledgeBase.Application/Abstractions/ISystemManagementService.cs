using KnowledgeBase.Contracts.System;

namespace KnowledgeBase.Application.Abstractions;

public interface ISystemManagementService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct);
    Task<UserDto> SaveUserAsync(Guid? id,SaveUserRequest request,CancellationToken ct);
    Task<bool> DeleteUserAsync(Guid id,CancellationToken ct);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct);
    Task<RoleDto> SaveRoleAsync(Guid? id,SaveRoleRequest request,CancellationToken ct);
    Task<bool> DeleteRoleAsync(Guid id,CancellationToken ct);
    Task<IReadOnlyList<TreeNodeDto>> GetDepartmentsAsync(CancellationToken ct);
    Task<TreeNodeDto> SaveDepartmentAsync(Guid? id,SaveDepartmentRequest request,CancellationToken ct);
    Task<bool> DeleteDepartmentAsync(Guid id,CancellationToken ct);
    Task<IReadOnlyList<TreeNodeDto>> GetOrganizationsAsync(CancellationToken ct);
    Task<TreeNodeDto> SaveOrganizationAsync(Guid? id,SaveOrganizationRequest request,CancellationToken ct);
    Task<bool> DeleteOrganizationAsync(Guid id,CancellationToken ct);
    Task<IReadOnlyList<MenuDto>> GetMenusAsync(CancellationToken ct);
    Task<MenuDto> SaveMenuAsync(Guid? id,SaveMenuRequest request,CancellationToken ct);
    Task<bool> DeleteMenuAsync(Guid id,CancellationToken ct);
    Task<PermissionProfileDto> GetPermissionProfileAsync(Guid userId,CancellationToken ct);
    Task<bool> ChangePasswordAsync(Guid userId,ChangePasswordRequest request,CancellationToken ct);
    Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(int take,CancellationToken ct);
}
