using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KnowledgeBase.Infrastructure.Persistence.Migrations;

/// <summary>
/// v0.6.0 基线迁移。所有 DDL 均使用 IF NOT EXISTS，既支持从历史 EnsureCreated 数据库平滑接管，
/// 也支持全新数据库直接创建完整结构。
/// </summary>
[DbContext(typeof(KnowledgeDbContext))]
[Migration("202609060600_BaselineV060")]
public sealed class BaselineV060 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS Kb_KnowledgeBase (Id TEXT NOT NULL PRIMARY KEY, Name TEXT NOT NULL, Description TEXT NOT NULL DEFAULT '', CreatedAt TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS Kb_Document (Id TEXT NOT NULL PRIMARY KEY, KnowledgeBaseId TEXT NOT NULL, ParentId TEXT NULL, Title TEXT NOT NULL, Slug TEXT NOT NULL, Status INTEGER NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Kb_Document_KnowledgeBaseId_Slug ON Kb_Document(KnowledgeBaseId, Slug);
CREATE TABLE IF NOT EXISTS Kb_DocumentContent (DocumentId TEXT NOT NULL PRIMARY KEY, Markdown TEXT NOT NULL, FOREIGN KEY(DocumentId) REFERENCES Kb_Document(Id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS Kb_Tag (Id TEXT NOT NULL PRIMARY KEY, Name TEXT NOT NULL, Color TEXT NOT NULL);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Kb_Tag_Name ON Kb_Tag(Name);
CREATE TABLE IF NOT EXISTS Kb_DocumentTag (DocumentId TEXT NOT NULL, TagId TEXT NOT NULL, PRIMARY KEY(DocumentId,TagId));
CREATE TABLE IF NOT EXISTS Kb_Favorite (UserId TEXT NOT NULL, DocumentId TEXT NOT NULL, CreatedAt TEXT NOT NULL, PRIMARY KEY(UserId,DocumentId));
CREATE TABLE IF NOT EXISTS Kb_RecentView (UserId TEXT NOT NULL, DocumentId TEXT NOT NULL, LastViewedAt TEXT NOT NULL, ViewCount INTEGER NOT NULL, PRIMARY KEY(UserId,DocumentId));
CREATE TABLE IF NOT EXISTS Kb_DocumentVersion (Id TEXT NOT NULL PRIMARY KEY, DocumentId TEXT NOT NULL, VersionNumber INTEGER NOT NULL, Title TEXT NOT NULL, Slug TEXT NOT NULL, Markdown TEXT NOT NULL, EditorId TEXT NULL, ChangeNote TEXT NOT NULL, CreatedAt TEXT NOT NULL);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Kb_DocumentVersion_DocumentId_VersionNumber ON Kb_DocumentVersion(DocumentId,VersionNumber);
CREATE TABLE IF NOT EXISTS Kb_Attachment (Id TEXT NOT NULL PRIMARY KEY, DocumentId TEXT NOT NULL, FileName TEXT NOT NULL, StoredName TEXT NOT NULL, ContentType TEXT NOT NULL, Size INTEGER NOT NULL, RelativePath TEXT NOT NULL, UploaderId TEXT NULL, CreatedAt TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS IX_Kb_Attachment_DocumentId ON Kb_Attachment(DocumentId);
CREATE TABLE IF NOT EXISTS Sys_User (Id TEXT NOT NULL PRIMARY KEY, UserName TEXT NOT NULL, DisplayName TEXT NOT NULL, PasswordHash TEXT NOT NULL, PasswordSalt TEXT NOT NULL, Enabled INTEGER NOT NULL, CreatedAt TEXT NOT NULL);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Sys_User_UserName ON Sys_User(UserName);
CREATE TABLE IF NOT EXISTS Sys_Role (Id TEXT NOT NULL PRIMARY KEY, Code TEXT NOT NULL, Name TEXT NOT NULL, Enabled INTEGER NOT NULL);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Sys_Role_Code ON Sys_Role(Code);
CREATE TABLE IF NOT EXISTS Sys_UserRole (UserId TEXT NOT NULL, RoleId TEXT NOT NULL, PRIMARY KEY(UserId,RoleId));
CREATE TABLE IF NOT EXISTS Sys_Department (Id TEXT NOT NULL PRIMARY KEY, ParentId TEXT NULL, Name TEXT NOT NULL, Sort INTEGER NOT NULL, Enabled INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS Sys_Organization (Id TEXT NOT NULL PRIMARY KEY, ParentId TEXT NULL, Name TEXT NOT NULL, Code TEXT NOT NULL, Sort INTEGER NOT NULL DEFAULT 0, Enabled INTEGER NOT NULL DEFAULT 1);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Sys_Organization_Code ON Sys_Organization(Code);
CREATE TABLE IF NOT EXISTS Sys_Menu (Id TEXT NOT NULL PRIMARY KEY, ParentId TEXT NULL, Name TEXT NOT NULL, Path TEXT NOT NULL, Permission TEXT NOT NULL, Type TEXT NOT NULL DEFAULT 'menu', Icon TEXT NOT NULL DEFAULT '', Sort INTEGER NOT NULL, Enabled INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS Sys_RoleMenu (RoleId TEXT NOT NULL, MenuId TEXT NOT NULL, PRIMARY KEY(RoleId,MenuId));
CREATE TABLE IF NOT EXISTS Sys_UserAppearanceSetting (Id TEXT NOT NULL PRIMARY KEY, UserKey TEXT NOT NULL, Theme TEXT NOT NULL, Locale TEXT NOT NULL, FontSize INTEGER NOT NULL, WatermarkEnabled INTEGER NOT NULL, WatermarkText TEXT NOT NULL, CompactMode INTEGER NOT NULL, UpdatedAt TEXT NOT NULL);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Sys_UserAppearanceSetting_UserKey ON Sys_UserAppearanceSetting(UserKey);
CREATE TABLE IF NOT EXISTS Sys_AuditLog (Id TEXT NOT NULL PRIMARY KEY, Category TEXT NOT NULL, Action TEXT NOT NULL, UserName TEXT NOT NULL, Target TEXT NOT NULL, IpAddress TEXT NOT NULL, Success INTEGER NOT NULL, Message TEXT NULL, CreatedAt TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS IX_Sys_AuditLog_CreatedAt ON Sys_AuditLog(CreatedAt);

CREATE TABLE IF NOT EXISTS Kb_KnowledgeBaseMember (KnowledgeBaseId TEXT NOT NULL, UserId TEXT NOT NULL, Role TEXT NOT NULL, CreatedAt TEXT NOT NULL, PRIMARY KEY(KnowledgeBaseId,UserId));
CREATE TABLE IF NOT EXISTS Kb_DocumentPermission (DocumentId TEXT NOT NULL, UserId TEXT NOT NULL, CanView INTEGER NOT NULL, CanEdit INTEGER NOT NULL, CanManage INTEGER NOT NULL, PRIMARY KEY(DocumentId,UserId));
CREATE TABLE IF NOT EXISTS Kb_ShareLink (Id TEXT NOT NULL PRIMARY KEY, DocumentId TEXT NOT NULL, Token TEXT NOT NULL, ExpiresAt TEXT NULL, PasswordHash TEXT NULL, Enabled INTEGER NOT NULL, CreatedBy TEXT NULL, CreatedAt TEXT NOT NULL);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Kb_ShareLink_Token ON Kb_ShareLink(Token);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 基线迁移不自动删除历史业务表，避免误操作破坏已有知识资产。
    }
}
