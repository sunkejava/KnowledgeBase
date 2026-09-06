using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KnowledgeBase.Infrastructure.Persistence.Migrations;

/// <summary>
/// v0.12.0 导入任务迁移。新增 Sys_ImportTask，用于记录 ZIP 异步导入进度、失败原因和任务历史。
/// </summary>
[DbContext(typeof(KnowledgeDbContext))]
[Migration("202609061200_V012ImportTasks")]
public sealed class V012ImportTasks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS Sys_ImportTask (
                Id TEXT NOT NULL CONSTRAINT PK_Sys_ImportTask PRIMARY KEY,
                UserId TEXT NOT NULL,
                KnowledgeBaseId TEXT NOT NULL,
                Name TEXT NOT NULL,
                SourceType TEXT NOT NULL,
                Status TEXT NOT NULL,
                SourceFileName TEXT NOT NULL,
                RelativePath TEXT NOT NULL,
                TotalCount INTEGER NOT NULL DEFAULT 0,
                ProcessedCount INTEGER NOT NULL DEFAULT 0,
                ImportedCount INTEGER NOT NULL DEFAULT 0,
                SkippedCount INTEGER NOT NULL DEFAULT 0,
                ErrorMessage TEXT NULL,
                CreatedAt TEXT NOT NULL,
                StartedAt TEXT NULL,
                CompletedAt TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Sys_ImportTask_UserId_CreatedAt ON Sys_ImportTask(UserId, CreatedAt);
            CREATE INDEX IF NOT EXISTS IX_Sys_ImportTask_Status_CreatedAt ON Sys_ImportTask(Status, CreatedAt);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS Sys_ImportTask;");
    }
}
