# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。产品强调专业、克制、工程化和长期可维护性，不把 AI 对话框作为视觉中心。

## 当前版本

`v0.7.0`

当前已经具备：JWT/RBAC、系统管理、知识库与层级文档、Markdown 编辑、标签、收藏、最近访问、附件、版本/回滚/Diff、基础全文搜索、Markdown 导入导出、公开分享、资源权限配置、审计日志、EF Core Migration，以及统一的前端列表组件、数据导出、列设置、分页和领域 API 模块。

## 技术栈

- 后端：.NET 10 / ASP.NET Core Web API / DDD + Clean Architecture / EF Core 10 / SQLite / JWT
- 前端：Vue 3 / TypeScript / Vite / Pinia / Vue Router / Axios / Element Plus
- CI：GitHub Actions，自动执行 .NET Release 构建和 Vue/TypeScript 生产构建

## v0.7.0 工程化调整

### 前端公共列表组件

所有主要列表型业务页面统一使用：

```text
frontend/src/components/common/BaseDataTable.vue
```

当前已接入：

- 知识库列表
- 知识中心：搜索 / 最近访问 / 收藏 / 标签
- 系统管理：用户 / 角色 / 部门 / 组织 / 菜单 / 审计日志
- 文档工作区：版本历史 / 附件列表

统一能力：

- 分页
- 每页 10 / 20 / 50 / 100 条
- 页码跳转
- 当前数据 CSV 导出
- UTF-8 BOM，Excel 直接打开中文不乱码
- 自定义显示/隐藏列
- 拖动表头分隔线调整列宽
- 列显示配置本地持久化
- 列宽本地持久化
- 每页条数本地持久化
- 排序列配置
- 固定操作列
- 自定义单元格插槽
- 统一空状态、加载状态、刷新入口

页面头部也提取为：

```text
frontend/src/components/common/PageHeader.vue
```

以后新增列表页面禁止重新复制一套 `el-table + el-pagination + 导出 + 列设置`。

### 前端 API 模块化

基础 Axios 客户端：

```text
frontend/src/api/http.ts
```

按领域拆分：

```text
frontend/src/api/modules/auth.ts
frontend/src/api/modules/system.ts
frontend/src/api/modules/knowledge.ts
frontend/src/api/modules/documents.ts
frontend/src/api/modules/share.ts
```

业务页面和 Pinia Store 应优先调用领域 API 模块，不允许在多个页面重复拼接同一接口地址。

`http.ts` 统一提供：

- `http`：需要 JWT 的业务请求客户端
- `publicHttp`：匿名公共请求客户端
- Bearer Token 自动注入
- 401 统一清理登录状态并跳转登录页

### 前后端统一分页基础

前端公共类型：

```text
frontend/src/types/table.ts
```

后端新增：

```text
KnowledgeBase.Contracts/Common/Paging.cs
KnowledgeBase.Infrastructure/Common/QueryablePagingExtensions.cs
```

统一提供：

```text
PageQuery
PageResult<T>
ToPageResultAsync<T>()
```

当前主要页面在已加载结果上完成分页；后续数据量较大的系统日志、搜索、用户等接口可以直接切换服务端分页，而不需要重新设计分页返回结构。

### 后端中文注释规范

后端代码注释统一使用中文：

- `/// <summary>` 使用中文描述类、接口、领域对象和重要方法职责；
- 复杂权限、数据库迁移、审计、文件处理等逻辑使用中文说明设计意图；
- 禁止新增无意义英文模板注释；
- 类型名、方法名、协议名、标准名等代码标识仍保持英文规范，例如 JWT、PBKDF2、EF Core、HTTP。

本版本新增和重构的后端公共分页、资源权限等代码均按该规范编写。

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

默认开发账号：

```text
admin / Admin123!
```

生产部署必须修改默认密码与 `Jwt:Key`。

### 组织与系统管理

- 用户、角色、部门、组织机构、菜单管理
- 部门/组织父子结构、排序、启停状态
- 菜单支持目录 / 菜单 / 按钮三类节点
- 系统管理接口受 `SUPER_ADMIN` 保护
- 系统管理各 Tab 已统一接入公共数据表格
- 审计日志支持分页、列设置和数据导出

### 知识库与文档

- 知识库 CRUD
- 知识库工作区
- 文档目录树、父子层级
- Markdown 新建、编辑、保存、删除
- `Kb_Document` / `Kb_DocumentContent` 分表
- 目录列表不加载正文，打开文档时按需加载
- 知识中心统一承载搜索、最近访问、收藏和标签
- 知识库列表支持分页、数据导出、自定义列和拖动列宽

### 文档版本与 Diff

- 手动版本快照
- 编辑已有文档时自动创建“编辑前快照”
- 版本列表 / 详情 / 回滚
- 回滚前自动备份当前版本
- Markdown 行级 Diff
- 版本对比当前文档
- 版本历史列表支持分页、导出、列设置、列宽调整
- Diff 当前限制前 2000 行，避免异常大文档造成内存膨胀

### 标签 / 收藏 / 最近访问

- 标签 CRUD、标签颜色
- 文档多标签绑定
- 收藏 / 取消收藏
- 当前用户收藏列表
- 打开文档自动记录最近访问
- 最近访问时间和累计次数
- 知识中心各列表支持统一分页与导出

### 附件

- 附件上传、列表、鉴权下载、删除
- 默认单文件上限 50 MB
- 文件默认保存到 `storage/attachments`
- 元数据保存于 `Kb_Attachment`
- 附件列表支持分页、CSV 导出、自定义列和列宽调整
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
- 匿名分享请求使用独立 `publicHttp` 客户端
- 公共接口不会暴露编辑和管理能力
- 当前版本暂未开放访问密码，密码字段已在数据结构中预留

### 知识库 / 文档数据权限

- `Kb_KnowledgeBaseMember`
- 知识库成员角色：`Viewer / Editor / Manager`
- `Kb_DocumentPermission`
- 单文档用户权限：查看 / 编辑 / 管理
- 超级管理员可通过 `/api/access/*` 维护成员和文档权限

当前阶段完成的是权限数据与管理 API。后续将把该数据正式接入知识库、文档读取/编辑/删除的资源级授权判断。

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

## 前端目录约定

```text
frontend/src
├─ api
│  ├─ http.ts
│  └─ modules
│     ├─ auth.ts
│     ├─ system.ts
│     ├─ knowledge.ts
│     ├─ documents.ts
│     └─ share.ts
├─ components
│  ├─ common
│  │  ├─ BaseDataTable.vue
│  │  └─ PageHeader.vue
│  └─ AppearancePanel.vue
├─ composables
├─ router
├─ stores
├─ types
│  └─ table.ts
├─ utils
│  └─ exportCsv.ts
└─ views
```

新增功能时遵循以下顺序：

1. 先判断是否已有公共组件可以复用；
2. 接口统一放入 `api/modules`，页面禁止重复写相同 URL；
3. 表格页面使用 `BaseDataTable`；
4. 公共类型放 `types`；
5. 通用无状态函数放 `utils`；
6. 跨页面状态才进入 Pinia Store；
7. 页面文件只保留业务编排和本页面特有交互。

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

该迁移兼容历史 SQLite 数据库。升级生产数据前仍建议先备份数据库。

后续新增迁移：

```bash
cd backend/KnowledgeBase.Api
dotnet ef migrations add <MigrationName> \
  --project ../KnowledgeBase.Infrastructure \
  --startup-project .

dotnet ef database update \
  --project ../KnowledgeBase.Infrastructure \
  --startup-project .
```

## 构建与 CI

本地后端：

```bash
cd backend/KnowledgeBase.Api
dotnet restore
dotnet run
```

本地前端：

```bash
cd frontend
npm install
npm run dev
```

生产构建校验：

```bash
dotnet build KnowledgeBase.slnx -c Release
cd frontend
npm run build
```

仓库包含：

```text
.github/workflows/ci.yml
```

每次推送到 `main` 或创建 PR 时自动执行：

- .NET 10 Release 构建
- `vue-tsc` TypeScript 检查
- Vite Production Build

前端 `tsconfig.json` 开启 `skipLibCheck`，用于隔离 Element Plus 等第三方声明文件与当前 TypeScript 版本之间的类型噪音；项目自身代码仍保持 `strict: true`。

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
2. 后端注释统一使用中文，代码标识遵守 .NET 英文命名规范。
3. 文档目录与正文分离，禁止工作区一次加载全部正文。
4. 文档附件、版本、标签按需加载。
5. 数据库升级统一由 Migration 管理。
6. 权限从系统 RBAC 向知识库、文档资源级授权继续下沉。
7. 文件存储、搜索、Embedding、LLM 保持抽象，可独立替换。
8. 前端公共能力先抽组件、类型、工具和 API 模块，再由页面组合。
9. 列表页面禁止重复实现分页、导出和列设置。
10. 关键管理和知识资产写操作纳入统一审计。

## 下一阶段

1. 将用户、日志、搜索等大数据列表正式切换为后端服务端分页。
2. 增加 Excel `.xlsx` 导出和按选中列/选中行导出。
3. 列设置增加拖动排序和列固定配置。
4. 完善部门 / 组织 / 菜单树形编辑公共组件。
5. 将 `Kb_KnowledgeBaseMember` / `Kb_DocumentPermission` 正式接入资源访问授权。
6. 完善知识库成员/文档权限前端管理面板。
7. 分享链接增加访问密码、访问次数和访问日志。
8. Markdown 批量 / ZIP、Word、PDF、HTML 导入导出。
9. Meilisearch 中文全文索引、高亮和异步索引任务。
10. API Key、Webhook、开放 API。
11. 最后再接入 Embedding、RAG、语义检索和知识问答；智能能力保持可关闭、可替换。
