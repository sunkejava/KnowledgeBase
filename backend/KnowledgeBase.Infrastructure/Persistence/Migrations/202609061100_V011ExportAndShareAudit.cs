using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KnowledgeBase.Infrastructure.Persistence.Migrations;

/// <summary>v0.11：新增导出任务、分享访问日志以及分享访问统计字段。</summary>
[DbContext(typeof(KnowledgeDbContext))]
[Migration("202609061100_V011ExportAndShareAudit")]
public sealed class V011ExportAndShareAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE Kb_ShareLink ADD COLUMN AccessCount INTEGER NOT NULL DEFAULT 0;");
        migrationBuilder.Sql("ALTER TABLE Kb_ShareLink ADD COLUMN LastAccessAt TEXT NULL;");
        migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS Kb_ShareAccessLog (Id TEXT NOT NULL PRIMARY KEY, ShareLinkId TEXT NOT NULL, IpAddress TEXT NOT NULL, UserAgent TEXT NOT NULL, Success INTEGER NOT NULL, Message TEXT NOT NULL, AccessedAt TEXT NOT NULL);");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Kb_ShareAccessLog_ShareLinkId_AccessedAt ON Kb_ShareAccessLog (ShareLinkId, AccessedAt);");
        migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS Sys_ExportTask (Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, KnowledgeBaseId TEXT NOT NULL, Name TEXT NOT NULL, Format TEXT NOT NULL, Status TEXT NOT NULL, FileName TEXT NULL, RelativePath TEXT NULL, ErrorMessage TEXT NULL, CreatedAt TEXT NOT NULL, StartedAt TEXT NULL, CompletedAt TEXT NULL);");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Sys_ExportTask_UserId_CreatedAt ON Sys_ExportTask (UserId, CreatedAt);");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Sys_ExportTask_Status_CreatedAt ON Sys_ExportTask (Status, CreatedAt);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS Sys_ExportTask;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS Kb_ShareAccessLog;");
    }
}
