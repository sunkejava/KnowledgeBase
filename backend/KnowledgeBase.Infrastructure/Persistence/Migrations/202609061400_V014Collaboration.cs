using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KnowledgeBase.Infrastructure.Persistence.Migrations;

/// <summary>
/// v0.14.0 协作能力迁移。新增文档评论、@成员关系和站内通知表。
/// </summary>
[DbContext(typeof(KnowledgeDbContext))]
[Migration("202609061400_V014Collaboration")]
public sealed class V014Collaboration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS Kb_Comment (
                Id TEXT NOT NULL CONSTRAINT PK_Kb_Comment PRIMARY KEY,
                DocumentId TEXT NOT NULL,
                UserId TEXT NOT NULL,
                ParentId TEXT NULL,
                Content TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Kb_Comment_DocumentId_CreatedAt ON Kb_Comment(DocumentId, CreatedAt);
            CREATE INDEX IF NOT EXISTS IX_Kb_Comment_UserId ON Kb_Comment(UserId);

            CREATE TABLE IF NOT EXISTS Kb_CommentMention (
                CommentId TEXT NOT NULL,
                UserId TEXT NOT NULL,
                CONSTRAINT PK_Kb_CommentMention PRIMARY KEY (CommentId, UserId)
            );
            CREATE INDEX IF NOT EXISTS IX_Kb_CommentMention_UserId ON Kb_CommentMention(UserId);

            CREATE TABLE IF NOT EXISTS Sys_Notification (
                Id TEXT NOT NULL CONSTRAINT PK_Sys_Notification PRIMARY KEY,
                UserId TEXT NOT NULL,
                Type TEXT NOT NULL,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                TargetUrl TEXT NULL,
                IsRead INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                ReadAt TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Sys_Notification_UserId_IsRead_CreatedAt ON Sys_Notification(UserId, IsRead, CreatedAt);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS Kb_CommentMention;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS Kb_Comment;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS Sys_Notification;");
    }
}
