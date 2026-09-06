namespace KnowledgeBase.Contracts.System;

public sealed record UserDto(Guid Id,string UserName,string DisplayName,bool Enabled,IReadOnlyList<Guid> RoleIds,IReadOnlyList<string> Roles,DateTimeOffset CreatedAt);
public sealed record SaveUserRequest(string UserName,string DisplayName,string? Password,bool Enabled,IReadOnlyList<Guid> RoleIds);
public sealed record ChangePasswordRequest(string OldPassword,string NewPassword);
public sealed record RoleDto(Guid Id,string Code,string Name,bool Enabled,IReadOnlyList<Guid> MenuIds);
public sealed record SaveRoleRequest(string Code,string Name,bool Enabled,IReadOnlyList<Guid> MenuIds);
public sealed record TreeNodeDto(Guid Id,Guid? ParentId,string Name,string? Code,int Sort,bool Enabled);
public sealed record SaveDepartmentRequest(Guid? ParentId,string Name,int Sort,bool Enabled);
public sealed record SaveOrganizationRequest(Guid? ParentId,string Name,string Code,int Sort,bool Enabled);
public sealed record MenuDto(Guid Id,Guid? ParentId,string Name,string Path,string Permission,string Type,string Icon,int Sort,bool Enabled);
public sealed record SaveMenuRequest(Guid? ParentId,string Name,string Path,string Permission,string Type,string Icon,int Sort,bool Enabled);
public sealed record PermissionProfileDto(IReadOnlyList<string> Roles,IReadOnlyList<string> Permissions,IReadOnlyList<MenuDto> Menus);
public sealed record AuditLogDto(Guid Id,string Category,string Action,string UserName,string Target,string IpAddress,bool Success,string? Message,DateTimeOffset CreatedAt);
