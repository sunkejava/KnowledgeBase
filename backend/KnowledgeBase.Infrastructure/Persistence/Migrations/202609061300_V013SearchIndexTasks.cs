using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KnowledgeBase.Infrastructure.Persistence.Migrations;

/// <summary>
/// v0.13.0 搜索索引任务迁移。新增 Sys_SearchIndexTask，用于记录搜索索引重建状态和失败历史。
/// </summary>
[DbContext(typeof(KnowledgeDbContext))]
[Migration("202609061300_V013SearchIndexTasks")]
public sealed class V013SearchIndexTasks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS Sys_SearchIndexTask (
                Id TEXT NOT NULL CONSTRAINT PK_Sys_SearchIndexTask PRIMARY KEY,
                UserId TEXT NOT NULL,
                Provider TEXT NOT NULL,
                Status TEXT NOT NULL,
                ErrorMessage TEXT NULL,
                CreatedAt TEXT NOT NULL,
                StartedAt TEXT NULL,
                CompletedAt TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Sys_SearchIndexTask_Status_CreatedAt ON Sys_SearchIndexTask(Status, CreatedAt);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS Sys_SearchIndexTask;");
    }
}
