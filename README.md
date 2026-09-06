# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。项目强调专业、克制、工程化和长期可维护性，前后端均按可持续扩展方向建设。

## 当前版本

`v0.11.0`

当前已具备：JWT/RBAC、用户/角色/部门/组织/菜单管理、知识库与层级文档、Markdown 编辑、标签、收藏、最近访问、附件、版本/回滚/Diff、权限感知搜索、Markdown/ZIP 导入导出、公开分享、资源级权限、分享访问审计、异步导出任务、审计日志、主题/语言/水印/字号配置、公共表格/树组件、服务端分页和 GitHub Actions CI。

## 技术栈

- 后端：.NET 10 / ASP.NET Core Web API / DDD + Clean Architecture / EF Core 10 / SQLite / JWT
- 前端：Vue 3 / TypeScript / Vite / Pinia / Vue Router / Axios / Element Plus
- 构建：GitHub Actions，自动执行 .NET Release Build 与 `vue-tsc + vite build`

## v0.11.0 重点更新

### 服务端异步导出任务中心

知识库全量导出不再在 HTTP 请求线程内同步生成文件。

新增：

```text
Sys_ExportTask
ExportTaskWorker
IExportTaskService
ExportTaskService
ExportTasksController
frontend/src/views/DataExchange.vue
frontend/src/api/modules/exchange.ts
```

任务状态：

```text
Pending -> Running -> Completed
                  -> Failed
```

处理流程：

1. 用户选择有访问权限的知识库。
2. `POST /api/export-tasks` 创建 Pending 任务。
3. `ExportTaskWorker` 后台领取任务。
4. 读取知识库文档与 Markdown 正文。
5. 服务端生成 ZIP 文件到 `storage/exports`。
6. 更新任务状态、文件名、完成时间或失败原因。
7. 前端任务中心分页查看并下载已完成文件。

后台 Worker 每次只领取一个待处理任务，避免多个大导出同时争用 SQLite、磁盘和内存资源。服务重启后仍可继续处理数据库中的 Pending 任务。

### Markdown ZIP 批量导入

数据交换页面新增 Markdown ZIP 批量导入：

- 支持 `.zip`
- ZIP 内识别 `.md` / `.markdown`
- 单个 Markdown 文件最大 20 MB
- ZIP 请求默认最大 200 MB
- 非 Markdown 文件自动跳过
- 返回总条目、成功导入数量、跳过数量
- 导入前执行知识库 Editor / Manager 权限校验

接口：

```text
POST /api/content-exchange/knowledge-bases/{knowledgeBaseId}/markdown-zip
```

当前版本按 Markdown 文件逐篇导入，后续可继续增加“根据 ZIP 目录恢复文档父子树”。

### 分享密码与访问审计

分享链接现支持：

- 可选访问密码
- 可选过期时间
- 手动停用
- 访问次数
- 最近访问时间
- 成功/失败访问日志
- IP 地址记录
- User-Agent 记录
- 失败原因记录

分享密码使用：

```text
PBKDF2-SHA256
随机 Salt
120000 次迭代
固定时间比较
```

不会保存明文分享密码。

新增：

```text
Kb_ShareAccessLog
DocumentShareLink.AccessCount
DocumentShareLink.LastAccessAt
frontend/src/components/knowledge/ShareManagementPanel.vue
```

资源管理页面现在可以选择文档后：

- 创建公开分享
- 配置密码
- 配置过期时间
- 复制链接
- 停用链接
- 查看访问次数
- 查看访问日志
- 导出分享链接/访问日志列表

匿名访问统一使用：

```text
POST /api/share/public/{token}/access
```

密码错误、链接停用、链接过期等访问都会写入访问日志。

## 资源权限规则

知识库角色：

```text
Viewer  -> 查看知识库和文档
Editor  -> Viewer + 新建/编辑文档、上传附件、导入内容
Manager -> Editor + 删除、成员管理、文档权限管理、分享管理
```

主要规则：

- 超级管理员拥有全部资源权限。
- 创建知识库后，创建人自动成为 `Manager`。
- 普通用户只看到自己有权限访问的知识库。
- 文档显式权限优先于知识库成员权限。
- 搜索在 EF Core 查询阶段按成员关系过滤。
- Markdown/ZIP 导入执行知识库编辑权限。
- 附件、版本、Diff、分享等接口均执行后端资源权限校验。
- 导出任务创建前验证知识库查看权限。

## 服务端分页

以下页面使用数据库层分页：

- 我的知识库
- 系统用户
- 审计日志
- 全文搜索
- 最近浏览
- 我的收藏
- 导出任务中心

统一契约：

```text
PageQuery
PageResult<T>
```

数据库查询优先使用：

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
- 10 / 20 / 50 / 100 条每页
- CSV 导出
- 勾选数据导出
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

业务页面禁止重复拼接接口 URL，通用表格、分页、列设置和导出逻辑不得在多个页面重复实现。

## 后端开发规范

- 后端解释性注释统一使用中文。
- XML `summary` 使用中文说明类、接口和关键方法职责。
- JWT、PBKDF2、EF Core、HTTP 等标准技术名称保留原名。
- Controller 只处理 HTTP、身份、权限和参数映射。
- 核心业务逻辑进入 Application/Infrastructure 服务层。
- 大数据查询优先数据库分页、过滤和排序。
- 大文件导出优先后台任务，不阻塞请求线程。
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
```

v0.11.0 新增数据库结构：

```text
Sys_ExportTask
Kb_ShareAccessLog
Kb_ShareLink.AccessCount
Kb_ShareLink.LastAccessAt
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
- Markdown ZIP 批量导入
- 服务端异步 ZIP 全量导出
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
- 已预留 Meilisearch / PostgreSQL FTS / OpenSearch

### 界面个性化

- 深色 / 浅色 / 跟随系统
- 简体中文 / English 基础切换
- 字号 12~18px
- 自定义水印
- 紧凑模式

## 主要 API

### 导出任务

```text
POST /api/export-tasks
GET  /api/export-tasks?page=1&pageSize=20
GET  /api/export-tasks/{id}/download
```

### 内容交换

```text
GET  /api/content-exchange/documents/{documentId}/markdown
POST /api/content-exchange/knowledge-bases/{knowledgeBaseId}/markdown
POST /api/content-exchange/knowledge-bases/{knowledgeBaseId}/markdown-zip
GET  /api/content-exchange/versions/{versionId}/diff-current
```

### 分享

```text
POST   /api/share/documents/{documentId}
GET    /api/share/documents/{documentId}
GET    /api/share/documents/{documentId}/access-logs
DELETE /api/share/links/{id}
POST   /api/share/public/{token}/access
```

### 资源权限

```text
GET    /api/access/knowledge-bases/{id}/users?keyword=&take=50
GET    /api/access/knowledge-bases/{id}/members
PUT    /api/access/knowledge-bases/{id}/members
DELETE /api/access/knowledge-bases/{id}/members/{userId}
GET    /api/access/documents/{id}/permissions
PUT    /api/access/documents/{id}/permissions
DELETE /api/access/documents/{id}/permissions/{userId}
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

## CI

仓库内置 `.github/workflows/ci.yml`，每次推送 `main` 或 Pull Request 自动执行：

- .NET 10 restore
- .NET 10 Release build
- npm install
- vue-tsc
- Vite production build

## 下一阶段

1. 导出任务增加重试、取消、过期清理、文件保留策略和并发度配置。
2. Markdown ZIP 导入恢复目录层级，并增加导入任务进度。
3. Word / HTML 导入解析，PDF 先支持文本型 PDF，再单独评估 OCR。
4. Meilisearch 中文全文索引、高亮、异步索引任务与重建索引。
5. 评论、@成员、通知中心。
6. API Key、Webhook、开放 API。
7. 存储抽象 `IFileStorage`，支持 Local / MinIO / S3 / OSS / COS。
8. 最后接入可选 Embedding、RAG、语义检索和知识问答，保持智能能力可关闭、可替换。
