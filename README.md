# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。项目强调专业、克制、工程化、权限安全和长期可维护性，前后端均按可持续扩展方向建设。

## 当前版本

`v0.14.0`

当前已具备：

- JWT / RBAC
- 用户、角色、部门、组织、菜单管理
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

# v0.14.0 重点更新

## 1. 统一文件存储抽象

新增：

```text
IFileStorage
└─ LocalFileStorage
```

业务服务不再直接依赖：

```text
File.Create
File.OpenRead
File.Delete
PhysicalFile
AppContext.BaseDirectory + 固定业务路径
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

当前已迁移到统一存储的链路：

```text
文档附件
异步导入源文件
异步导出结果文件
```

后续增加：

```text
MinIO
Amazon S3
阿里云 OSS
腾讯云 COS
```

时，业务服务无需修改，只增加 `IFileStorage` 实现和 Provider 注册即可。

### 历史文件兼容

v0.14.0 会兼容旧数据中的：

```text
storage/attachments/...
storage/imports/...
storage/exports/...
```

新服务读取时会自动转换为统一对象 Key，不要求手工搬迁已有文件。

Docker 下默认：

```text
Storage__Provider=local
Storage__Local__Root=/app/storage
```

并挂载：

```text
kb-storage:/app/storage
```

---

## 2. 文档评论

新增：

```text
Kb_Comment
Kb_CommentMention
```

对应后端：

```text
DocumentComment
DocumentCommentMention
ICollaborationService
CollaborationService
CollaborationController
```

支持：

- 文档评论
- 评论分页
- 评论内容搜索
- 本人删除评论
- 超级管理员删除评论
- 评论 @成员
- @成员候选搜索
- 仅允许 @ 当前知识库成员
- 评论 CSV 导出
- 评论列表列设置、列宽、分页等公共表格能力

评论页面：

```text
/collaboration
```

用户先选择知识库，再选择具体文档进行讨论。

---

## 3. @成员权限隔离

评论 @成员接口不会开放全部系统用户。

候选接口：

```text
GET /api/collaboration/documents/{documentId}/mention-users
```

后端会：

```text
文档
 ↓
KnowledgeBaseId
 ↓
Kb_KnowledgeBaseMember
 ↓
Sys_User
```

只返回：

```text
Id
UserName
DisplayName
```

客户端即使手工提交其他用户 GUID，后端仍会重新校验该用户是否属于当前知识库。

---

## 4. 通知中心

新增：

```text
Sys_Notification
UserNotification
NotificationDto
NotificationSummaryDto
```

当前通知来源：

```text
评论 @成员
```

后续任务通知、系统公告、分享提醒等都继续复用同一通知表。

通知能力：

- 服务端分页
- 标题 / 内容搜索
- 未读数量
- 单条已读
- 全部已读
- 点击通知跳转目标文档
- 通知列表 CSV 导出
- 顶栏未读数量
- 侧栏未读数量

前端新增：

```text
frontend/src/views/Notifications.vue
frontend/src/stores/notifications.ts
frontend/src/api/modules/collaboration.ts
```

通知入口：

```text
/notifications
```

Pinia `notification store` 统一维护未读数量，避免 `App.vue` 和通知页面各自维护重复状态。

---

## 5. v0.14.0 数据库迁移

新增 Migration：

```text
202609061400_V014Collaboration
```

新增表：

```text
Kb_Comment
Kb_CommentMention
Sys_Notification
```

程序启动继续统一执行：

```csharp
await db.Database.MigrateAsync();
```

生产升级前请先备份 SQLite 数据库。

---

# 搜索体系

## 搜索抽象

```text
IKnowledgeSearchService
├─ SqliteKnowledgeSearchService
└─ MeilisearchKnowledgeSearchService
```

统一能力：

```text
SearchAsync
UpsertDocumentAsync
DeleteDocumentAsync
RebuildIndexAsync
ProviderName
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

Docker：

```bash
docker compose --profile search up -d
```

---

# 导入导出

支持：

```text
Markdown
HTML / HTM
DOCX
Markdown ZIP
```

ZIP 支持恢复父子文档目录。

异步任务：

```text
Sys_ImportTask
Sys_ExportTask
```

状态：

```text
Pending
Running
Completed
Failed
Cancelled
```

支持：

- 排队
- 进度
- 取消
- 重试
- 历史清理
- 文件清理

---

# 权限模型

```text
Viewer
  查看知识库和文档

Editor
  Viewer
  + 创建/编辑文档
  + 上传附件
  + 导入内容

Manager
  Editor
  + 删除
  + 成员管理
  + 文档权限管理
  + 分享管理
```

原则：

- 超级管理员拥有全部资源权限。
- 知识库创建人自动成为 Manager。
- 普通用户只能看到自己有权限的知识库。
- 文档显式权限优先于知识库成员权限。
- 搜索必须在后端执行权限过滤。
- 附件、版本、Diff、分享、导入导出均执行后端资源权限。
- 评论读取与创建要求拥有文档查看权限。
- 前端隐藏按钮不是安全边界。

---

# 前端公共组件规范

公共组件：

```text
frontend/src/components/common/
├─ BaseDataTable.vue
├─ BaseTreeManager.vue
└─ PageHeader.vue
```

`BaseDataTable` 支持：

- 本地分页
- 服务端分页
- 每页条数
- CSV 导出
- 勾选导出
- 显示 / 隐藏列
- 拖动调整列宽
- 列顺序调整
- 配置持久化
- 固定操作列
- 排序
- Loading / Empty 状态
- 自定义单元格
- 自定义操作区

业务列表页面禁止重复实现表格、分页、列设置和导出逻辑。

---

# 前端 API 规范

统一放在：

```text
frontend/src/api/modules/
├─ auth.ts
├─ system.ts
├─ knowledge.ts
├─ documents.ts
├─ access.ts
├─ share.ts
├─ exchange.ts
├─ search.ts
└─ collaboration.ts
```

页面禁止自行散落：

```ts
http.get('/xxx')
http.post('/xxx')
```

业务 URL、参数和请求配置统一进入领域 API Module。

---

# 后端开发规范

- 所有解释性代码注释统一使用中文。
- XML `summary` 使用中文描述职责。
- JWT、EF Core、HTTP、PBKDF2 等标准技术名称保留原名。
- Controller 只处理 HTTP、身份、权限和参数映射。
- 业务逻辑进入 Application / Infrastructure。
- 文件访问统一使用 `IFileStorage`。
- 搜索统一使用 `IKnowledgeSearchService`。
- 大列表优先数据库分页。
- 大文件优先后台任务。
- 数据库结构统一 EF Core Migration。
- 权限校验必须在后端执行。

---

# 主要数据库表

## 系统

```text
Sys_User
Sys_Role
Sys_UserRole
Sys_Department
Sys_Organization
Sys_Menu
Sys_RoleMenu
Sys_AuditLog
Sys_UserAppearanceSetting
Sys_Notification
Sys_ImportTask
Sys_ExportTask
Sys_SearchIndexTask
```

## 知识资产

```text
Kb_KnowledgeBase
Kb_KnowledgeBaseMember
Kb_Document
Kb_DocumentContent
Kb_DocumentVersion
Kb_DocumentPermission
Kb_Tag
Kb_DocumentTag
Kb_Attachment
Kb_Favorite
Kb_RecentView
Kb_ShareLink
Kb_ShareAccessLog
Kb_Comment
Kb_CommentMention
```

---

# 启动

## 后端

```bash
cd backend/KnowledgeBase.Api
dotnet restore
dotnet run
```

默认开发账号：

```text
admin / Admin123!
```

生产环境必须修改默认密码与：

```text
Jwt:Key
```

## 前端

```bash
cd frontend
npm install
npm run dev
```

---

# Docker

复制环境配置：

```bash
cp .env.example .env
```

默认 SQLite 搜索：

```bash
docker compose up -d
```

启用 Meilisearch：

```bash
docker compose --profile search up -d
```

重要持久化卷：

```text
kb-data     SQLite
kb-storage  附件 / 导入 / 导出文件
meili-data  Meilisearch
```

---

# CI

`.github/workflows/ci.yml` 在 push main / Pull Request 时执行：

```text
NuGet Restore
NuGet Vulnerability Scan
.NET 10 Release Build
npm install
vue-tsc
Vite Production Build
```

后端漏洞扫描：

```bash
dotnet list KnowledgeBase.slnx package --vulnerable --include-transitive
```

当前 .NET 稳定依赖已消除此前高危 NuGet 告警。

---

# 当前 Migration

```text
202609060600_BaselineV060
202609061100_V011ExportAndShareAudit
202609061200_V012ImportTasks
202609061300_V013SearchIndexTasks
202609061400_V014Collaboration
```

---

# 下一阶段

优先路线：

1. `IFileStorage` 增加 MinIO 实现，并提供存储连通性测试与迁移工具。
2. 评论回复树、评论编辑、评论定位到文档段落。
3. SignalR 实时通知与在线状态。
4. DOCX 表格、图片、超链接导入。
5. PDF 文本型文档导入；OCR 单独作为可选能力。
6. Meilisearch 中文分词、同义词、停用词和相关性配置。
7. API Key、Webhook、开放 API。
8. Embedding / RAG / 语义搜索保持可关闭、可替换。
