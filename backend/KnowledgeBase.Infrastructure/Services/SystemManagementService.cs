using System.Security.Cryptography;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.System;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// 系统管理服务实现。
/// 负责用户、角色、组织机构、菜单、权限画像和审计日志等后台基础数据维护。
/// </summary>
public sealed class SystemManagementService(KnowledgeDbContext db) : ISystemManagementService
{
    /// <summary>
    /// 分页查询用户。用户主表先分页，再按当前页用户批量加载角色关系，避免全量用户和角色关系一次性进入内存。
    /// </summary>
    public async Task<PageResult<UserDto>> GetUsersAsync(PageQuery query, string? keyword, CancellationToken ct)
    {
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;
        var source = db.Users.AsNoTracking().AsQueryable();
        var normalizedKeyword = (keyword ?? query.Keyword)?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            source = source.Where(x =>
                x.UserName.Contains(normalizedKeyword) ||
                x.DisplayName.Contains(normalizedKeyword));
        }

        var total = await source.LongCountAsync(ct);
        var users = await source
            .OrderBy(x => x.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (users.Count == 0)
            return new PageResult<UserDto>([], total, page, pageSize);

        var userIds = users.Select(x => x.Id).ToArray();
        var links = await db.UserRoles.AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .ToListAsync(ct);
        var roleIds = links.Select(x => x.RoleId).Distinct().ToArray();
        var roles = await db.Roles.AsNoTracking()
            .Where(x => roleIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var items = users.Select(user =>
        {
            var userRoleIds = links.Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToList();
            var roleNames = userRoleIds.Where(roles.ContainsKey).Select(x => roles[x].Name).ToList();
            return new UserDto(user.Id, user.UserName, user.DisplayName, user.Enabled, userRoleIds, roleNames, user.CreatedAt);
        }).ToList();

        return new PageResult<UserDto>(items, total, page, pageSize);
    }

    /// <summary>创建或更新用户，并同步用户角色关系。</summary>
    public async Task<UserDto> SaveUserAsync(Guid? id, SaveUserRequest request, CancellationToken ct)
    {
        SysUser user;
        if (id is null)
        {
            var (hash, salt) = Hash(string.IsNullOrWhiteSpace(request.Password) ? "ChangeMe123!" : request.Password);
            user = new SysUser(request.UserName, request.DisplayName, hash, salt);
            db.Users.Add(user);
        }
        else
        {
            user = await db.Users.FirstAsync(x => x.Id == id, ct);
            user.Update(request.DisplayName, request.Enabled);
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var (hash, salt) = Hash(request.Password);
                user.ChangePassword(hash, salt);
            }
            db.UserRoles.RemoveRange(db.UserRoles.Where(x => x.UserId == user.Id));
        }

        foreach (var roleId in request.RoleIds.Distinct())
            db.UserRoles.Add(new SysUserRole(user.Id, roleId));

        await db.SaveChangesAsync(ct);
        return await BuildUserDtoAsync(user, ct);
    }

    /// <summary>删除用户。内置 admin 账号不允许删除。</summary>
    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null || user.UserName == "admin") return false;
        db.UserRoles.RemoveRange(db.UserRoles.Where(x => x.UserId == id));
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>获取全部角色及对应菜单授权。</summary>
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct)
    {
        var roles = await db.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        var links = await db.RoleMenus.AsNoTracking().ToListAsync(ct);
        return roles.Select(x => new RoleDto(
            x.Id,
            x.Code,
            x.Name,
            x.Enabled,
            links.Where(m => m.RoleId == x.Id).Select(m => m.MenuId).ToList())).ToList();
    }

    /// <summary>创建或更新角色，并同步菜单授权关系。</summary>
    public async Task<RoleDto> SaveRoleAsync(Guid? id, SaveRoleRequest request, CancellationToken ct)
    {
        SysRole role;
        if (id is null)
        {
            role = new SysRole(request.Code, request.Name);
            db.Roles.Add(role);
        }
        else
        {
            role = await db.Roles.FirstAsync(x => x.Id == id, ct);
            role.Update(request.Code, request.Name, request.Enabled);
            db.RoleMenus.RemoveRange(db.RoleMenus.Where(x => x.RoleId == role.Id));
        }

        foreach (var menuId in request.MenuIds.Distinct())
            db.RoleMenus.Add(new SysRoleMenu(role.Id, menuId));

        await db.SaveChangesAsync(ct);
        var menuIds = await db.RoleMenus.AsNoTracking().Where(x => x.RoleId == role.Id).Select(x => x.MenuId).ToListAsync(ct);
        return new RoleDto(role.Id, role.Code, role.Name, role.Enabled, menuIds);
    }

    /// <summary>删除角色。内置超级管理员角色不允许删除。</summary>
    public async Task<bool> DeleteRoleAsync(Guid id, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (role is null || role.Code == "SUPER_ADMIN") return false;
        db.UserRoles.RemoveRange(db.UserRoles.Where(x => x.RoleId == id));
        db.RoleMenus.RemoveRange(db.RoleMenus.Where(x => x.RoleId == id));
        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>获取部门列表。</summary>
    public async Task<IReadOnlyList<TreeNodeDto>> GetDepartmentsAsync(CancellationToken ct) =>
        await db.Departments.AsNoTracking().OrderBy(x => x.Sort)
            .Select(x => new TreeNodeDto(x.Id, x.ParentId, x.Name, null, x.Sort, x.Enabled))
            .ToListAsync(ct);

    /// <summary>创建或更新部门。</summary>
    public async Task<TreeNodeDto> SaveDepartmentAsync(Guid? id, SaveDepartmentRequest request, CancellationToken ct)
    {
        SysDepartment item;
        if (id is null)
        {
            item = new SysDepartment(request.Name, request.ParentId, request.Sort);
            db.Departments.Add(item);
        }
        else
        {
            item = await db.Departments.FirstAsync(x => x.Id == id, ct);
            item.Update(request.Name, request.ParentId, request.Sort, request.Enabled);
        }
        await db.SaveChangesAsync(ct);
        return new TreeNodeDto(item.Id, item.ParentId, item.Name, null, item.Sort, item.Enabled);
    }

    /// <summary>删除部门。</summary>
    public Task<bool> DeleteDepartmentAsync(Guid id, CancellationToken ct) => DeleteNode(db.Departments, id, ct);

    /// <summary>获取组织机构列表。</summary>
    public async Task<IReadOnlyList<TreeNodeDto>> GetOrganizationsAsync(CancellationToken ct) =>
        await db.Organizations.AsNoTracking().OrderBy(x => x.Sort)
            .Select(x => new TreeNodeDto(x.Id, x.ParentId, x.Name, x.Code, x.Sort, x.Enabled))
            .ToListAsync(ct);

    /// <summary>创建或更新组织机构。</summary>
    public async Task<TreeNodeDto> SaveOrganizationAsync(Guid? id, SaveOrganizationRequest request, CancellationToken ct)
    {
        SysOrganization item;
        if (id is null)
        {
            item = new SysOrganization(request.Name, request.Code, request.ParentId);
            db.Organizations.Add(item);
        }
        else
        {
            item = await db.Organizations.FirstAsync(x => x.Id == id, ct);
        }
        item.Update(request.Name, request.Code, request.ParentId, request.Sort, request.Enabled);
        await db.SaveChangesAsync(ct);
        return new TreeNodeDto(item.Id, item.ParentId, item.Name, item.Code, item.Sort, item.Enabled);
    }

    /// <summary>删除组织机构。</summary>
    public Task<bool> DeleteOrganizationAsync(Guid id, CancellationToken ct) => DeleteNode(db.Organizations, id, ct);

    /// <summary>获取菜单和按钮权限节点。</summary>
    public async Task<IReadOnlyList<MenuDto>> GetMenusAsync(CancellationToken ct) =>
        await db.Menus.AsNoTracking().OrderBy(x => x.Sort)
            .Select(x => new MenuDto(x.Id, x.ParentId, x.Name, x.Path, x.Permission, x.Type, x.Icon, x.Sort, x.Enabled))
            .ToListAsync(ct);

    /// <summary>创建或更新菜单节点。</summary>
    public async Task<MenuDto> SaveMenuAsync(Guid? id, SaveMenuRequest request, CancellationToken ct)
    {
        SysMenu item;
        if (id is null)
        {
            item = new SysMenu(request.Name, request.Path, request.Permission, request.ParentId, request.Sort);
            db.Menus.Add(item);
        }
        else
        {
            item = await db.Menus.FirstAsync(x => x.Id == id, ct);
        }
        item.Update(request.Name, request.Path, request.Permission, request.Type, request.Icon, request.ParentId, request.Sort, request.Enabled);
        await db.SaveChangesAsync(ct);
        return new MenuDto(item.Id, item.ParentId, item.Name, item.Path, item.Permission, item.Type, item.Icon, item.Sort, item.Enabled);
    }

    /// <summary>删除菜单，同时清理角色菜单关联。</summary>
    public async Task<bool> DeleteMenuAsync(Guid id, CancellationToken ct)
    {
        var item = await db.Menus.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return false;
        db.RoleMenus.RemoveRange(db.RoleMenus.Where(x => x.MenuId == id));
        db.Menus.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>获取当前用户拥有的角色、权限标识和菜单列表。</summary>
    public async Task<PermissionProfileDto> GetPermissionProfileAsync(Guid userId, CancellationToken ct)
    {
        var roleIds = await db.UserRoles.Where(x => x.UserId == userId).Select(x => x.RoleId).ToListAsync(ct);
        var roles = await db.Roles.Where(x => roleIds.Contains(x.Id) && x.Enabled).Select(x => x.Code).ToListAsync(ct);
        if (roles.Contains("SUPER_ADMIN"))
        {
            var all = await GetMenusAsync(ct);
            return new PermissionProfileDto(
                roles,
                all.Where(x => !string.IsNullOrWhiteSpace(x.Permission)).Select(x => x.Permission).Distinct().ToList(),
                all);
        }

        var menuIds = await db.RoleMenus.Where(x => roleIds.Contains(x.RoleId)).Select(x => x.MenuId).Distinct().ToListAsync(ct);
        var menus = (await GetMenusAsync(ct)).Where(x => menuIds.Contains(x.Id) && x.Enabled).ToList();
        return new PermissionProfileDto(
            roles,
            menus.Where(x => !string.IsNullOrWhiteSpace(x.Permission)).Select(x => x.Permission).Distinct().ToList(),
            menus);
    }

    /// <summary>校验原密码后修改当前用户密码。</summary>
    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null || !Verify(request.OldPassword, user.PasswordHash, user.PasswordSalt)) return false;
        var (hash, salt) = Hash(request.NewPassword);
        user.ChangePassword(hash, salt);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>分页查询审计日志，支持按类别、动作、用户、目标、IP 和说明关键字模糊筛选。</summary>
    public async Task<PageResult<AuditLogDto>> GetAuditLogsAsync(PageQuery query, string? keyword, CancellationToken ct)
    {
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;
        var source = db.AuditLogs.AsNoTracking().AsQueryable();
        var normalizedKeyword = (keyword ?? query.Keyword)?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            source = source.Where(x =>
                x.Category.Contains(normalizedKeyword) ||
                x.Action.Contains(normalizedKeyword) ||
                x.UserName.Contains(normalizedKeyword) ||
                x.Target.Contains(normalizedKeyword) ||
                x.IpAddress.Contains(normalizedKeyword) ||
                (x.Message != null && x.Message.Contains(normalizedKeyword)));
        }

        var total = await source.LongCountAsync(ct);
        var items = await source.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto(x.Id, x.Category, x.Action, x.UserName, x.Target, x.IpAddress, x.Success, x.Message, x.CreatedAt))
            .ToListAsync(ct);

        return new PageResult<AuditLogDto>(items, total, page, pageSize);
    }

    /// <summary>构建单个用户 DTO，避免保存后重新读取全部用户列表。</summary>
    private async Task<UserDto> BuildUserDtoAsync(SysUser user, CancellationToken ct)
    {
        var roleIds = await db.UserRoles.AsNoTracking().Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToListAsync(ct);
        var roleNames = await db.Roles.AsNoTracking().Where(x => roleIds.Contains(x.Id)).Select(x => x.Name).ToListAsync(ct);
        return new UserDto(user.Id, user.UserName, user.DisplayName, user.Enabled, roleIds, roleNames, user.CreatedAt);
    }

    /// <summary>删除简单树节点。</summary>
    private async Task<bool> DeleteNode<T>(DbSet<T> set, Guid id, CancellationToken ct) where T : class
    {
        var item = await set.FindAsync([id], ct);
        if (item is null) return false;
        set.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>使用 PBKDF2-SHA256 生成密码哈希和随机盐。</summary>
    private static (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    /// <summary>使用固定时间比较验证密码，降低时序攻击风险。</summary>
    private static bool Verify(string password, string hash, string salt)
    {
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), 120000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(hash));
    }
}
