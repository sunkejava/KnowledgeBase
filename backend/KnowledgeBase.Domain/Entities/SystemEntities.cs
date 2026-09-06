namespace KnowledgeBase.Domain.Entities;

/// <summary>系统用户。</summary>
public sealed class SysUser
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public bool Enabled { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private SysUser() { }
    public SysUser(string userName, string displayName, string passwordHash, string passwordSalt)
    { UserName = userName.Trim(); DisplayName = displayName.Trim(); PasswordHash = passwordHash; PasswordSalt = passwordSalt; }
}

/// <summary>系统角色。</summary>
public sealed class SysRole
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool Enabled { get; private set; } = true;
    private SysRole() { }
    public SysRole(string code, string name) { Code = code.Trim(); Name = name.Trim(); }
}

public sealed class SysUserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    private SysUserRole() { }
    public SysUserRole(Guid userId, Guid roleId) { UserId = userId; RoleId = roleId; }
}

public sealed class SysDepartment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Sort { get; private set; }
    private SysDepartment() { }
    public SysDepartment(string name, Guid? parentId = null, int sort = 0) { Name = name.Trim(); ParentId = parentId; Sort = sort; }
}

public sealed class SysOrganization
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    private SysOrganization() { }
    public SysOrganization(string name, string code, Guid? parentId = null) { Name = name.Trim(); Code = code.Trim(); ParentId = parentId; }
}

public sealed class SysMenu
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Path { get; private set; } = string.Empty;
    public string Permission { get; private set; } = string.Empty;
    public int Sort { get; private set; }
    private SysMenu() { }
    public SysMenu(string name, string path, string permission, Guid? parentId = null, int sort = 0)
    { Name = name.Trim(); Path = path.Trim(); Permission = permission.Trim(); ParentId = parentId; Sort = sort; }
}

public sealed class SysRoleMenu
{
    public Guid RoleId { get; private set; }
    public Guid MenuId { get; private set; }
    private SysRoleMenu() { }
    public SysRoleMenu(Guid roleId, Guid menuId) { RoleId = roleId; MenuId = menuId; }
}
