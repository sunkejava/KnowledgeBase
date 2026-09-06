using KnowledgeBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Persistence;

public sealed class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentContent> DocumentContents => Set<DocumentContent>();
    public DbSet<KnowledgeBaseSpace> KnowledgeBases => Set<KnowledgeBaseSpace>();
    public DbSet<UserAppearanceSetting> UserAppearanceSettings => Set<UserAppearanceSetting>();
    public DbSet<SysUser> Users => Set<SysUser>();
    public DbSet<SysRole> Roles => Set<SysRole>();
    public DbSet<SysUserRole> UserRoles => Set<SysUserRole>();
    public DbSet<SysDepartment> Departments => Set<SysDepartment>();
    public DbSet<SysOrganization> Organizations => Set<SysOrganization>();
    public DbSet<SysMenu> Menus => Set<SysMenu>();
    public DbSet<SysRoleMenu> RoleMenus => Set<SysRoleMenu>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity => { entity.ToTable("Kb_Document"); entity.HasKey(x => x.Id); entity.Property(x => x.Title).HasMaxLength(200).IsRequired(); entity.Property(x => x.Slug).HasMaxLength(220).IsRequired(); entity.HasIndex(x => new { x.KnowledgeBaseId, x.Slug }).IsUnique(); });
        modelBuilder.Entity<DocumentContent>(entity => { entity.ToTable("Kb_DocumentContent"); entity.HasKey(x => x.DocumentId); entity.Property(x => x.Markdown).IsRequired(); entity.HasOne<Document>().WithOne().HasForeignKey<DocumentContent>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<KnowledgeBaseSpace>(entity => { entity.ToTable("Kb_KnowledgeBase"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.Property(x => x.Description).HasMaxLength(500); });
        modelBuilder.Entity<UserAppearanceSetting>(entity => { entity.ToTable("Sys_UserAppearanceSetting"); entity.HasKey(x => x.Id); entity.Property(x => x.UserKey).HasMaxLength(100).IsRequired(); entity.HasIndex(x => x.UserKey).IsUnique(); entity.Property(x => x.Theme).HasMaxLength(20); entity.Property(x => x.Locale).HasMaxLength(20); entity.Property(x => x.WatermarkText).HasMaxLength(40); });
        modelBuilder.Entity<SysUser>(entity => { entity.ToTable("Sys_User"); entity.HasKey(x => x.Id); entity.Property(x => x.UserName).HasMaxLength(50).IsRequired(); entity.Property(x => x.DisplayName).HasMaxLength(80).IsRequired(); entity.HasIndex(x => x.UserName).IsUnique(); });
        modelBuilder.Entity<SysRole>(entity => { entity.ToTable("Sys_Role"); entity.HasKey(x => x.Id); entity.Property(x => x.Code).HasMaxLength(50).IsRequired(); entity.Property(x => x.Name).HasMaxLength(80).IsRequired(); entity.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<SysUserRole>(entity => { entity.ToTable("Sys_UserRole"); entity.HasKey(x => new { x.UserId, x.RoleId }); });
        modelBuilder.Entity<SysDepartment>(entity => { entity.ToTable("Sys_Department"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); });
        modelBuilder.Entity<SysOrganization>(entity => { entity.ToTable("Sys_Organization"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.Property(x => x.Code).HasMaxLength(60).IsRequired(); entity.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<SysMenu>(entity => { entity.ToTable("Sys_Menu"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(80).IsRequired(); entity.Property(x => x.Path).HasMaxLength(200); entity.Property(x => x.Permission).HasMaxLength(120); });
        modelBuilder.Entity<SysRoleMenu>(entity => { entity.ToTable("Sys_RoleMenu"); entity.HasKey(x => new { x.RoleId, x.MenuId }); });
    }
}
