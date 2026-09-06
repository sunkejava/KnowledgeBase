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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();
    public DbSet<DocumentTagLink> DocumentTagLinks => Set<DocumentTagLink>();
    public DbSet<DocumentFavorite> DocumentFavorites => Set<DocumentFavorite>();
    public DbSet<DocumentRecentView> DocumentRecentViews => Set<DocumentRecentView>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentAttachment> DocumentAttachments => Set<DocumentAttachment>();
    public DbSet<DocumentShareLink> DocumentShareLinks => Set<DocumentShareLink>();

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
        modelBuilder.Entity<SysMenu>(entity => { entity.ToTable("Sys_Menu"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(80).IsRequired(); entity.Property(x => x.Path).HasMaxLength(200); entity.Property(x => x.Permission).HasMaxLength(120); entity.Property(x => x.Type).HasMaxLength(20); entity.Property(x => x.Icon).HasMaxLength(80); });
        modelBuilder.Entity<SysRoleMenu>(entity => { entity.ToTable("Sys_RoleMenu"); entity.HasKey(x => new { x.RoleId, x.MenuId }); });
        modelBuilder.Entity<AuditLog>(entity => { entity.ToTable("Sys_AuditLog"); entity.HasKey(x => x.Id); entity.Property(x => x.Category).HasMaxLength(40); entity.Property(x => x.Action).HasMaxLength(80); entity.Property(x => x.UserName).HasMaxLength(80); entity.Property(x => x.Target).HasMaxLength(200); entity.Property(x => x.IpAddress).HasMaxLength(64); entity.Property(x => x.Message).HasMaxLength(1000); entity.HasIndex(x => x.CreatedAt); });
        modelBuilder.Entity<DocumentTag>(entity => { entity.ToTable("Kb_Tag"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(80).IsRequired(); entity.Property(x => x.Color).HasMaxLength(20); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<DocumentTagLink>(entity => { entity.ToTable("Kb_DocumentTag"); entity.HasKey(x => new { x.DocumentId, x.TagId }); });
        modelBuilder.Entity<DocumentFavorite>(entity => { entity.ToTable("Kb_Favorite"); entity.HasKey(x => new { x.UserId, x.DocumentId }); entity.HasIndex(x => x.CreatedAt); });
        modelBuilder.Entity<DocumentRecentView>(entity => { entity.ToTable("Kb_RecentView"); entity.HasKey(x => new { x.UserId, x.DocumentId }); entity.HasIndex(x => x.LastViewedAt); });
        modelBuilder.Entity<DocumentVersion>(entity => { entity.ToTable("Kb_DocumentVersion"); entity.HasKey(x => x.Id); entity.Property(x => x.Title).HasMaxLength(200); entity.Property(x => x.Slug).HasMaxLength(220); entity.Property(x => x.ChangeNote).HasMaxLength(500); entity.Property(x => x.Markdown).IsRequired(); entity.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique(); });
        modelBuilder.Entity<DocumentAttachment>(entity => { entity.ToTable("Kb_Attachment"); entity.HasKey(x => x.Id); entity.Property(x => x.FileName).HasMaxLength(255); entity.Property(x => x.StoredName).HasMaxLength(255); entity.Property(x => x.ContentType).HasMaxLength(120); entity.Property(x => x.RelativePath).HasMaxLength(500); entity.HasIndex(x => x.DocumentId); });
        modelBuilder.Entity<DocumentShareLink>(entity => { entity.ToTable("Kb_ShareLink"); entity.HasKey(x => x.Id); entity.Property(x => x.Token).HasMaxLength(64).IsRequired(); entity.HasIndex(x => x.Token).IsUnique(); entity.Property(x => x.PasswordHash).HasMaxLength(200); entity.HasIndex(x => x.DocumentId); });
    }
}
