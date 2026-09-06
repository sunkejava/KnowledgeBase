# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。项目强调专业、克制、工程化和长期可维护性，前后端均按可持续扩展方向建设。

## 当前版本

`v0.10.0`

当前已具备：JWT/RBAC、用户/角色/部门/组织/菜单管理、知识库与层级文档、Markdown 编辑、标签、收藏、最近访问、附件、版本/回滚/Diff、基础全文搜索、Markdown 导入导出、公开分享、资源级权限、审计日志、主题/语言/水印/字号配置、公共表格/树组件、服务端分页和 GitHub Actions CI。

## 技术栈

- 后端：.NET 10 / ASP.NET Core Web API / DDD + Clean Architecture / EF Core 10 / SQLite / JWT
- 前端：Vue 3 / TypeScript / Vite / Pinia / Vue Router / Axios / Element Plus
- 构建：GitHub Actions，自动执行 .NET Release Build 与 `vue-tsc + vite build`

## v0.10.0 重点更新

### 资源权限覆盖继续下沉

在 v0.9.0 已完成知识库、文档、搜索、标签、收藏、最近访问和版本权限基础上，本版本继续覆盖：

- 附件列表：需要文档查看权限
- 附件下载：需要文档查看权限
- 附件上传：需要文档编辑权限
- 附件删除：需要文档编辑权限
- Markdown 导出：需要文档查看权限
- Markdown 导入知识库：需要知识库 Editor / Manager 权限
- 导入为子文档：同时校验父文档编辑权限
- 版本 Diff：需要目标文档查看权限
- 创建分享链接：需要文档管理权限
- 查看分享链接列表：需要文档管理权限
- 停用分享链接：需要文档管理权限
- 匿名分享页面仍只通过有效分享 Token 读取

权限安全边界在后端，不依赖前端按钮隐藏。

### 权限可视化管理

新增：

```text
frontend/src/views/ResourceAccess.vue
frontend/src/components/knowledge/ResourcePermissionPanel.vue
frontend/src/api/modules/access.ts
```

知识库列表增加“权限”入口。

权限管理页面支持：

- 查看知识库成员
- 搜索可授权用户
- 添加成员
- 修改成员角色
- 移除成员
- Viewer / Editor / Manager 角色设置
- 选择具体文档
- 查看文档显式权限
- 添加 / 修改文档查看、编辑、管理权限
- 移除文档显式权限
- 成员列表和权限列表复用 `BaseDataTable`
- 列配置、分页、勾选、导出能力继续复用公共组件

权限候选用户接口只返回最小必要信息：用户 ID、用户名、显示名，不直接开放完整系统用户管理数据。

## 资源权限规则

### 知识库角色

```text
Viewer  -> 查看知识库和文档
Editor  -> Viewer + 新建/编辑文档、上传附件、导入 Markdown
Manager -> Editor + 删除、成员管理、文档权限管理、分享管理
```

主要规则：

- 超级管理员拥有全部资源权限。
- 创建知识库后，创建人自动成为 `Manager`。
- 普通用户只看到自己有权限访问的知识库。
- 文档显式权限优先于知识库成员权限。
- 搜索直接在 EF Core 查询阶段按成员关系过滤。
- 权限维护接口允许超级管理员或对应资源 Manager 调用。

## 服务端分页

以下页面已经使用数据库层分页：

- 我的知识库
- 系统用户
- 审计日志
- 全文搜索
- 最近浏览
- 我的收藏

统一契约：

```text
PageQuery
PageResult<T>
```

数据库查询统一使用：

```text
Where -> Count -> OrderBy -> Skip -> Take
```

最大单页限制为 200 条。

## 前端公共组件

```text
frontend/src/components/common/
├─ BaseDataTable.vue
├─ BaseTreeManager.vue
└─ PageHeader.vue
```

`BaseDataTable` 统一支持：

- 本地分页 / 服务端分页
- 每页条数配置和记忆
- CSV 导出
- 勾选数据导出
- 自定义显示/隐藏列
- 拖动调整列宽
- 列顺序调整
- 列配置持久化
- 固定操作列
- 排序
- 加载状态 / 空状态
- 自定义单元格 / 操作区

`BaseTreeManager` 用于部门、组织机构、菜单等层级数据管理。

## 前端 API 规范

接口统一集中在：

```text
frontend/src/api/
├─ http.ts
└─ modules/
   ├─ auth.ts
   ├─ system.ts
   ├─ knowledge.ts
   ├─ documents.ts
   ├─ access.ts
   └─ share.ts
```

业务页面禁止重复拼接接口 URL。需要登录的请求使用 `http`，匿名分享等公开接口使用 `publicHttp`。

## 后端开发规范

- 后端解释性注释统一使用中文。
- XML `summary` 使用中文说明类、接口和关键方法职责。
- JWT、PBKDF2、EF Core、HTTP 等标准技术名称保留原名。
- Controller 只处理协议、身份和参数映射。
- 权限判断在后端执行。
- 数据库筛选、权限过滤、分页、排序优先在数据库层完成。
- 禁止为展示单页数据先读取整表。
- 文档目录、正文、附件、版本、标签按需加载。
- 数据库升级统一由 EF Core Migration 管理。

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
- 系统管理服务端分页

默认开发账号：

```text
admin / Admin123!
```

生产环境必须修改默认密码和 `Jwt:Key`。

### 知识库与文档

- 知识库 CRUD
- 权限感知知识库分页
- 文档树 / 父子层级
- Markdown 新建、编辑、保存、删除
- `Kb_Document` / `Kb_DocumentContent` 分表
- 目录不加载正文
- 文档正文按需读取
- 标签、收藏、最近访问
- 版本快照、Diff、回滚
- 附件上传下载
- Markdown 导入导出
- 分享链接

### 搜索

- 标题 + Markdown 正文搜索
- 服务端分页
- 权限过滤
- 按知识库过滤
- 命中摘要
- 当前使用 SQLite 查询
- 已预留 Meilisearch / PostgreSQL FTS / OpenSearch 替换入口

### 界面个性化

- 深色 / 浅色 / 跟随系统
- 简体中文 / English 基础切换
- 字号 12~18px
- 自定义水印
- 紧凑模式
- 浏览器持久化
- 后端账号偏好接口

## EF Core Migration

从 v0.6.0 起统一使用：

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

生产升级前请先备份数据库。

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

## 主要资源权限 API

```text
GET    /api/access/knowledge-bases/{id}/users?keyword=&take=50
GET    /api/access/knowledge-bases/{id}/members
PUT    /api/access/knowledge-bases/{id}/members
DELETE /api/access/knowledge-bases/{id}/members/{userId}

GET    /api/access/documents/{id}/permissions
PUT    /api/access/documents/{id}/permissions
DELETE /api/access/documents/{id}/permissions/{userId}
```

## CI

仓库内置：

```text
.github/workflows/ci.yml
```

每次推送到 `main` 或 Pull Request 自动执行：

- `.NET 10 restore`
- `.NET 10 Release build`
- `npm install`
- `vue-tsc`
- `vite build`

## 下一阶段

1. 增加服务端 CSV / Excel 全量导出接口与异步导出任务。
2. 分享链接增加密码、访问次数、访问日志、手动失效时间。
3. Markdown ZIP 批量导入导出。
4. Word / PDF / HTML 导入解析。
5. Meilisearch 中文全文索引、高亮和异步索引任务。
6. 评论、@成员、通知中心。
7. API Key、Webhook、开放 API。
8. 最后接入可选 Embedding、RAG、语义检索和知识问答，保持智能能力可关闭、可替换。
