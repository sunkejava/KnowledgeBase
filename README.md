# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。产品强调专业、克制、工程化和长期可维护性，不把 AI 对话框作为视觉中心。

## 当前版本

`v0.8.0`

当前已经具备 JWT/RBAC、系统管理、知识库与层级文档、Markdown 编辑、标签、收藏、最近访问、附件、版本/回滚/Diff、基础全文搜索、Markdown 导入导出、公开分享、资源权限配置、审计日志、主题/语言/水印/字号等界面能力，并在 v0.7/v0.8 完成前端公共组件、接口模块化和后台服务端分页基础建设。

## 技术栈

- 后端：.NET 10 / ASP.NET Core Web API / DDD + Clean Architecture / EF Core 10 / SQLite / JWT
- 前端：Vue 3 / TypeScript / Vite / Pinia / Vue Router / Axios / Element Plus
- 构建：GitHub Actions，自动执行 .NET Release Build 与 `vue-tsc + vite build`

## v0.8.0 新增

### 服务端分页

系统管理开始从“前端拿全量数据再分页”切换为真正服务端分页：

- 用户列表：数据库层 `Count + Skip + Take`
- 审计日志：数据库层 `Count + Skip + Take`
- 支持 `page / pageSize / keyword`
- 最大单页限制 200 条
- 用户角色关系只加载当前页需要的数据，不再全量加载用户和关系表
- 前端公共表格同时支持本地分页和服务端分页模式

通用契约：

```text
KnowledgeBase.Contracts/Common/Paging.cs
PageQuery
PageResult<T>
```

### 系统管理树形 CRUD

部门、组织机构、菜单权限不再只是展示列表，现已支持真实维护：

- 新增根节点
- 新增子节点
- 编辑
- 删除
- 排序
- 启用 / 停用
- 父节点选择
- 菜单目录 / 菜单 / 按钮节点类型
- 路由、权限标识和图标配置

公共树组件：

```text
frontend/src/components/common/BaseTreeManager.vue
```

### 表格能力增强

`BaseDataTable.vue` 当前统一支持：

- 本地分页
- 服务端分页
- 10 / 20 / 50 / 100 条每页
- 每页条数记忆
- CSV 导出
- 勾选行导出
- 服务端分页时导出当前页
- 自定义显示 / 隐藏列
- 拖动表头调整列宽
- 调整列顺序
- 列配置本地持久化
- 恢复默认列配置
- 固定操作列
- 排序
- 加载状态
- 空状态
- 统一刷新入口
- 自定义单元格与操作区

## 已实现功能

### 身份认证与权限

- JWT 登录
- Bearer Token 自动注入
- 401 自动退出
- PBKDF2-SHA256 密码哈希
- 用户 CRUD、启停用、密码维护
- 角色 CRUD、用户角色分配
- 菜单、路由、按钮权限标识
- 角色菜单授权
- `/api/system/profile` 当前用户权限画像
- 前端 Permission Store
- 默认超级管理员保护

默认开发账号：

```text
admin / Admin123!
```

生产部署必须修改默认密码与 `Jwt:Key`。

### 系统管理

- 用户管理
- 角色管理
- 部门树管理
- 组织机构树管理
- 菜单与按钮权限管理
- 审计日志
- 用户、审计日志服务端分页
- 列表查询、分页、导出、列配置

### 知识库与文档

- 知识库 CRUD
- 知识库工作区
- 文档目录树、父子层级
- Markdown 新建、编辑、保存、删除
- `Kb_Document` / `Kb_DocumentContent` 分表
- 目录列表不加载正文
- 打开文档后按需加载正文
- 知识中心统一承载搜索、最近访问、收藏和标签

### 文档版本与 Diff

- 手动版本快照
- 编辑已有文档前自动创建快照
- 版本列表 / 详情 / 回滚
- 回滚前自动备份当前版本
- Markdown 行级 Diff
- 版本对比当前文档
- Diff 当前限制前 2000 行

### 标签 / 收藏 / 最近访问

- 标签 CRUD、标签颜色
- 文档多标签绑定
- 收藏 / 取消收藏
- 我的收藏
- 最近访问时间
- 累计访问次数

### 附件

- 上传、列表、鉴权下载、删除
- 默认单文件上限 50 MB
- 默认目录 `storage/attachments`
- 元数据保存于 `Kb_Attachment`
- 后续可替换 MinIO / S3 / OSS / COS

### 搜索

- 标题与 Markdown 正文搜索
- 按知识库过滤
- 命中摘要
- 当前 SQLite 基础查询
- 已预留 Meilisearch / PostgreSQL FTS / OpenSearch 替换入口

### Markdown 导入导出

- 单文档导出 `.md`
- `.md / .markdown` 导入知识库
- 可作为当前文档子文档导入
- 单文件上限 20 MB

### 文档分享

- 创建分享链接
- 分享链接列表 / 停用 API
- 可配置过期时间
- `/share/{token}` 匿名只读页面
- 公共请求使用独立 `publicHttp`
- 数据结构已预留访问密码

### 资源权限

- `Kb_KnowledgeBaseMember`
- 知识库成员角色：`Viewer / Editor / Manager`
- `Kb_DocumentPermission`
- 文档用户级查看 / 编辑 / 管理权限
- `/api/access/*` 权限维护 API

当前已完成权限数据模型与管理接口，后续继续把权限判断下沉到知识库、文档、附件、版本、分享等资源访问链路。

### 界面个性化

- 深色 / 浅色 / 跟随系统
- 简体中文 / English 基础切换
- 字号 12~18px
- 自定义水印
- 紧凑模式
- 本地即时持久化
- 后端账号偏好接口

## 前端工程规范

### 公共组件

可复用控件统一进入：

```text
frontend/src/components/common/
├─ BaseDataTable.vue
├─ BaseTreeManager.vue
└─ PageHeader.vue
```

业务页面禁止重复实现通用分页器、列设置、导出逻辑和树形列表基础行为。

### API 模块化

```text
frontend/src/api/
├─ http.ts
└─ modules/
   ├─ auth.ts
   ├─ system.ts
   ├─ knowledge.ts
   ├─ documents.ts
   └─ share.ts
```

页面与 Store 应通过领域 API 模块调用接口，避免在多个页面散落并重复拼接 URL。

`http` 用于需要登录的接口；`publicHttp` 用于匿名分享等公开接口。

### 列表页面要求

新增列表类页面默认必须考虑：

1. 查询条件
2. 分页
3. 每页条数
4. 导出
5. 勾选导出
6. 显示 / 隐藏列
7. 列宽调整
8. 列顺序调整
9. 加载与空状态
10. 数据量较大时使用服务端分页

## 后端开发规范

- 后端解释性注释统一使用中文
- XML `summary` 使用中文说明类、接口和关键方法职责
- JWT、PBKDF2、EF Core、HTTP 等标准技术名词保持原名
- Controller 只处理 HTTP 协议和参数映射，不承载核心业务逻辑
- 数据库查询优先在数据库层完成筛选、分页和排序
- 禁止为了展示单页数据先加载整张大表到内存
- 文档目录、正文、附件、版本、标签继续按需加载
- 数据库升级统一由 EF Core Migration 管理

## EF Core Migration

从 v0.6.0 开始使用：

```csharp
await db.Database.MigrateAsync();
```

首个基线迁移：

```text
202609060600_BaselineV060
```

新增迁移：

```bash
cd backend/KnowledgeBase.Api

dotnet ef migrations add <MigrationName> \
  --project ../KnowledgeBase.Infrastructure \
  --startup-project .

dotnet ef database update \
  --project ../KnowledgeBase.Infrastructure \
  --startup-project .
```

生产环境升级前请先备份数据库。

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

生产构建校验：

```bash
cd frontend
npm run build

cd ../backend
dotnet build KnowledgeBase.Api/KnowledgeBase.Api.csproj -c Release
```

默认 SQLite 数据库：`knowledgebase.db`。

## 主要 API

### 认证 / 系统

```text
POST   /api/auth/login
GET    /api/system/profile
GET    /api/system/users?page=1&pageSize=20&keyword=
POST   /api/system/users
PUT    /api/system/users/{id}
DELETE /api/system/users/{id}
GET    /api/system/roles
GET|POST|PUT|DELETE /api/system/departments
GET|POST|PUT|DELETE /api/system/organizations
GET|POST|PUT|DELETE /api/system/menus
POST   /api/system/change-password
GET    /api/system/audit-logs?page=1&pageSize=20&keyword=
```

### 知识库 / 文档

```text
GET|POST|PUT|DELETE /api/knowledge-bases
GET|POST|PUT|DELETE /api/documents
```

### 知识资产

```text
GET|POST|PUT|DELETE /api/knowledge-assets/tags
GET|PUT /api/knowledge-assets/documents/{id}/tags
POST /api/knowledge-assets/documents/{id}/favorite
GET  /api/knowledge-assets/favorites
POST /api/knowledge-assets/documents/{id}/recent
GET  /api/knowledge-assets/recent
POST /api/knowledge-assets/documents/{id}/versions
GET  /api/knowledge-assets/documents/{id}/versions
POST /api/knowledge-assets/versions/{versionId}/restore
GET  /api/knowledge-assets/search
```

### 导入 / 导出 / Diff

```text
GET  /api/content-exchange/documents/{id}/markdown
POST /api/content-exchange/knowledge-bases/{knowledgeBaseId}/markdown
GET  /api/content-exchange/versions/{versionId}/diff-current
```

### 分享与资源权限

```text
POST   /api/share/documents/{documentId}
GET    /api/share/documents/{documentId}
DELETE /api/share/links/{id}
GET    /api/share/public/{token}

GET|PUT /api/access/knowledge-bases/{id}/members
DELETE  /api/access/knowledge-bases/{id}/members/{userId}
GET|PUT /api/access/documents/{id}/permissions
DELETE  /api/access/documents/{id}/permissions/{userId}
```

## CI

仓库内置：

```text
.github/workflows/ci.yml
```

每次推送到 `main` 或创建 Pull Request 时自动执行：

- `.NET 10 restore`
- `.NET 10 Release build`
- `npm install`
- `vue-tsc`
- `vite build`

`tsconfig.json` 保持 `strict: true`，同时开启 `skipLibCheck` 隔离 Element Plus 等第三方声明文件的类型兼容噪音，项目自身 TypeScript 代码仍严格检查。

## 下一阶段

1. 知识中心搜索、收藏、最近访问切换为服务端分页。
2. 知识库列表切换服务端分页，并增加排序字段白名单。
3. 增加服务端 CSV / Excel 全量导出任务，避免只能导出当前页。
4. 将资源权限真正接入知识库、文档、附件、版本和分享读取/写入链路。
5. 增加知识库成员与文档权限可视化管理面板。
6. 分享链接增加密码、访问次数和访问日志。
7. Markdown ZIP 批量导入导出，继续支持 Word / PDF / HTML 导入。
8. Meilisearch 中文全文索引、高亮和异步索引任务。
9. 评论、@成员、通知中心。
10. API Key、Webhook、开放 API。
11. 最后接入可选 Embedding、RAG、语义检索和知识问答，保持智能能力可关闭、可替换。
