# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。项目强调专业、克制、工程化、权限安全、SQLite 兼容性和长期可维护性。

## 当前版本

`v0.14.2`

详细说明文档：

```text
docs/PROJECT_GUIDE.md
docs/SQLITE_ENGINEERING_RULES.md
```

## v0.14.2 重点修复

### 1. 修复 SQLite 历史数据库字段缺失

早期 `BaselineV060` 创建 `Kb_KnowledgeBase` 时遗漏 `UpdatedAt`，而当前领域实体 `KnowledgeBaseSpace` 一直包含该属性，导致 EF Core 查询生成：

```sql
SELECT ..., k.UpdatedAt
FROM Kb_KnowledgeBase AS k
```

旧数据库会报：

```text
SQLite Error 1: 'no such column: k.UpdatedAt'
```

本版本新增 Migration：

```text
202609061500_V0142SchemaAndMenuSeed
```

自动补齐：

```text
Kb_KnowledgeBase.UpdatedAt
Kb_DocumentContent.PlainText
Kb_DocumentContent.UpdatedAt
```

并自动回填历史数据。升级时 **不需要删除 SQLite 数据库**，程序启动后会通过：

```csharp
await db.Database.MigrateAsync();
```

自动完成升级。

> 生产升级前仍建议备份 SQLite 数据库文件。

### 2. SQLite DateTimeOffset 查询规范

项目已禁止直接让 SQLite 对 `DateTimeOffset` 做 `ORDER BY` 和范围比较。

统一使用：

```text
ToSqliteSafeDateTimeOffsetPageAsync
ToSqliteSafeNullableDateTimeOffsetPageAsync
ToSqliteSafeDateTimeOffsetListAsync
FirstOrDefaultSqliteSafeDateTimeOffsetAsync
```

CI 会执行：

```text
scripts/check_sqlite_datetimeoffset.py
```

避免后续再次引入同类运行时异常。

### 3. 菜单权限初始化完善

v0.14.2 会自动初始化标准菜单与权限节点，包括：

```text
工作台
知识资产
├─ 我的知识库
├─ 全部文档
├─ 最近浏览
├─ 我的收藏
└─ 协作评论

平台管理
├─ 标签管理
├─ 通知中心
├─ 数据交换
└─ 搜索管理

系统管理
├─ 用户管理
├─ 新增用户
├─ 编辑用户
├─ 删除用户
├─ 角色管理
├─ 角色授权
├─ 部门管理
├─ 组织管理
├─ 菜单管理
└─ 审计日志
```

核心权限编码示例：

```text
dashboard:view
knowledgebase:view
document:view
favorite:view
recent:view
collaboration:view
tag:view
notification:view
exchange:view
search:manage
system:view
system:user:view
system:user:create
system:user:update
system:user:delete
system:role:view
system:role:update
system:department:manage
system:organization:manage
system:menu:manage
system:audit:view
```

`SUPER_ADMIN` 会自动关联全部初始化菜单。

### 4. 工作台改为真实数据

原工作台中的示例数字和演示列表已全部删除。

新增真实接口：

```http
GET /api/dashboard/overview
```

统计内容包括：

- 当前用户可访问知识库数量
- 当前用户可访问文档数量
- 我的收藏数量
- 未读通知数量
- 相关知识库成员数
- 最近访问文档
- 热门文档
- 导入任务状态
- 导出任务状态
- 搜索索引任务状态
- 失败任务数量

所有数据均在后端按当前用户资源权限过滤。

### 5. 左侧菜单激活样式修复

以前 `/knowledge-center` 下的：

```text
全部文档
最近浏览
我的收藏
标签管理
```

虽然 query 不同，但 Vue Router 默认会把同一路由记录同时判定为 active，造成多个菜单同时高亮。

现在由 `App.vue` 统一基于：

```text
route.path + route.query.tab
```

计算唯一激活项，并使用：

```text
nav-active
```

控制样式。

---

## 当前主要能力

- JWT 登录认证
- RBAC 用户 / 角色 / 菜单权限
- 部门 / 组织机构
- 知识库管理
- 层级文档
- Markdown 编辑
- 标签
- 收藏
- 最近浏览
- 文档版本 / Diff / 回滚
- 附件
- Viewer / Editor / Manager 资源权限
- 文档显式权限
- 分享密码 / 分享审计
- 评论 / @成员
- 通知中心
- Markdown / HTML / DOCX / ZIP 导入
- 异步导入 / 导出任务
- SQLite / Meilisearch 搜索
- 搜索索引任务中心
- IFileStorage 统一存储抽象
- 审计日志
- 主题 / 语言 / 水印 / 字号
- 公共表格 / 树组件
- 服务端分页
- Docker Compose / Nginx
- GitHub Actions CI

---

## 系统管理入口

前端路由：

```text
/system
```

页面：

```text
frontend/src/views/SystemManagement.vue
```

包含：

```text
用户
角色
部门
组织机构
菜单权限
审计日志
```

默认管理员：

```text
admin / Admin123!
```

生产环境必须修改默认密码。

---

## 技术栈

### 后端

- .NET 10
- ASP.NET Core Web API
- EF Core 10
- SQLite
- DDD / Clean Architecture
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

## 目录结构

```text
KnowledgeBase/
├─ backend/
│  ├─ KnowledgeBase.Api
│  ├─ KnowledgeBase.Application
│  ├─ KnowledgeBase.Contracts
│  ├─ KnowledgeBase.Domain
│  └─ KnowledgeBase.Infrastructure
├─ frontend/
│  └─ src/
│     ├─ api/modules
│     ├─ components/common
│     ├─ components/knowledge
│     ├─ stores
│     └─ views
├─ docs/
├─ scripts/
├─ docker-compose.yml
└─ KnowledgeBase.slnx
```

---

## 启动

### 后端

```bash
cd backend/KnowledgeBase.Api
dotnet restore
dotnet run
```

启动时自动执行数据库 Migration。

### 前端

```bash
cd frontend
npm install
npm run dev
```

---

## Docker

```bash
cp .env.example .env
docker compose up -d
```

启用 Meilisearch：

```bash
docker compose --profile search up -d
```

---

## SQLite 开发要求

使用 SQLite 时必须遵守：

- 不直接对 `DateTimeOffset` 执行数据库排序。
- 不直接对 `DateTimeOffset` 执行 `< > <= >=` 范围查询。
- 中小数据量可先数据库过滤、再内存时间排序。
- 大数据量必须增加 Unix 毫秒 `long` 字段用于索引和排序。
- 数据结构变更必须提供 Migration。
- 禁止只修改 Entity 而不更新数据库结构。
- 新增字段后必须验证历史数据库升级路径。

完整规范：

```text
docs/SQLITE_ENGINEERING_RULES.md
```

---

## CI

`.github/workflows/ci.yml` 当前检查：

```text
SQLite DateTimeOffset 查询规则
NuGet Restore
NuGet Vulnerability Scan
.NET 10 Release Build
npm install
vue-tsc
Vite Production Build
```

所有合并到 `main` 的功能都应先通过 CI。
