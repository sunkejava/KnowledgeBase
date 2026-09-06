# KnowledgeBase 项目详细说明

> 适用版本：v0.14.x 及后续版本
>
> 技术栈：.NET 10 + Vue 3 + TypeScript + EF Core 10 + SQLite + JWT + Element Plus

---

## 1. 项目定位

KnowledgeBase 是一套面向企业内部知识沉淀、文档协作、权限控制、检索、分享和数据交换场景的知识库管理系统。

项目设计目标：

- 前后端彻底分离；
- 后端采用 .NET 10、DDD/Clean Architecture 分层；
- 前端采用 Vue 3 + TypeScript；
- 默认 SQLite，部署简单；
- 可选 Meilisearch 提升全文搜索能力；
- Viewer / Editor / Manager 资源级权限；
- 系统级 RBAC 与知识库级权限同时存在；
- 附件、导入、导出统一通过文件存储抽象；
- 支持 Docker 部署；
- 后端解释性注释统一使用中文；
- 前端表格、树、分页、导出、列配置、接口调用均优先复用公共基础设施。

---

# 2. 项目目录

```text
KnowledgeBase/
├─ backend/
│  ├─ KnowledgeBase.Api/             ASP.NET Core Web API、Controller、中间件、Worker
│  ├─ KnowledgeBase.Application/     应用层接口与业务抽象
│  ├─ KnowledgeBase.Contracts/       DTO、请求模型、分页契约
│  ├─ KnowledgeBase.Domain/          领域实体
│  └─ KnowledgeBase.Infrastructure/  EF Core、服务实现、搜索、存储等
│
├─ frontend/
│  ├─ src/api/                       Axios 与领域 API 模块
│  ├─ src/components/common/         公共页面组件
│  ├─ src/components/knowledge/      知识域公共组件
│  ├─ src/stores/                    Pinia Store
│  ├─ src/views/                     页面
│  └─ src/router/                    前端路由
│
├─ docs/                             项目说明与工程规范
├─ scripts/                          CI/工程检查脚本
├─ docker-compose.yml
└─ KnowledgeBase.slnx
```

---

# 3. 登录与默认账号

开发环境默认管理员：

```text
用户名：admin
密码：Admin123!
```

默认管理员拥有：

```text
SUPER_ADMIN
```

生产环境上线后必须：

1. 修改默认管理员密码；
2. 修改 `Jwt:Key`；
3. 不要继续使用开发环境示例密钥。

---

# 4. 前端主要页面与路由

## 4.1 工作台

```text
/
```

用于平台总览。

---

## 4.2 我的知识库

```text
/knowledge-bases
```

功能：

- 知识库列表；
- 服务端分页；
- 搜索；
- 新建、编辑、删除；
- 进入文档工作区；
- 进入资源权限管理。

---

## 4.3 知识库工作区

```text
/knowledge-bases/{knowledgeBaseId}
```

主要能力：

- 文档树；
- Markdown 编辑；
- 新建文档；
- 新建子文档；
- 保存；
- 删除；
- 标签；
- 收藏；
- 最近访问；
- 附件；
- 历史版本；
- Diff；
- 回滚；
- 分享；
- Markdown 导入导出。

正文与目录元数据分开加载，进入知识库时不会一次性加载所有 Markdown 正文。

---

## 4.4 知识中心

```text
/knowledge-center
```

支持 Tab：

```text
search      全文搜索
recent      最近浏览
favorites   我的收藏
tags        标签
```

---

## 4.5 资源权限管理

```text
/knowledge-bases/{knowledgeBaseId}/access
```

包括：

- 知识库成员；
- Viewer / Editor / Manager；
- 文档显式权限；
- 分享安全管理。

---

## 4.6 数据交换

```text
/data-exchange
```

包括：

- 异步导出任务；
- 异步导入任务；
- ZIP 导入；
- Markdown 导入；
- HTML 导入；
- DOCX 导入；
- 下载导出结果；
- 任务取消；
- 任务重试；
- 历史清理。

---

## 4.7 搜索管理

```text
/search-management
```

仅超级管理员使用。

功能：

- 查看当前搜索 Provider；
- SQLite / Meilisearch 状态；
- 创建全量索引重建任务；
- 查看任务；
- 重试；
- 清理历史任务。

---

## 4.8 协作评论

```text
/collaboration
```

功能：

- 选择知识库；
- 选择文档；
- 评论；
- @知识库成员；
- 评论分页；
- 评论搜索；
- 评论删除；
- CSV 导出。

---

## 4.9 通知中心

```text
/notifications
```

功能：

- 未读数量；
- 通知分页；
- 搜索；
- 单条已读；
- 全部已读；
- 从 @通知定位到知识库对应文档。

---

# 5. 系统管理、菜单权限管理页面在哪里

系统后台统一入口：

```text
/system
```

侧边栏名称：

```text
系统管理
```

显示条件：

```text
当前用户角色包含 SUPER_ADMIN
或
当前用户拥有 system:view 权限
```

如果普通用户没有 `system:view`，侧边栏不会显示系统管理入口，这是权限设计，不是页面丢失。

系统管理页面文件：

```text
frontend/src/views/SystemManagement.vue
```

前端接口模块：

```text
frontend/src/api/modules/system.ts
```

后端 Controller：

```text
backend/KnowledgeBase.Api/Controllers/SystemManagementController.cs
```

接口前缀：

```text
/api/system
```

系统管理页面包含 6 个 Tab：

```text
用户
角色
部门
组织机构
菜单权限
审计日志
```

---

# 6. 用户管理

入口：

```text
系统管理 -> 用户
```

支持：

- 用户分页；
- 用户名查询；
- 姓名查询；
- 新增用户；
- 编辑用户；
- 启停用户；
- 修改密码；
- 分配角色；
- 删除用户；
- CSV 导出；
- 自定义显示列；
- 调整列宽；
- 列配置持久化。

接口：

```text
GET    /api/system/users
POST   /api/system/users
PUT    /api/system/users/{id}
DELETE /api/system/users/{id}
```

用户管理接口要求：

```text
SUPER_ADMIN
```

---

# 7. 角色与菜单授权

入口：

```text
系统管理 -> 角色
```

角色用于系统级 RBAC。

角色字段：

```text
Code
Name
Enabled
MenuIds
```

维护角色时可以为角色分配多个菜单/按钮节点。

接口：

```text
GET    /api/system/roles
POST   /api/system/roles
PUT    /api/system/roles/{id}
DELETE /api/system/roles/{id}
```

关联表：

```text
Sys_Role
Sys_UserRole
Sys_RoleMenu
```

内置：

```text
SUPER_ADMIN
```

不可删除。

---

# 8. 菜单权限管理

入口：

```text
系统管理 -> 菜单权限
```

这是系统菜单、路由菜单和按钮权限的统一维护页面。

支持节点类型：

```text
Directory   目录
Menu        菜单
Button      按钮/操作权限
```

字段：

```text
Name        名称
ParentId    父节点
Path        前端路由
Permission  权限标识
Type        节点类型
Icon        图标
Sort        排序
Enabled     是否启用
```

示例：

```text
系统管理
├─ 用户管理
│  ├─ system:user:view
│  ├─ system:user:create
│  ├─ system:user:update
│  └─ system:user:delete
├─ 角色管理
├─ 部门管理
└─ 菜单管理
```

接口：

```text
GET    /api/system/menus
POST   /api/system/menus
PUT    /api/system/menus/{id}
DELETE /api/system/menus/{id}
```

当前菜单数据来自：

```text
Sys_Menu
```

角色菜单关联：

```text
Sys_RoleMenu
```

当前用户权限画像：

```text
GET /api/system/profile
```

返回：

```text
Roles
Permissions
Menus
```

前端 Pinia：

```text
frontend/src/stores/permission.ts
```

负责缓存当前用户角色、权限标识和可见菜单信息。

---

# 9. 部门与组织机构

入口：

```text
系统管理 -> 部门
系统管理 -> 组织机构
```

统一使用：

```text
BaseTreeManager
```

支持：

- 根节点；
- 子节点；
- 编辑；
- 删除；
- 排序；
- 启停；
- 导出；
- 列设置。

表：

```text
Sys_Department
Sys_Organization
```

---

# 10. 审计日志

入口：

```text
系统管理 -> 审计日志
```

接口：

```text
GET /api/system/audit-logs
```

记录：

- 用户；
- 请求动作；
- 请求路径；
- IP；
- 成功/失败；
- 错误信息；
- 操作时间。

表：

```text
Sys_AuditLog
```

写入中间件：

```text
backend/KnowledgeBase.Api/Middleware/AuditMiddleware.cs
```

---

# 11. 两套权限模型的区别

项目存在两套权限，不要混淆。

## 11.1 系统级 RBAC

控制：

```text
用户管理
角色管理
菜单管理
系统页面
系统按钮
系统配置
```

核心表：

```text
Sys_User
Sys_Role
Sys_UserRole
Sys_Menu
Sys_RoleMenu
```

## 11.2 知识资源级权限

控制：

```text
某个知识库能否查看
某个知识库能否编辑
某个知识库能否管理
某个具体文档能否查看/编辑/管理
```

知识库角色：

```text
Viewer
Editor
Manager
```

核心表：

```text
Kb_KnowledgeBaseMember
Kb_DocumentPermission
```

不要使用系统角色代替知识库成员权限。

---

# 12. Viewer / Editor / Manager

```text
Viewer
  查看知识库
  查看文档
  搜索
  收藏
  最近访问
  评论

Editor
  Viewer 全部能力
  新建文档
  编辑文档
  标签绑定
  上传附件
  导入内容

Manager
  Editor 全部能力
  删除资源
  成员管理
  文档权限管理
  分享管理
```

超级管理员绕过资源权限检查。

---

# 13. 核心数据库表

系统类：

```text
Sys_User
Sys_Role
Sys_UserRole
Sys_Menu
Sys_RoleMenu
Sys_Department
Sys_Organization
Sys_AuditLog
Sys_UserAppearanceSetting
Sys_ExportTask
Sys_ImportTask
Sys_SearchIndexTask
Sys_Notification
```

知识类：

```text
Kb_KnowledgeBase
Kb_KnowledgeBaseMember
Kb_Document
Kb_DocumentContent
Kb_DocumentPermission
Kb_DocumentVersion
Kb_Tag
Kb_DocumentTag
Kb_Favorite
Kb_RecentView
Kb_Attachment
Kb_ShareLink
Kb_ShareAccessLog
Kb_Comment
Kb_CommentMention
```

---

# 14. 为什么 Document 和 DocumentContent 分表

设计：

```text
Kb_Document
  只保存标题、父级、状态、时间等目录元数据

Kb_DocumentContent
  保存 Markdown 正文
```

原因：

进入知识库时只查询：

```text
Kb_Document
```

用户真正打开文档时才读取：

```text
Kb_DocumentContent
```

避免知识库文档越多，进入工作区越慢。

---

# 15. 搜索体系

抽象：

```text
IKnowledgeSearchService
```

实现：

```text
SqliteKnowledgeSearchService
MeilisearchKnowledgeSearchService
```

配置：

```json
"Search": {
  "Provider": "sqlite"
}
```

或：

```text
Search:Provider=meilisearch
```

Meilisearch 索引支持：

- 标题；
- Markdown 正文；
- knowledgeBaseId 权限过滤；
- 单文档增量索引；
- 全量重建；
- 后台索引任务。

---

# 16. 文件存储

统一抽象：

```text
IFileStorage
```

当前实现：

```text
LocalFileStorage
```

目前以下业务都通过统一存储抽象：

- 文档附件；
- 导入源文件；
- 导出结果。

配置：

```json
"Storage": {
  "Provider": "local",
  "Local": {
    "Root": "storage"
  }
}
```

后续可以增加：

```text
MinIOFileStorage
S3FileStorage
OssFileStorage
CosFileStorage
```

而不修改附件、导入、导出业务代码。

---

# 17. 后台任务

目前有三类持久化任务：

```text
Sys_ExportTask
Sys_ImportTask
Sys_SearchIndexTask
```

由后台 Worker 顺序处理。

状态通常包括：

```text
Pending
Running
Completed
Failed
Cancelled
```

这样服务重启后任务记录不会丢失。

---

# 18. SQLite DateTimeOffset 强制规范

这是项目必须长期遵守的规则。

SQLite / EF Core 不能可靠支持：

```csharp
query.OrderBy(x => x.CreatedAt)
query.OrderByDescending(x => x.UpdatedAt)
query.Where(x => x.CreatedAt < cutoff)
```

当字段类型是：

```csharp
DateTimeOffset
```

时禁止直接这样写。

中小数据量列表使用：

```text
ToSqliteSafeDateTimeOffsetPageAsync
ToSqliteSafeDateTimeOffsetListAsync
FirstOrDefaultSqliteSafeDateTimeOffsetAsync
```

这些方法定义在：

```text
backend/KnowledgeBase.Infrastructure/Common/QueryablePagingExtensions.cs
```

原则：

```text
数据库：Where 普通字段 + Select
内存：DateTimeOffset 排序/范围比较
```

对于百万级、大规模日志/任务表，不应把全部记录载入内存，而应额外维护：

```text
CreatedAtUnixMs long
UpdatedAtUnixMs long
```

数据库中使用 long 完成：

```text
ORDER BY
WHERE range
Skip
Take
```

CI 中已经增加：

```text
scripts/check_sqlite_datetimeoffset.py
```

用于阻止常见 DateTimeOffset 不安全 LINQ 再次进入主分支。

详细规则参见：

```text
docs/SQLITE_ENGINEERING_RULES.md
```

---

# 19. 前端公共组件规范

公共组件：

```text
frontend/src/components/common/
├─ BaseDataTable.vue
├─ BaseTreeManager.vue
└─ PageHeader.vue
```

`BaseDataTable` 负责：

- 本地分页；
- 服务端分页；
- CSV 导出；
- 勾选导出；
- 显示/隐藏列；
- 调整列宽；
- 列顺序；
- 配置持久化；
- 排序；
- 自定义单元格；
- 操作列。

任何新列表页面原则上都必须优先复用该组件。

---

# 20. 前端 API 规范

业务页面禁止自行散落 Axios URL。

统一放在：

```text
frontend/src/api/modules/
```

当前主要模块：

```text
auth.ts
system.ts
knowledge.ts
documents.ts
access.ts
share.ts
exchange.ts
search.ts
collaboration.ts
```

页面只调用：

```ts
systemApi.users(...)
documentApi.get(...)
collaborationApi.notifications(...)
```

而不是在页面中重复：

```ts
http.get('/system/users')
```

---

# 21. 后端开发规范

1. 后端解释性注释全部使用中文；
2. 标准技术名保留英文，如 JWT、EF Core、SQLite；
3. Controller 只做 HTTP、参数、身份和权限；
4. 业务逻辑放 Application/Infrastructure 服务；
5. 数据库查询优先过滤后再加载；
6. 大文件走后台任务；
7. 数据库变更必须走 EF Core Migration；
8. 权限必须由后端执行；
9. SQLite 禁止直接用 DateTimeOffset 做 ORDER BY / 范围比较；
10. 公共逻辑不得复制到多个 Service/Controller。

---

# 22. 启动方式

后端：

```bash
cd backend/KnowledgeBase.Api
dotnet restore
dotnet run
```

前端：

```bash
cd frontend
npm install
npm run dev
```

默认：

```text
API：http://localhost:5000
Web：http://localhost:5173
```

---

# 23. Docker 部署

默认 SQLite 搜索：

```bash
docker compose up -d
```

启用 Meilisearch：

```bash
docker compose --profile search up -d
```

生产环境建议先复制：

```text
.env.example -> .env
```

并修改：

```text
JWT_KEY
MEILISEARCH_API_KEY
VITE_API_BASE_URL
```

---

# 24. CI

GitHub Actions：

```text
.github/workflows/ci.yml
```

执行：

```text
SQLite DateTimeOffset 静态规则检查
NuGet 漏洞检查
.NET 10 Release Build
vue-tsc
Vite Production Build
```

任何一项失败都不应该视为可交付版本。

---

# 25. 常见问题

## 25.1 为什么看不到“系统管理”菜单？

需要：

```text
SUPER_ADMIN
```

或权限：

```text
system:view
```

默认 `admin` 是 `SUPER_ADMIN`。

## 25.2 菜单管理在哪里？

```text
/system
-> 菜单权限 Tab
```

## 25.3 角色如何授权菜单？

```text
/system
-> 角色
-> 编辑角色
-> 分配 MenuIds
```

## 25.4 知识库 Manager 与系统角色是什么关系？

不是同一套权限。

```text
系统角色 -> 管系统后台
Viewer/Editor/Manager -> 管具体知识库资源
```

## 25.5 SQLite 报 DateTimeOffset ORDER BY 错误怎么办？

禁止直接：

```csharp
.OrderByDescending(x => x.CreatedAt)
```

改用项目统一 SQLite 安全扩展。

---

# 26. 后续建议

优先级建议：

1. MinIO 存储实现；
2. SignalR 实时通知；
3. 评论回复树；
4. DOCX 图片/表格/超链接增强；
5. PDF 文本导入；
6. 系统菜单动态路由进一步完善；
7. 服务端 Excel 全量导出；
8. API Key / Webhook；
9. 可选 Embedding / RAG / 语义检索。
