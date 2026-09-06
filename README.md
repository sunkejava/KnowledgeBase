# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。产品强调专业、克制、工程化和长期可维护性，不把 AI 对话框作为视觉中心。

## 当前版本

`v0.6.0`

当前已经具备：JWT/RBAC、系统管理、知识库与层级文档、Markdown 编辑、标签、收藏、最近访问、附件、版本/回滚/Diff、基础全文搜索、Markdown 导入导出、公开分享、资源权限配置、审计日志，以及主题/语言/水印/字号等界面个性化能力。

## 技术栈

- 后端：.NET 10 / ASP.NET Core Web API / DDD + Clean Architecture / EF Core 10 / SQLite / JWT
- 前端：Vue 3 / TypeScript / Vite / Pinia / Vue Router / Axios / Element Plus

## 已实现

### 身份认证与系统权限
- JWT 登录、Bearer Token 自动注入、401 自动退出
- PBKDF2-SHA256 密码哈希
- 用户 CRUD、启停用、密码维护
- 角色 CRUD、用户角色分配
- 菜单、路由、按钮权限标识
- 角色菜单授权
- 当前用户权限画像 `/api/system/profile`
- 前端 Permission Store
- 默认超级管理员保护

默认开发账号：`admin / Admin123!`。生产部署必须修改默认密码与 `Jwt:Key`。

### 组织管理
- 用户、角色、部门、组织机构、菜单管理
- 部门/组织父子结构、排序、启停状态
- 菜单支持目录 / 菜单 / 按钮三类节点
- 系统管理接口受 `SUPER_ADMIN` 保护

### 知识库与文档
- 知识库 CRUD
- 知识库工作区
- 文档目录树、父子层级
- Markdown 新建、编辑、保存、删除
- `Kb_Document` / `Kb_DocumentContent` 分表
- 目录列表不加载正文，打开文档时按需加载
- 知识中心统一承载搜索、最近访问、收藏和标签

### 文档版本与 Diff
- 手动版本快照
- 编辑已有文档时自动创建“编辑前快照”
- 版本列表 / 详情 / 回滚
- 回滚前自动备份当前版本
- Markdown 行级 Diff
- 版本对比当前文档
- Diff 当前限制前 2000 行，避免异常大文档造成内存膨胀

### 标签 / 收藏 / 最近访问
- 标签 CRUD、标签颜色
- 文档多标签绑定
- 收藏 / 取消收藏
- 当前用户收藏列表
- 打开文档自动记录最近访问
- 最近访问时间和累计次数

### 附件
- 附件上传、列表、鉴权下载、删除
- 默认单文件上限 50 MB
- 文件默认保存到 `storage/attachments`
- 元数据保存于 `Kb_Attachment`
- 后续可替换 MinIO / S3 / OSS / COS

### 搜索
- 标题与 Markdown 正文搜索
- 按知识库过滤
- 命中摘要
- 当前 SQLite 直接查询实现基础搜索
- 服务入口已隔离，后续可切 Meilisearch / PostgreSQL FTS / OpenSearch

### Markdown 导入导出
- 单文档导出 `.md`
- 将 `.md` / `.markdown` 导入指定知识库
- 导入时可挂载到当前文档作为子文档
- 单次 Markdown 导入上限 20 MB
- 工作区提供导入 / 导出入口

### 文档分享
- 登录用户创建分享链接
- 分享链接列表 / 停用 API
- 可配置过期时间
- `/share/{token}` 匿名只读页面
- 公共接口不会暴露编辑和管理能力
- 当前版本暂未开放访问密码，密码字段已在数据结构中预留

### 知识库 / 文档数据权限
v0.6.0 开始加入资源级权限数据模型：

- `Kb_KnowledgeBaseMember`
- 知识库成员角色：`Viewer / Editor / Manager`
- `Kb_DocumentPermission`
- 单文档用户权限：查看 / 编辑 / 管理
- 超级管理员可通过 `/api/access/*` 维护成员和文档权限

当前阶段完成的是**权限数据与管理 API**。下一阶段将把该数据正式接入知识库、文档读取/编辑/删除的资源级授权判断。

### 审计与安全
- `Sys_AuditLog`
- 登录与写操作自动审计中间件
- 记录用户、请求路径、IP、成功状态和异常摘要
- 审计日志查询
- 修改密码接口

### 界面个性化
- 深色 / 浅色 / 跟随系统
- 简体中文 / English 基础切换
- 字号 12~18px
- 自定义水印
- 紧凑模式
- 本地即时持久化 + 后端账号偏好接口

## EF Core Migration

从 `v0.6.0` 开始正式切换到 EF Core Migration：

```csharp
await db.Database.MigrateAsync();
```

不再使用 `EnsureCreatedAsync` 作为版本升级机制。

首个基线迁移：

```text
202609060600_BaselineV060
```

该迁移的 DDL 使用 `CREATE TABLE/INDEX IF NOT EXISTS`，因此：

- 从 v0.5 及更早版本升级时，可以接管历史 SQLite 数据库；
- 不再要求为了新增表而删除 `knowledgebase.db`；
- 全新环境也能通过 Migration 初始化数据库。

后续新增迁移建议：

```bash
cd backend/KnowledgeBase.Api
dotnet ef migrations add <MigrationName> \
  --project ../KnowledgeBase.Infrastructure \
  --startup-project .

dotnet ef database update \
  --project ../KnowledgeBase.Infrastructure \
  --startup-project .
```

> 升级生产数据前仍建议先备份数据库。

## 主要数据表

```text
Sys_User
Sys_Role
Sys_UserRole
Sys_Department
Sys_Organization
Sys_Menu
Sys_RoleMenu
Sys_UserAppearanceSetting
Sys_AuditLog

Kb_KnowledgeBase
Kb_Document
Kb_DocumentContent
Kb_Tag
Kb_DocumentTag
Kb_Favorite
Kb_RecentView
Kb_DocumentVersion
Kb_Attachment
Kb_ShareLink
Kb_KnowledgeBaseMember
Kb_DocumentPermission
```

## 启动

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

默认 SQLite 数据库：`knowledgebase.db`。

## 主要 API

### 认证 / 系统
- `POST /api/auth/login`
- `GET /api/system/profile`
- `GET|POST|PUT|DELETE /api/system/users`
- `GET|POST|PUT|DELETE /api/system/roles`
- `GET|POST|PUT|DELETE /api/system/departments`
- `GET|POST|PUT|DELETE /api/system/organizations`
- `GET|POST|PUT|DELETE /api/system/menus`
- `POST /api/system/change-password`
- `GET /api/system/audit-logs`

### 知识库 / 文档
- `GET|POST|PUT|DELETE /api/knowledge-bases`
- `GET|POST|PUT|DELETE /api/documents`

### 知识资产
- `GET|POST|PUT|DELETE /api/knowledge-assets/tags`
- `GET|PUT /api/knowledge-assets/documents/{id}/tags`
- `POST /api/knowledge-assets/documents/{id}/favorite`
- `GET /api/knowledge-assets/favorites`
- `POST /api/knowledge-assets/documents/{id}/recent`
- `GET /api/knowledge-assets/recent`
- `POST /api/knowledge-assets/documents/{id}/versions`
- `GET /api/knowledge-assets/documents/{id}/versions`
- `GET /api/knowledge-assets/versions/{versionId}`
- `POST /api/knowledge-assets/versions/{versionId}/restore`
- `GET /api/knowledge-assets/search`

### 导入 / 导出 / Diff
- `GET /api/content-exchange/documents/{id}/markdown`
- `POST /api/content-exchange/knowledge-bases/{knowledgeBaseId}/markdown`
- `GET /api/content-exchange/versions/{versionId}/diff-current`

### 分享
- `POST /api/share/documents/{documentId}`
- `GET /api/share/documents/{documentId}`
- `DELETE /api/share/links/{id}`
- `GET /api/share/public/{token}`（匿名）

### 资源权限
- `GET|PUT /api/access/knowledge-bases/{id}/members`
- `DELETE /api/access/knowledge-bases/{id}/members/{userId}`
- `GET|PUT /api/access/documents/{id}/permissions`
- `DELETE /api/access/documents/{id}/permissions/{userId}`

### 附件
- `POST /api/attachments/document/{documentId}`
- `GET /api/attachments/document/{documentId}`
- `GET /api/attachments/{id}/download`
- `DELETE /api/attachments/{id}`

### 偏好设置
- `GET|PUT /api/settings/appearance`

## 架构原则

1. Controller 不直接承载知识库核心业务逻辑。
2. 文档目录与正文分离，禁止工作区一次加载全部正文。
3. 文档附件、版本、标签按需加载。
4. 数据库升级统一由 Migration 管理。
5. 权限从系统 RBAC 向知识库、文档资源级授权继续下沉。
6. 文件存储、搜索、Embedding、LLM 保持抽象，可独立替换。
7. 前端所有主要交互连接真实 API，不做假页面。
8. 关键管理和知识资产写操作纳入统一审计。

## 下一阶段

1. 将 `Kb_KnowledgeBaseMember` / `Kb_DocumentPermission` 正式接入资源访问授权。
2. 完善知识库成员/文档权限前端管理面板。
3. 分享链接增加访问密码、访问次数、访问日志和手动失效时间。
4. Markdown 批量 / ZIP 导入导出。
5. Word、PDF、HTML 文档解析导入。
6. Meilisearch 中文全文索引、高亮和异步索引任务。
7. 评论、@成员、通知中心。
8. API Key、Webhook、开放 API。
9. 最后再接入 Embedding、RAG、语义检索和知识问答；智能能力保持可关闭、可替换。
