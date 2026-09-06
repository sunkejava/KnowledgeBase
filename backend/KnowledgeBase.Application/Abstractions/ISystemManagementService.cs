using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.System;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 系统管理应用服务契约，统一承载用户、角色、组织、菜单和审计日志等后台管理能力。
/// </summary>
public interface ISystemManagementService
{
    /// <summary>分页查询用户。</summary>
    Task<PageResult<UserDto>> GetUsersAsync(PageQuery query, string? keyword, CancellationToken ct);

    /// <summary>创建或更新用户。</summary>
    Task<UserDto> SaveUserAsync(Guid? id, SaveUserRequest request, CancellationToken ct);

    /// <summary>删除用户。</summary>
    Task<bool> DeleteUserAsync(Guid id, CancellationToken ct);

    /// <summary>获取全部角色，主要用于用户维护和角色授权下拉框。</summary>
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct);

    /// <summary>创建或更新角色。</summary>
    Task<RoleDto> SaveRoleAsync(Guid? id, SaveRoleRequest request, CancellationToken ct);

    /// <summary>删除角色。</summary>
    Task<bool> DeleteRoleAsync(Guid id, CancellationToken ct);

    /// <summary>获取部门列表。</summary>
    Task<IReadOnlyList<TreeNodeDto>> GetDepartmentsAsync(CancellationToken ct);

    /// <summary>创建或更新部门。</summary>
    Task<TreeNodeDto> SaveDepartmentAsync(Guid? id, SaveDepartmentRequest request, CancellationToken ct);

    /// <summary>删除部门。</summary>
    Task<bool> DeleteDepartmentAsync(Guid id, CancellationToken ct);

    /// <summary>获取组织机构列表。</summary>
    Task<IReadOnlyList<TreeNodeDto>> GetOrganizationsAsync(CancellationToken ct);

    /// <summary>创建或更新组织机构。</summary>
    Task<TreeNodeDto> SaveOrganizationAsync(Guid? id, SaveOrganizationRequest request, CancellationToken ct);

    /// <summary>删除组织机构。</summary>
    Task<bool> DeleteOrganizationAsync(Guid id, CancellationToken ct);

    /// <summary>获取菜单与按钮权限节点。</summary>
    Task<IReadOnlyList<MenuDto>> GetMenusAsync(CancellationToken ct);

    /// <summary>创建或更新菜单。</summary>
    Task<MenuDto> SaveMenuAsync(Guid? id, SaveMenuRequest request, CancellationToken ct);

    /// <summary>删除菜单。</summary>
    Task<bool> DeleteMenuAsync(Guid id, CancellationToken ct);

    /// <summary>获取当前用户权限画像。</summary>
    Task<PermissionProfileDto> GetPermissionProfileAsync(Guid userId, CancellationToken ct);

    /// <summary>修改当前用户密码。</summary>
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct);

    /// <summary>分页查询审计日志。</summary>
    Task<PageResult<AuditLogDto>> GetAuditLogsAsync(PageQuery query, string? keyword, CancellationToken ct);
}
