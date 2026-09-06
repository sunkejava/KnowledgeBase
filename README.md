# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。项目强调专业、克制、工程化、权限安全和长期可维护性，前后端均按可持续扩展方向建设。

## 当前版本

`v0.14.1`

当前已具备：

- JWT / RBAC
- 用户、角色、部门、组织、菜单权限管理
- 知识库与层级文档
- Markdown 编辑
- 标签、收藏、最近访问
- 附件、版本、回滚、Diff
- Viewer / Editor / Manager 资源权限
- 文档显式权限
- 安全分享与访问审计
- 异步导入 / 导出任务
- Markdown / HTML / DOCX / ZIP 导入
- SQLite / Meilisearch 可切换全文搜索
- 搜索索引任务
- 文档评论 / @成员 / 通知中心
- 统一文件存储抽象
- 审计日志
- 主题、语言、水印、字号
- 公共表格 / 树组件
- 服务端分页
- Docker Compose / Nginx
- GitHub Actions CI

详细项目说明：

```text
docs/PROJECT_GUIDE.md
```

SQLite 工程规范：

```text
docs/SQLITE_ENGINEERING_RULES.md
```

---

# v0.14.1 修复与工程加固

## 1. 修复 SQLite DateTimeOffset 排序异常

已修复典型异常：

```text
SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses.
Convert the values to a supported type, or use LINQ to Objects to order the results on the client side.
```

此前多处业务查询直接对 `DateTimeOffset` 执行：

```csharp
.OrderBy(x => x.CreatedAt)
.OrderByDescending(x => x.UpdatedAt)
```

在 EF Core + SQLite 下会运行时失败。

现已统一增加 SQLite 安全扩展：

```text
ToSqliteSafeDateTimeOffsetPageAsync
ToSqliteSafeNullableDateTimeOffsetPageAsync
ToSqliteSafeDateTimeOffsetListAsync
FirstOrDefaultSqliteSafeDateTimeOffsetAsync
```

文件：

```text
backend/KnowledgeBase.Infrastructure/Common/QueryablePagingExtensions.cs
```

已处理范围包括：

- 知识库列表
- 文档树
- 收藏
- 最近访问
- 附件
- 分享链接
- 分享访问日志
- 审计日志
- 评论
- 通知
- 导入任务
- 导出任务
- 搜索索引任务
- SQLite 全文搜索结果
- 后台任务领取顺序

同时检查并处理了 SQLite 对 `DateTimeOffset` 范围比较的兼容问题，例如：

```csharp
.Where(x => x.CreatedAt < cutoff)
```

任务历史清理改为数据库先按普通字段过滤，再在已物化集合中比较 `DateTimeOffset`。

### 大数据量规则

中小列表可以在过滤/投影后通过 LINQ-to-Objects 排序。

如果日志、任务、历史表达到几十万或百万级，禁止把全部数据读入内存排序，必须额外维护：

```text
CreatedAtUnixMs long
UpdatedAtUnixMs long
```

并在 SQLite 中直接对 INTEGER 字段执行索引、范围过滤、排序和分页。

---

## 2. 增加 SQLite DateTimeOffset CI 防回归检查

新增：

```text
scripts/check_sqlite_datetimeoffset.py
```

GitHub Actions 后端构建前会执行：

```bash
python3 scripts/check_sqlite_datetimeoffset.py
```

用于阻止后续再次直接把常见 `DateTimeOffset` 字段交给 SQLite 做不支持的排序/范围比较。

本规则已经沉淀到：

```text
docs/SQLITE_ENGINEERING_RULES.md
```

后续所有 `.NET + EF Core + SQLite` 项目都应默认遵守这套规范。

---

## 3. 清理重复系统管理 Controller

删除旧版：

```text
backend/KnowledgeBase.Api/Controllers/SystemController.cs
```

该 Controller 与新版：

```text
SystemManagementController.cs
```

存在重复 `/api/system/users|roles|departments|organizations|menus` 路由，可能导致运行时路由冲突。

现在系统管理后端统一由：

```text
backend/KnowledgeBase.Api/Controllers/SystemManagementController.cs
```

提供。

---

# 系统管理、菜单权限管理在哪里

前端统一入口：

```text
/system
```

侧边栏名称：

```text
系统管理
```

显示条件：

```text
SUPER_ADMIN
或
system:view
```

默认开发账号：

```text
admin / Admin123!
```

默认 `admin` 为 `SUPER_ADMIN`，因此可以看到完整系统管理页面。

系统管理页面：

```text
frontend/src/views/SystemManagement.vue
```

包含 6 个 Tab：

```text
用户
角色
部门
组织机构
菜单权限
审计日志
```

菜单权限支持维护：

```text
父节点
名称
类型（目录 / 菜单 / 按钮）
前端路由 Path
Permission 权限标识
Icon
Sort
Enabled
```

角色管理通过 `MenuIds` 绑定菜单/按钮权限。

当前用户的角色、权限标识和菜单画像由：

```text
GET /api/system/profile
```

返回，前端统一存储于：

```text
frontend/src/stores/permission.ts
```

更详细的菜单、角色、系统 RBAC、资源权限、数据库表、接口和页面说明，请查看：

```text
docs/PROJECT_GUIDE.md
```

---

## 技术栈

### 后端

- .NET 10
- ASP.NET Core Web API
- DDD + Clean Architecture
- EF Core 10
- SQLite
- JWT
- PBKDF2-SHA256

### 前端

- Vue 3
- TypeScript
- Vite
- Pinia
- Vue Router
- Axios
- Element Plus

### 可选基础设施

- Meilisearch
- Docker Compose
- Nginx

---

# v0.14.0 主要能力

## 统一文件存储抽象

```text
IFileStorage
└─ LocalFileStorage
```

统一接口：

```text
CreateWriteAsync
OpenReadAsync
ExistsAsync
DeleteAsync
ProviderName
```

当前配置：

```json
"Storage": {
  "Provider": "local",
  "Local": {
    "Root": "storage"
  }
}
```

当前已迁移：

```text
文档附件
异步导入源文件
异步导出结果文件
```

后续可以增加：

```text
MinIO
Amazon S3
阿里云 OSS
腾讯云 COS
```

---

## 文档评论 / @成员 / 通知中心

评论页面：

```text
/collaboration
```

通知页面：

```text
/notifications
```

数据表：

```text
Kb_Comment
Kb_CommentMention
Sys_Notification
```

支持：

- 文档评论
- @当前知识库成员
- 评论分页/搜索/导出
- 通知未读数量
- 单条已读
- 全部已读
- 通知定位到对应知识库和文档

---

# 搜索体系

```text
IKnowledgeSearchService
├─ SqliteKnowledgeSearchService
└─ MeilisearchKnowledgeSearchService
```

默认：

```text
Search:Provider = sqlite
```

可选：

```text
Search:Provider = meilisearch
```

Meilisearch 支持：

- 标题 + Markdown 正文
- knowledgeBaseId 权限过滤
- 增量索引
- 删除索引
- 全量重建
- 搜索任务中心
- 等待 Meilisearch taskUid 真正 succeeded 后才完成任务

---

# 导入导出

支持：

```text
Markdown
HTML / HTM
DOCX
Markdown ZIP
```

异步任务：

```text
Sys_ImportTask
Sys_ExportTask
```

支持排队、进度、取消、重试、历史清理和文件清理。

---

# 权限模型

## 系统级 RBAC

控制系统后台：

```text
Sys_User
Sys_Role
Sys_UserRole
Sys_Menu
Sys_RoleMenu
```

## 知识资源级权限

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

这两套权限不是同一套模型，详细说明参见 `docs/PROJECT_GUIDE.md`。

---

# 前端公共组件规范

```text
frontend/src/components/common/
├─ BaseDataTable.vue
├─ BaseTreeManager.vue
└─ PageHeader.vue
```

列表页面统一复用分页、CSV 导出、列显示、列宽调整、列顺序和持久化配置能力。

前端 API 统一放在：

```text
frontend/src/api/modules/
```

业务页面禁止重复拼接 API URL。

---

# 后端开发规范

- 所有解释性代码注释统一使用中文。
- Controller 只处理 HTTP、身份、权限和参数映射。
- 业务逻辑进入 Application / Infrastructure。
- 文件访问统一使用 `IFileStorage`。
- 搜索统一使用 `IKnowledgeSearchService`。
- 数据库结构统一 EF Core Migration。
- 权限校验必须在后端执行。
- SQLite 禁止直接用 `DateTimeOffset` 做 SQL `ORDER BY` 或范围比较。
- 大规模 SQLite 时间查询优先维护 Unix 毫秒 `long` 字段。

---

# 启动

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

---

# Docker

默认 SQLite 搜索：

```bash
docker compose up -d
```

启用 Meilisearch：

```bash
docker compose --profile search up -d
```

持久化卷：

```text
kb-data     SQLite
kb-storage  附件 / 导入 / 导出
meili-data  Meilisearch
```

---

# CI

`.github/workflows/ci.yml` 当前执行：

```text
SQLite DateTimeOffset 查询规则检查
NuGet Restore
NuGet Vulnerability Scan
.NET 10 Release Build
npm install
vue-tsc
Vite Production Build
```

---

# Migration

```text
202609060600_BaselineV060
202609061100_V011ExportAndShareAudit
202609061200_V012ImportTasks
202609061300_V013SearchIndexTasks
202609061400_V014Collaboration
```

---

# 文档索引

完整项目说明：

```text
docs/PROJECT_GUIDE.md
```

SQLite 工程规范：

```text
docs/SQLITE_ENGINEERING_RULES.md
```

---

# 下一阶段

1. 对可能持续增长的日志/任务表增加 `CreatedAtUnixMs`，将 SQLite 时间分页从内存排序进一步升级为数据库 INTEGER 索引排序。
2. `IFileStorage` 增加 MinIO 实现和迁移工具。
3. SignalR 实时通知。
4. 评论回复树与评论编辑。
5. DOCX 表格、图片、超链接导入增强。
6. PDF 文本导入。
7. API Key / Webhook / 开放 API。
