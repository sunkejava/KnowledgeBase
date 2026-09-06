# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。项目强调专业、克制、工程化、权限安全和长期可维护性，前后端均按可持续扩展方向建设。

## 当前版本

`v0.13.0`

当前已具备：JWT/RBAC、用户/角色/部门/组织/菜单管理、知识库与层级文档、Markdown 编辑、标签、收藏、最近访问、附件、版本/回滚/Diff、资源级权限、公开分享与访问审计、异步导入导出任务、Markdown/HTML/DOCX/ZIP 导入、可切换全文搜索、搜索索引任务、审计日志、主题/语言/水印/字号配置、公共表格/树组件、服务端分页、Docker 部署和 GitHub Actions CI。

## 技术栈

- 后端：.NET 10 / ASP.NET Core Web API / DDD + Clean Architecture / EF Core 10 / SQLite / JWT
- 前端：Vue 3 / TypeScript / Vite / Pinia / Vue Router / Axios / Element Plus
- 搜索：默认 SQLite；可选 Meilisearch
- 部署：Docker Compose / Nginx
- 构建：GitHub Actions，自动执行 NuGet 安全检查、.NET Release Build 与 `vue-tsc + vite build`

---

## v0.13.0 重点更新

### 1. .NET 10 稳定版与依赖安全升级

项目不再使用早期 `.NET 10 preview.7` 包，核心依赖统一升级到稳定版：

```text
Microsoft.AspNetCore.Authentication.JwtBearer 10.0.11
Microsoft.AspNetCore.OpenApi                  10.0.11
Microsoft.EntityFrameworkCore.Sqlite         10.0.11
Microsoft.EntityFrameworkCore.Design         10.0.11
System.Security.Cryptography.Xml             10.0.11
```

升级前 CI 曾报告 `Microsoft.OpenApi`、`SQLitePCLRaw.lib.e_sqlite3`、`Microsoft.Build.*`、`System.Security.Cryptography.Xml` 等高危依赖告警；升级后后端 CI 已达到：

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

CI 同时升级为：

```text
actions/checkout@v7
actions/setup-dotnet@v5
actions/setup-node@v6
```

并增加传递依赖安全检查：

```bash
dotnet list KnowledgeBase.slnx package --vulnerable --include-transitive
```

### 2. 统一全文搜索抽象

新增：

```text
IKnowledgeSearchService
├─ SqliteKnowledgeSearchService
└─ MeilisearchKnowledgeSearchService
```

业务 Controller 不再直接依赖 SQLite 搜索实现。

统一接口负责：

```text
SearchAsync
UpsertDocumentAsync
DeleteDocumentAsync
RebuildIndexAsync
ProviderName
```

原 `KnowledgeAssetService` 中重复的全文搜索逻辑已经移除，标签、收藏、最近访问、版本和搜索职责正式拆开。

### 3. 默认 SQLite / 可选 Meilisearch

默认配置：

```json
"Search": {
  "Provider": "sqlite",
  "Meilisearch": {
    "Endpoint": "http://127.0.0.1:7700",
    "ApiKey": "",
    "IndexName": "knowledge_documents"
  }
}
```

默认 `sqlite` 模式：

- 无需部署额外组件
- 直接读取文档业务表
- 支持标题 + Markdown 正文搜索
- 服务端分页
- 知识库权限过滤

切换：

```text
Search:Provider = meilisearch
```

后启用外部 Meilisearch。

Meilisearch 索引字段：

```text
id
knowledgeBaseId
title
markdown
updatedAt
```

其中 `knowledgeBaseId` 配置为 filterable attribute，普通用户搜索时先从数据库解析允许访问的知识库，再将允许范围作为搜索过滤条件发送给 Meilisearch。

### 4. Meilisearch 增量索引

文档发生以下操作时：

```text
创建
修改
删除
```

会自动执行对应索引：

```text
UpsertDocumentAsync
DeleteDocumentAsync
```

搜索索引属于派生数据，因此 Meilisearch 临时不可用时：

- 文档保存不会失败
- 主业务数据仍正常提交
- 后端记录 Warning 日志
- 管理员可进入“搜索管理”执行全量重建

避免搜索基础设施故障拖垮知识库核心写入链路。

### 5. 搜索索引任务中心

新增：

```text
Sys_SearchIndexTask
SearchIndexTask
ISearchIndexTaskService
SearchIndexTaskService
SearchIndexTaskWorker
SearchManagementController
frontend/src/views/SearchManagement.vue
frontend/src/api/modules/search.ts
```

索引任务状态：

```text
Pending
Running
Completed
Failed
```

支持：

- 查看当前 Search Provider
- 查看外部搜索服务地址
- 创建全量索引重建任务
- 分页查看任务历史
- 查看失败原因
- 失败任务重试
- 清理历史任务

接口：

```text
GET    /api/search-management/status
POST   /api/search-management/rebuild
GET    /api/search-management/tasks?page=1&pageSize=20
POST   /api/search-management/tasks/{id}/retry
DELETE /api/search-management/tasks?retentionDays=30
```

以上管理接口仅允许 `SUPER_ADMIN` 调用。

### 6. Meilisearch 任务真实完成确认

Meilisearch 写索引本身是异步任务。本系统不会在收到 `taskUid` 后立即把本地任务标记完成，而是继续轮询：

```text
GET /tasks/{taskUid}
```

直到：

```text
succeeded
```

才将 `Sys_SearchIndexTask` 标记为 `Completed`。

如果 Meilisearch 返回：

```text
failed
canceled
```

本地索引任务会进入 `Failed` 并记录错误信息。

索引首次不存在时会自动创建并配置：

```text
primaryKey = id
searchableAttributes = title, markdown
filterableAttributes = knowledgeBaseId
```

### 7. DOCX 导入

新增：

```text
POST /api/content-exchange/knowledge-bases/{knowledgeBaseId}/docx
```

支持 `.docx`，最大 50 MB。

当前实现直接读取 Office Open XML 包中的：

```text
word/document.xml
```

无需安装 Microsoft Office，也不依赖桌面 COM 组件。

当前支持：

- 普通段落文本
- Heading1 ~ Heading6
- 标题转换为 Markdown `#` ~ `######`
- 段落换行
- 继续执行知识库 Editor / Manager 权限校验

当前暂不保留：

- 图片
- 表格结构
- 超链接样式
- 复杂编号列表
- 公式

后续可继续增强 DOCX 资源与复杂结构解析。

### 8. Docker 生产化调整

后端镜像已经从：

```text
10.0-preview
```

切换为稳定：

```text
mcr.microsoft.com/dotnet/sdk:10.0
mcr.microsoft.com/dotnet/aspnet:10.0
```

前端新增多阶段生产镜像：

```text
Node.js Build
    ↓
Vite dist
    ↓
Nginx Runtime
```

不再通过 Docker 直接运行 Vite 开发服务器。

同时修复 SQLite 数据卷：

```text
Data Source=/app/data/knowledgebase.db
kb-data:/app/data
```

确保容器重建后数据库真实持久化。

附件、导入导出文件统一挂载：

```text
kb-storage:/app/storage
```

### 9. Docker 可选 Meilisearch

`docker-compose.yml` 已提供可选搜索 profile，当前镜像：

```text
getmeili/meilisearch:v1.53.1
```

默认 SQLite 启动：

```bash
docker compose up -d
```

启用 Meilisearch：

```bash
cp .env.example .env
```

修改：

```text
SEARCH_PROVIDER=meilisearch
MEILISEARCH_API_KEY=<强随机密钥>
JWT_KEY=<强随机密钥>
```

然后：

```bash
docker compose --profile search up -d
```

首次启用后进入“搜索管理”执行一次全量索引重建。

---

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
- SQLite 搜索在 EF Core 查询阶段执行成员过滤。
- Meilisearch 搜索通过 `knowledgeBaseId` filter 执行允许范围过滤。
- Markdown / HTML / DOCX / ZIP 导入执行知识库编辑权限。
- 附件、版本、Diff、分享等接口执行后端资源权限校验。
- 前端按钮隐藏不是权限安全边界。

---

## 异步任务体系

目前已具备：

```text
导出任务      Sys_ExportTask
导入任务      Sys_ImportTask
搜索索引任务  Sys_SearchIndexTask
```

导入/导出任务支持：

- Pending / Running / Completed / Failed / Cancelled
- 分页
- 取消
- 重试
- 失败原因
- 历史清理
- 临时文件清理

索引任务支持：

- Pending / Running / Completed / Failed
- 后台串行执行
- 重试
- 历史清理

---

## 服务端分页

当前主要服务端分页页面：

- 我的知识库
- 系统用户
- 审计日志
- 全文搜索
- 最近浏览
- 我的收藏
- 导出任务
- 导入任务
- 搜索索引任务

统一契约：

```text
PageQuery
PageResult<T>
```

数据库查询优先：

```text
Where -> Count -> OrderBy -> Skip -> Take
```

---

## 前端公共组件规范

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
- 拖动列宽
- 列顺序调整
- 列配置持久化
- 固定操作列
- 排序
- Loading / Empty
- 自定义单元格和操作区

业务页面禁止自行复制一套表格、分页、列设置和导出逻辑。

API 统一集中于：

```text
frontend/src/api/modules/
├─ auth.ts
├─ system.ts
├─ knowledge.ts
├─ documents.ts
├─ access.ts
├─ share.ts
├─ exchange.ts
└─ search.ts
```

业务页面禁止散落拼接接口 URL。

---

## 后端开发规范

- 后端解释性注释统一使用中文。
- XML `summary` 使用中文说明类、接口和关键方法职责。
- JWT、PBKDF2、EF Core、HTTP、Meilisearch 等标准技术名称保留原名。
- Controller 只负责 HTTP、身份、权限和参数映射。
- 核心业务逻辑进入 Application / Infrastructure 服务层。
- 搜索通过 `IKnowledgeSearchService` 抽象，不允许业务页面/Controller 绑定具体搜索引擎。
- 大数据查询优先数据库分页、过滤和排序。
- 大文件导入导出优先后台任务。
- 搜索索引、导出文件等派生数据故障不能无条件拖垮核心文档写入。
- 数据库升级统一使用 EF Core Migration。
- 权限判断必须在后端执行。

---

## EF Core Migration

程序启动时执行：

```csharp
await db.Database.MigrateAsync();
```

当前迁移：

```text
202609060600_BaselineV060
202609061100_V011ExportAndShareAudit
202609061200_V012ImportTasks
202609061300_V013SearchIndexTasks
```

v0.13.0 新增：

```text
Sys_SearchIndexTask
```

生产升级前必须备份数据库。

---

## 主要功能

### 身份认证与系统管理

- JWT 登录 / Bearer Token
- 401 自动退出
- PBKDF2-SHA256 密码存储
- 用户 CRUD / 启停用 / 密码维护
- 角色 CRUD / 角色菜单授权
- 部门树 / 组织机构树
- 菜单 / 按钮权限
- 审计日志

默认开发账号：

```text
admin / Admin123!
```

生产环境必须修改默认密码和 `Jwt:Key`。

### 知识库与文档

- 知识库 CRUD
- 权限感知分页
- 文档父子目录树
- Markdown 编辑
- 正文与目录分表
- 标签 / 收藏 / 最近访问
- 版本 / Diff / 回滚
- 附件
- Markdown 导入导出
- HTML 导入
- DOCX 导入
- Markdown ZIP 层级导入
- 异步 ZIP 导入导出
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

- SQLite 默认全文搜索
- 可选 Meilisearch
- Provider 抽象
- 服务端分页
- 资源权限隔离
- 按知识库过滤
- 文档增量索引
- 全量索引重建任务
- 索引失败重试

### 界面个性化

- 深色 / 浅色 / 跟随系统
- 简体中文 / English 基础切换
- 字号 12~18px
- 自定义水印
- 紧凑模式

---

## 本地启动

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

---

## CI

仓库内置：

```text
.github/workflows/ci.yml
```

每次 push `main` 或 Pull Request 自动执行：

```text
NuGet restore
NuGet vulnerable package check
.NET 10 Release Build
npm install
vue-tsc
Vite production build
```

---

## 下一阶段

1. DOCX 增强：表格、图片、超链接、列表和附件资源导入。
2. PDF 文本型文档导入；扫描型 PDF 单独评估 OCR，不把 OCR 强绑进核心服务。
3. Meilisearch 中文分词/同义词/停用词/搜索高亮与相关性配置。
4. 搜索索引任务增加文档数、已索引数、耗时和进度。
5. 存储抽象 `IFileStorage`，支持 Local / MinIO / S3 / OSS / COS。
6. 评论、@成员、通知中心。
7. API Key、Webhook 和开放 API。
8. 最后接入可选 Embedding、RAG、语义检索和知识问答，保持智能能力可关闭、可替换。
