# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。产品强调专业、克制、工程化和长期可维护性，不把 AI 对话框作为视觉中心。

## 当前版本

`v0.5.0`

当前已经具备：登录与 RBAC、系统管理、知识库与层级文档、Markdown 编辑、标签、收藏、最近访问、附件、文档版本与回滚、基础全文搜索、审计日志，以及主题/语言/水印/字号等界面个性化能力。

## 技术栈

- 后端：.NET 10 / ASP.NET Core Web API / DDD + Clean Architecture / EF Core 10 / SQLite / JWT
- 前端：Vue 3 / TypeScript / Vite / Pinia / Vue Router / Axios / Element Plus

## 已实现

### 身份认证与权限
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

### 系统与组织管理
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
- 目录列表不加载 Markdown 正文，打开文档时按需加载
- 知识中心统一承载搜索、最近访问、收藏和标签

### 文档版本
- 手动创建版本快照
- 编辑已存在文档时自动创建“编辑前快照”
- 版本列表
- 版本详情 API
- 一键恢复历史版本
- 恢复前自动保留当前内容快照
- 版本正文独立存储于 `Kb_DocumentVersion`

### 标签
- 标签 CRUD API
- 文档与标签多对多关系
- 工作区为文档绑定/取消标签
- 标签颜色字段
- `Kb_Tag` / `Kb_DocumentTag`

### 收藏与最近访问
- 文档收藏 / 取消收藏
- 当前用户收藏列表
- 打开文档自动记录最近访问
- 最近访问时间与累计次数
- `Kb_Favorite` / `Kb_RecentView`

### 附件
- 文档附件上传
- 默认单文件上限 50 MB
- 附件列表、下载、删除
- 元数据存储于 `Kb_Attachment`
- 文件默认保存到应用目录 `storage/attachments`
- 下载使用鉴权请求，不暴露匿名物理目录
- 后续可通过存储抽象切换 MinIO / S3 / OSS / COS

### 全文搜索
- 标题搜索
- Markdown 正文搜索
- 可按知识库过滤
- 返回命中内容摘要
- 当前 SQLite 版本使用数据库查询实现基础全文搜索
- 后续数据量增大后切换 Meilisearch / PostgreSQL FTS / OpenSearch，不改变上层搜索入口

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

默认 SQLite 数据库为 `knowledgebase.db`。

> 当前仍处于快速开发阶段，数据库启动使用 `EnsureCreatedAsync`。如果本地已经存在旧版本数据库，EF Core 不会自动向现有 SQLite 文件补建新增表。开发环境从旧版本升级到 v0.5.0 时建议先备份后删除旧 `knowledgebase.db` 再启动。下一阶段将正式切换 EF Core Migration，之后数据库结构通过 Migration 增量升级，不再要求删除数据库。

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

### 附件
- `POST /api/attachments/document/{documentId}`
- `GET /api/attachments/document/{documentId}`
- `GET /api/attachments/{id}/download`
- `DELETE /api/attachments/{id}`

### 偏好设置
- `GET|PUT /api/settings/appearance`

## 架构原则

1. Controller 不直接承载知识库核心业务逻辑。
2. 文档目录与正文分离，杜绝工作区一次加载全部正文。
3. 文档附件只在需要时加载，不跟随正文/目录返回。
4. 历史版本使用快照表保存，避免覆盖后无法回滚。
5. 权限从系统级向知识库、空间、文档级逐层扩展。
6. 文件存储、全文搜索、Embedding、LLM 保持抽象，可独立替换。
7. 前端表格、树、收藏、版本、附件、标签均连接真实 API，不做假页面。
8. 关键管理与知识资产操作纳入统一审计。

## 下一阶段

优先级从高到低：

1. **EF Core Migration 正式数据库升级体系**，解决版本升级必须删开发库的问题。
2. 文档版本 Diff，对 Markdown 做行级差异展示。
3. 标签管理页完善新增/编辑/删除与标签筛选。
4. Meilisearch 全文索引、中文分词与高亮结果。
5. 知识库 / 空间 / 文档三级数据权限。
6. 文档分享、公开链接、访问密码与有效期。
7. 文档导入：Markdown、TXT、Word、PDF、HTML、ZIP。
8. 批量导出 Markdown / PDF。
9. API Key、Webhook、开放 API。
10. 最后再接入 Embedding、RAG、语义检索和知识问答，AI 能力保持可关闭、可替换。
