# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。项目强调专业、克制、工程化和长期可维护性，前后端均按可持续扩展方向建设。

## 当前版本

`v0.12.0`

当前已具备：JWT/RBAC、用户/角色/部门/组织/菜单管理、知识库与层级文档、Markdown 编辑、标签、收藏、最近访问、附件、版本/回滚/Diff、权限感知搜索、Markdown/HTML/ZIP 导入导出、公开分享、资源级权限、分享访问审计、异步导入导出任务、审计日志、主题/语言/水印/字号配置、公共表格/树组件、服务端分页和 GitHub Actions CI。

## 技术栈

- 后端：.NET 10 / ASP.NET Core Web API / DDD + Clean Architecture / EF Core 10 / SQLite / JWT
- 前端：Vue 3 / TypeScript / Vite / Pinia / Vue Router / Axios / Element Plus
- 构建：GitHub Actions，自动执行 .NET Release Build 与 `vue-tsc + vite build`

## v0.12.0 重点更新

### 导入/导出任务生命周期

异步任务不再只有创建、完成和失败状态，本版本新增：

```text
Pending   排队中
Running   处理中
Completed 已完成
Failed    失败
Cancelled 已取消
```

导入、导出任务均支持：

- 分页查看任务历史
- Pending 任务取消
- Failed / Cancelled 任务重试
- 按保留天数清理历史任务
- 清理任务时同步删除相关临时文件
- 后端顺序 Worker 处理任务，避免多个大文件任务同时争用 SQLite、磁盘和内存

导出任务接口：

```text
POST   /api/export-tasks
GET    /api/export-tasks?page=1&pageSize=20
POST   /api/export-tasks/{id}/cancel
POST   /api/export-tasks/{id}/retry
DELETE /api/export-tasks/cleanup?olderThanDays=7
GET    /api/export-tasks/{id}/download
```

导入任务接口：

```text
POST   /api/import-tasks/knowledge-bases/{knowledgeBaseId}/zip
GET    /api/import-tasks?page=1&pageSize=20
POST   /api/import-tasks/{id}/cancel
POST   /api/import-tasks/{id}/retry
DELETE /api/import-tasks/cleanup?olderThanDays=7
```

### 异步 ZIP 导入与进度

新增：

```text
Sys_ImportTask
ImportTask
IImportTaskService
ImportTaskService
ImportTaskWorker
ImportTasksController
```

导入任务会记录：

- 总文件数量
- 已处理数量
- 成功导入数量
- 跳过数量
- 开始/完成时间
- 失败原因
- 源文件名称

前端“数据交换”页面现在分为：

```text
导出任务
导入任务
```

并复用 `BaseDataTable` 提供分页、列配置、列宽调整、导出、刷新等公共能力。

### ZIP 父子层级恢复

知识库 ZIP 导出不再把所有 Markdown 文件放在 ZIP 根目录。

导出结构示例：

```text
产品手册-<id>.md
产品手册-<id>/
├─ 安装说明-<id>.md
└─ 常见问题-<id>.md
```

再次导入时会根据 ZIP 路径恢复 `ParentId`，从而尽可能保持原知识库的文档树结构。

文件名中的内部 ID 只用于恢复层级，导入后的文档标题会自动去掉该后缀。

### HTML 导入

新增：

```text
POST /api/content-exchange/knowledge-bases/{knowledgeBaseId}/html
```

支持 `.html / .htm`，后端会：

- 去除 script/style
- 转换 h1/h2/h3 为 Markdown 标题
- 转换列表项
- 处理段落和换行
- HTML Entity 解码
- 创建标准 Markdown 文档

目前定位为基础 HTML 文本导入；复杂网页样式、表格、图片资源后续继续增强。

## v0.11.0 能力

### 安全分享

分享链接支持：

- 可选访问密码
- 可选过期时间
- 手动停用
- 访问次数
- 最近访问时间
- IP / User-Agent
- 成功/失败访问日志

分享密码使用 PBKDF2-SHA256、随机 Salt、120000 次迭代和固定时间比较，不保存明文。

### 服务端异步 ZIP 导出

知识库全量导出通过 `ExportTaskWorker` 后台执行，生成文件保存于：

```text
storage/exports
```

任务和文件均受当前用户身份及资源权限约束。

## 资源权限规则

```text
Viewer  -> 查看知识库和文档
Editor  -> Viewer + 新建/编辑文档、上传附件、导入内容
Manager -> Editor + 删除、成员管理、文档权限管理、分享管理
```

主要规则：

- 超级管理员拥有全部资源权限。
- 创建知识库后，创建人自动成为 Manager。
- 普通用户只看到自己有权限访问的知识库。
- 文档显式权限优先于知识库成员权限。
- 搜索在 EF Core 查询阶段按成员关系过滤。
- Markdown / HTML / ZIP 导入执行知识库编辑权限。
- 附件、版本、Diff、分享等接口执行后端资源权限校验。
- 导出任务创建前验证知识库查看权限。

## 服务端分页

目前使用数据库层分页的主要页面包括：

- 我的知识库
- 系统用户
- 审计日志
- 全文搜索
- 最近浏览
- 我的收藏
- 导出任务
- 导入任务

统一契约：

```text
PageQuery
PageResult<T>
```

数据库查询优先：

```text
Where -> Count -> OrderBy -> Skip -> Take
```

## 前端公共组件与接口规范

公共组件：

```text
frontend/src/components/common/
├─ BaseDataTable.vue
├─ BaseTreeManager.vue
└─ PageHeader.vue

frontend/src/components/knowledge/
├─ ResourcePermissionPanel.vue
└─ ShareManagementPanel.vue
```

`BaseDataTable` 统一支持：

- 本地分页 / 服务端分页
- 每页条数配置和记忆
- CSV 导出 / 勾选导出
- 显示/隐藏列
- 拖动调整列宽
- 列顺序调整
- 列配置持久化
- 固定操作列
- 排序
- 加载/空状态
- 自定义单元格与操作区

API 集中在：

```text
frontend/src/api/modules/
├─ auth.ts
├─ system.ts
├─ knowledge.ts
├─ documents.ts
├─ access.ts
├─ share.ts
└─ exchange.ts
```

业务页面禁止重复拼接接口 URL，通用表格、分页、列设置、导出和任务操作逻辑不得在多个页面重复实现。

## 后端开发规范

- 后端解释性注释统一使用中文。
- XML `summary` 使用中文说明类、接口和关键方法职责。
- JWT、PBKDF2、EF Core、HTTP 等标准技术名称保留原名。
- Controller 只处理 HTTP、身份、权限和参数映射。
- 核心业务逻辑进入 Application / Infrastructure 服务层。
- 大数据查询优先数据库分页、过滤和排序。
- 大文件导入导出优先后台任务，不阻塞请求线程。
- 数据库升级统一由 EF Core Migration 管理。
- 权限判断必须在后端执行，前端隐藏按钮不是安全边界。

## EF Core Migration

程序启动时执行：

```csharp
await db.Database.MigrateAsync();
```

现有迁移：

```text
202609060600_BaselineV060
202609061100_V011ExportAndShareAudit
202609061200_V012ImportTasks
```

v0.12.0 新增：

```text
Sys_ImportTask
```

生产环境升级前请先备份数据库。

## 主要功能

### 身份认证与系统管理

- JWT 登录 / Bearer Token
- 401 自动退出
- PBKDF2-SHA256 密码存储
- 用户 CRUD、启停用、密码维护
- 角色 CRUD、角色菜单授权
- 部门树 / 组织机构树
- 菜单、按钮权限
- 审计日志

默认开发账号：

```text
admin / Admin123!
```

生产环境必须修改默认密码和 `Jwt:Key`。

### 知识库与文档

- 知识库 CRUD
- 权限感知知识库分页
- 文档目录树 / 父子层级
- Markdown 编辑
- 正文与目录分表
- 标签、收藏、最近访问
- 版本、Diff、回滚
- 附件
- Markdown 单文档导入导出
- HTML 导入
- Markdown ZIP 层级导入
- 异步 ZIP 全量导出
- Viewer / Editor / Manager
- 文档显式权限

### 分享

- 分享链接创建/停用
- 可选过期时间
- 可选访问密码
- PBKDF2 分享密码存储
- 访问次数
- 最近访问时间
- 成功/失败访问日志
- IP / User-Agent 审计
- 匿名只读分享页面

### 搜索

- 标题 + Markdown 正文搜索
- 服务端分页
- 资源权限隔离
- 按知识库过滤
- 命中摘要
- 当前 SQLite 查询
- 后续替换 Meilisearch / PostgreSQL FTS / OpenSearch

### 界面个性化

- 深色 / 浅色 / 跟随系统
- 简体中文 / English 基础切换
- 字号 12~18px
- 自定义水印
- 紧凑模式

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

## CI

仓库内置 `.github/workflows/ci.yml`，每次推送 `main` 或 Pull Request 自动执行：

- .NET 10 restore
- .NET 10 Release build
- npm install
- vue-tsc
- Vite production build

## 下一阶段

1. 抽象搜索接口 `IKnowledgeSearchService`，接入 Meilisearch 中文全文索引、高亮、重建索引任务。
2. 增加 Word `.docx` 导入；PDF 优先支持文本型 PDF，再单独评估 OCR。
3. 导入导出任务增加并发度配置、自动过期清理和磁盘空间保护。
4. 评论、@成员、通知中心。
5. API Key、Webhook、开放 API。
6. 存储抽象 `IFileStorage`，支持 Local / MinIO / S3 / OSS / COS。
7. 最后接入可选 Embedding、RAG、语义检索和知识问答，保持智能能力可关闭、可替换。
