using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KnowledgeBase.Infrastructure.Persistence.Migrations;

/// <summary>
/// v0.14.2 数据结构修复与菜单权限初始化。
/// 修复早期基线迁移遗漏 Kb_KnowledgeBase.UpdatedAt 的问题，并补齐系统初始菜单权限节点。
/// </summary>
[DbContext(typeof(KnowledgeDbContext))]
[Migration("202609061500_V0142SchemaAndMenuSeed")]
public sealed class V0142SchemaAndMenuSeed : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 早期 BaselineV060 创建 Kb_KnowledgeBase 时遗漏 UpdatedAt，实体模型却一直包含该属性。
        // 对已有数据库执行增量补列，并使用 CreatedAt 回填，避免所有读取知识库的查询因列缺失失败。
        migrationBuilder.Sql("ALTER TABLE Kb_KnowledgeBase ADD COLUMN UpdatedAt TEXT NOT NULL DEFAULT '1970-01-01T00:00:00+00:00';");
        migrationBuilder.Sql("UPDATE Kb_KnowledgeBase SET UpdatedAt = CreatedAt WHERE UpdatedAt = '1970-01-01T00:00:00+00:00' OR UpdatedAt IS NULL;");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Kb_KnowledgeBase_UpdatedAt ON Kb_KnowledgeBase(UpdatedAt);");

        // 使用固定 GUID 初始化菜单，便于后续版本继续做幂等升级和角色授权。
        migrationBuilder.Sql("""
INSERT OR IGNORE INTO Sys_Menu(Id,ParentId,Name,Path,Permission,Type,Icon,Sort,Enabled) VALUES
('10000000-0000-0000-0000-000000000001',NULL,'工作台','/','dashboard:view','Menu','LayoutDashboard',10,1),
('10000000-0000-0000-0000-000000000010',NULL,'知识资产','','knowledge:view','Directory','Boxes',20,1),
('10000000-0000-0000-0000-000000000011','10000000-0000-0000-0000-000000000010','我的知识库','/knowledge-bases','knowledgebase:view','Menu','Boxes',21,1),
('10000000-0000-0000-0000-000000000012','10000000-0000-0000-0000-000000000010','全部文档','/knowledge-center?tab=search','document:view','Menu','FileText',22,1),
('10000000-0000-0000-0000-000000000013','10000000-0000-0000-0000-000000000010','最近浏览','/knowledge-center?tab=recent','recent:view','Menu','Clock3',23,1),
('10000000-0000-0000-0000-000000000014','10000000-0000-0000-0000-000000000010','我的收藏','/knowledge-center?tab=favorites','favorite:view','Menu','Star',24,1),
('10000000-0000-0000-0000-000000000015','10000000-0000-0000-0000-000000000010','协作评论','/collaboration','collaboration:view','Menu','MessageSquareText',25,1),
('10000000-0000-0000-0000-000000000020',NULL,'平台管理','','platform:view','Directory','Settings2',30,1),
('10000000-0000-0000-0000-000000000021','10000000-0000-0000-0000-000000000020','标签管理','/knowledge-center?tab=tags','tag:view','Menu','BookOpen',31,1),
('10000000-0000-0000-0000-000000000022','10000000-0000-0000-0000-000000000020','通知中心','/notifications','notification:view','Menu','Bell',32,1),
('10000000-0000-0000-0000-000000000023','10000000-0000-0000-0000-000000000020','数据交换','/data-exchange','exchange:view','Menu','Download',33,1),
('10000000-0000-0000-0000-000000000024','10000000-0000-0000-0000-000000000020','搜索管理','/search-management','search:manage','Menu','FileSearch',34,1),
('10000000-0000-0000-0000-000000000030',NULL,'系统管理','/system','system:view','Directory','Settings',40,1),
('10000000-0000-0000-0000-000000000031','10000000-0000-0000-0000-000000000030','用户管理','','system:user:view','Button','',41,1),
('10000000-0000-0000-0000-000000000032','10000000-0000-0000-0000-000000000030','新增用户','','system:user:create','Button','',42,1),
('10000000-0000-0000-0000-000000000033','10000000-0000-0000-0000-000000000030','编辑用户','','system:user:update','Button','',43,1),
('10000000-0000-0000-0000-000000000034','10000000-0000-0000-0000-000000000030','删除用户','','system:user:delete','Button','',44,1),
('10000000-0000-0000-0000-000000000035','10000000-0000-0000-0000-000000000030','角色管理','','system:role:view','Button','',45,1),
('10000000-0000-0000-0000-000000000036','10000000-0000-0000-0000-000000000030','角色授权','','system:role:update','Button','',46,1),
('10000000-0000-0000-0000-000000000037','10000000-0000-0000-0000-000000000030','部门管理','','system:department:manage','Button','',47,1),
('10000000-0000-0000-0000-000000000038','10000000-0000-0000-0000-000000000030','组织管理','','system:organization:manage','Button','',48,1),
('10000000-0000-0000-0000-000000000039','10000000-0000-0000-0000-000000000030','菜单管理','','system:menu:manage','Button','',49,1),
('10000000-0000-0000-0000-000000000040','10000000-0000-0000-0000-000000000030','审计日志','','system:audit:view','Button','',50,1);

INSERT OR IGNORE INTO Sys_RoleMenu(RoleId,MenuId)
SELECT r.Id,m.Id FROM Sys_Role r CROSS JOIN Sys_Menu m WHERE r.Code='SUPER_ADMIN';
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // SQLite 删除列会触发表重建，且该修复列已成为正式模型的一部分，因此回滚时不删除 UpdatedAt。
        migrationBuilder.Sql("DELETE FROM Sys_RoleMenu WHERE MenuId LIKE '10000000-%';");
        migrationBuilder.Sql("DELETE FROM Sys_Menu WHERE Id LIKE '10000000-%';");
    }
}
