# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。项目强调工程化、权限安全、SQLite 兼容性、公共组件复用和长期可维护性。

## 当前版本

`v0.14.3`

详细文档：

```text
docs/PROJECT_GUIDE.md                 项目完整说明
docs/SQLITE_ENGINEERING_RULES.md      SQLite 工程规范
docs/DOCUMENT_IMPORT_GUIDE.md         常见文档导入说明
```

---

# v0.14.3 重点更新：常见文档统一导入

知识库现在支持直接导入：

```text
TXT
Markdown (.md / .markdown)
HTML / HTM
PDF
DOC
DOCX
XLS
XLSX
CSV
Markdown ZIP
```

统一单文档接口：

```http
POST /api/content-exchange/knowledge-bases/{knowledgeBaseId}/document
```

支持格式查询：

```http
GET /api/content-exchange/supported-formats
```

可通过：

```text
parentId
```

把导入结果直接创建为已有文档的子文档。

## 导入入口

### 知识库工作区

```text
我的知识库
  ↓
进入知识库
  ↓
导入
```

现在工作区的“导入”按钮不再只接受 Markdown，而是统一支持：

```text
.txt
.md
.markdown
.html
.htm
.pdf
.doc
.docx
.xls
.xlsx
.csv
```

当前已选中文档时，新导入文档会作为其子文档；未选中文档时创建为根文档。

### 数据交换

```text
平台 → 数据交换 → 导入常见文档
```

支持一次选择多个文件，前端顺序导入并汇总失败文件。

Markdown ZIP 继续使用现有异步任务中心，适合大批量迁移。

## 各格式解析方式

| 格式 | 实现 | 结果 |
| --- | --- | --- |
| TXT | 内置文本解析器 | 正文 Markdown |
| Markdown | 原样读取 | 保留 Markdown |
| HTML | 内置转换 | 标题、列表、段落转 Markdown |
| PDF | PdfPig 0.1.16 | 提取文本型 PDF 正文 |
| DOCX | Office Open XML | 正文 + Heading1~Heading6 |
| DOC | LibreOffice headless | 提取 Word 97-2003 正文 |
| XLS/XLSX | ExcelDataReader 3.9.0 | 每个 Sheet 转 Markdown 表格 |
| CSV | 内置 CSV 解析器 | 转 Markdown 表格 |

### TXT 中文编码

支持：

```text
UTF-8
UTF-8 BOM
UTF-16 LE/BE
GBK / GB18030 fallback
```

用于兼容历史 Windows 中文 TXT 文件。

### PDF

当前支持**文本型 PDF**。

如果 PDF 是扫描件或纯图片，系统不会创建空文档，而会返回明确错误：

```text
PDF 未提取到可用文本。该文件可能是扫描件或纯图片 PDF，请使用 OCR 后再导入。
```

OCR 后续作为可选能力接入，不让普通 PDF 导入强依赖 OCR 环境。

### Excel

`.xls/.xlsx` 中每个工作表会转换成类似：

```markdown
## Sheet1

| 姓名 | 部门 | 状态 |
| --- | --- | --- |
| 张三 | 研发 | 在职 |
| 李四 | 运维 | 在职 |
```

目标是让 Excel 数据可以：

- 在知识库中阅读；
- 全文搜索；
- 版本管理；
- 参与后续 RAG / AI 检索。

暂不尝试 1:1 还原单元格颜色、图表、宏和复杂排版。

### DOC（Word 97-2003）

`.doc` 使用 **LibreOffice headless** 转文本，不依赖 Microsoft Office COM，也不引入老旧 HWPF/ScratchPad 依赖。

Docker 后端镜像已经安装：

```text
libreoffice-writer
```

Windows 本地运行时建议安装 LibreOffice。如果 `soffice.exe` 不在 PATH，可配置：

```json
"DocumentImport": {
  "LibreOfficePath": "C:\\Program Files\\LibreOffice\\program\\soffice.exe"
}
```

详细格式说明见：

```text
docs/DOCUMENT_IMPORT_GUIDE.md
```

---

# 主要功能

## 知识资产

- 知识库 CRUD
- 层级文档目录
- Markdown 正文编辑
- 文档元数据与正文分离加载
- 标签
- 收藏
- 最近浏览
- 附件
- 版本快照
- 版本恢复
- Diff
- 评论 / @成员
- 通知中心
- 安全分享
- 分享密码
- 分享访问日志

## 权限

资源角色：

```text
Viewer
Editor
Manager
```

支持：

- 知识库成员权限
- 单文档显式权限
- 创建人自动 Manager
- 搜索权限过滤
- 附件 / 版本 / 分享 / 导入导出后端权限校验
- SUPER_ADMIN 全资源权限

## 系统管理

入口：

```text
/system
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

权限链路：

```text
Sys_User
  ↓
Sys_UserRole
  ↓
Sys_Role
  ↓
Sys_RoleMenu
  ↓
Sys_Menu
```

系统会初始化工作台、知识资产、平台管理和系统管理的标准菜单/按钮权限。

## 工作台

工作台数据全部来自真实业务数据，不使用演示数字。

统计包括：

- 当前用户可访问知识库
- 可访问文档
- 收藏
- 未读通知
- 相关成员
- 最近访问
- 热门知识
- 导入任务
- 导出任务
- 索引任务
- 失败任务

---

# 搜索

统一抽象：

```text
IKnowledgeSearchService
├─ SqliteKnowledgeSearchService
└─ MeilisearchKnowledgeSearchService
```

默认：

```text
Search:Provider=sqlite
```

可选：

```text
Search:Provider=meilisearch
```

Meilisearch 支持：

- 标题 + Markdown 正文
- knowledgeBaseId 权限过滤
- 新增/编辑/删除增量索引
- 全量索引重建任务
- 任务失败重试

---

# 文件存储

业务文件统一经过：

```text
IFileStorage
└─ LocalFileStorage
```

当前覆盖：

- 附件
- 导入源文件
- 导出结果文件

后续可以继续增加：

```text
MinIO
S3
OSS
COS
```

业务服务不直接耦合磁盘路径。

---

# 后台任务

当前任务体系：

```text
Sys_ExportTask
Sys_ImportTask
Sys_SearchIndexTask
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
- 后台 Worker
- 进度
- 取消
- 重试
- 历史清理

---

# SQLite 兼容规则

项目已修复历史 Migration 与实体模型不一致的问题，并禁止 SQLite 对 `DateTimeOffset` 直接执行不兼容的排序/范围查询。

CI 自动执行：

```text
scripts/check_sqlite_datetimeoffset.py
```

中小规模时间排序统一使用：

```text
ToSqliteSafeDateTimeOffsetPageAsync
ToSqliteSafeNullableDateTimeOffsetPageAsync
ToSqliteSafeDateTimeOffsetListAsync
FirstOrDefaultSqliteSafeDateTimeOffsetAsync
```

大规模日志/任务表推荐额外维护 Unix 毫秒 `long` 列，在 SQLite 中直接索引、排序和分页。

详细规范：

```text
docs/SQLITE_ENGINEERING_RULES.md
```

---

# 前端工程规范

公共组件：

```text
frontend/src/components/common/
├─ BaseDataTable.vue
├─ BaseTreeManager.vue
└─ PageHeader.vue
```

业务列表禁止重复实现：

- 分页
- 列设置
- 列宽
- CSV 导出
- 勾选导出
- Loading / Empty

API 统一集中：

```text
frontend/src/api/modules/
```

页面不得散落重复 Axios URL。

常见文档导入统一使用：

```text
exchangeApi.importDocument
或
documentApi.importDocument
```

---

# 后端工程规范

- 后端解释性注释全部使用中文。
- Controller 保持轻量，只处理 HTTP、身份、权限和参数映射。
- 业务逻辑进入 Application / Infrastructure。
- 文档解析统一进入 `IContentExchangeService`，后续继续拆成 `IDocumentParser`。
- 文件访问统一经过 `IFileStorage`。
- 搜索统一经过 `IKnowledgeSearchService`。
- 大列表使用服务端分页。
- 大任务优先后台队列。
- Entity 变化必须同步 Migration。
- 不允许通过删除 SQLite 数据库代替正式数据库升级。

---

# 技术栈

后端：

```text
.NET 10
ASP.NET Core Web API
EF Core 10
SQLite
JWT
PBKDF2-SHA256
PdfPig
ExcelDataReader
LibreOffice headless（仅老式 DOC）
```

前端：

```text
Vue 3
TypeScript
Vite
Pinia
Vue Router
Axios
Element Plus
Lucide
```

可选基础设施：

```text
Meilisearch
Docker Compose
Nginx
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

生产环境必须修改默认密码和 JWT Key。

## 前端

```bash
cd frontend
npm install
npm run dev
```

---

# Docker

```bash
cp .env.example .env
docker compose up -d
```

启用 Meilisearch：

```bash
docker compose --profile search up -d
```

持久化：

```text
kb-data      SQLite
kb-storage   附件 / 导入 / 导出
meili-data   Meilisearch
```

后端 Docker 镜像同时包含 LibreOffice Writer，用于 `.doc` 导入。

---

# CI

每次 push main / Pull Request 自动执行：

```text
SQLite DateTimeOffset 静态检查
NuGet Restore
NuGet Vulnerability Scan
.NET 10 Release Build
npm install
vue-tsc
Vite Production Build
```

---

# 当前主要 Migration

```text
202609060600_BaselineV060
202609061100_V011ExportAndShareAudit
202609061200_V012ImportTasks
202609061300_V013SearchIndexTasks
202609061400_V014Collaboration
202609061500_V0142SchemaAndMenuSeed
```

---

# 下一阶段

优先路线：

1. PDF OCR 作为可选解析 Provider。
2. DOCX 表格、图片、超链接完整转换。
3. 常见文档导入接入异步 ImportTask，支持超大 PDF/Excel。
4. `IDocumentParser` 按格式拆分，支持插件式 Parser 注册。
5. MinIO 存储实现。
6. SignalR 实时通知。
7. 评论回复树与编辑。
8. Meilisearch 中文相关性、同义词和高亮。
