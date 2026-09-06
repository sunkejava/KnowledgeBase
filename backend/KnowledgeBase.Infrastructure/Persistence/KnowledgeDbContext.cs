using KnowledgeBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Persistence;

/// <summary>
/// 知识库数据上下文。开发环境默认 SQLite，后续可替换 PostgreSQL。
/// </summary>
public sealed class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<KnowledgeBaseSpace> KnowledgeBases => Set<KnowledgeBaseSpace>();
    public DbSet<UserAppearanceSetting> UserAppearanceSettings => Set<UserAppearanceSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Kb_Document");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(220).IsRequired();
            entity.HasIndex(x => new { x.KnowledgeBaseId, x.Slug }).IsUnique();
        });

        modelBuilder.Entity<KnowledgeBaseSpace>(entity =>
        {
            entity.ToTable("Kb_KnowledgeBase");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<UserAppearanceSetting>(entity =>
        {
            entity.ToTable("Sys_UserAppearanceSetting");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserKey).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.UserKey).IsUnique();
            entity.Property(x => x.Theme).HasMaxLength(20);
            entity.Property(x => x.Locale).HasMaxLength(20);
            entity.Property(x => x.WatermarkText).HasMaxLength(40);
        });
    }
}
