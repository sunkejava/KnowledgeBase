# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。强调工程化、可维护性和长期扩展能力，界面采用克制、专业的科技风格，不把“AI 对话框”作为产品中心。

## 当前版本

`v0.3.0`

当前已具备：**JWT 登录、RBAC 基础模型、知识库 CRUD、文档目录树、Markdown 文档编辑保存、系统管理基础查询、主题/语言/水印/字号配置**。

## 技术栈

### 后端
- .NET 10 / ASP.NET Core Web API
- DDD + Clean Architecture
- EF Core 10 + SQLite
- JWT Bearer Authentication
- PBKDF2-SHA256 密码哈希
- OpenAPI

### 前端
- Vue 3 + TypeScript + Vite
- Pinia + Vue Router + Axios
- Element Plus + Lucide Icons

## 已实现

### 身份认证 / RBAC
- JWT 登录
- Bearer Token 自动注入
- 401 自动退出
- 用户、角色、用户角色、菜单、角色菜单领域模型
- 部门、组织机构模型
- 超级管理员保护的系统查询接口
- 首次启动初始化管理员

开发环境默认账号：

```text
admin / Admin123!
```

生产部署后必须修改默认密码，并替换 `Jwt:Key`。

### 知识库与文档
- 知识库列表、创建、编辑、删除
- 知识库工作区
- 文档目录树
- 父子文档层级
- 新建根文档 / 子文档
- Markdown 编辑
- Markdown 正文真实持久化
- 编辑保存 / 删除
- `Kb_Document` 与 `Kb_DocumentContent` 分表
- 目录请求不返回 Markdown 正文，只有打开具体文档时才加载正文，避免文档数量增加后页面越来越慢

### 系统管理
当前已有真实查询：用户、角色、部门、组织机构、菜单。下一阶段继续补完整 CRUD、树形编辑、用户角色分配、角色菜单授权、动态路由与按钮级权限。

### 界面个性化
- 深色 / 浅色 / 跟随系统
- 简体中文 / English 基础切换
- 全局字号 12~18px
- 自定义水印
- 紧凑模式
- localStorage 即时恢复
- 后端用户界面偏好接口

## 工程结构

```text
KnowledgeBase
├─ backend
│  ├─ KnowledgeBase.Api
│  ├─ KnowledgeBase.Application
│  ├─ KnowledgeBase.Contracts
│  ├─ KnowledgeBase.Domain
│  └─ KnowledgeBase.Infrastructure
├─ frontend
├─ docs
├─ docker-compose.yml
└─ KnowledgeBase.slnx
```

## 启动

### 后端
```bash
cd backend
dotnet restore
dotnet run --project KnowledgeBase.Api/KnowledgeBase.Api.csproj
```

### 前端
```bash
cd frontend
npm install
npm run dev
```

前端默认 API：`http://localhost:5000/api`，可通过 `VITE_API_BASE_URL` 调整。

> 当前开发阶段使用 `EnsureCreatedAsync` 初始化新数据库。若你已经运行过旧版并生成了 `knowledgebase.db`，由于本阶段新增了 RBAC 和文档正文表，建议删除旧开发数据库后重新启动。正式版本将切换 EF Core Migration 管理升级，不要求生产删除数据库。

## API

```text
POST   /api/auth/login
GET    /api/knowledge-bases
GET    /api/knowledge-bases/{id}
POST   /api/knowledge-bases
PUT    /api/knowledge-bases/{id}
DELETE /api/knowledge-bases/{id}

GET    /api/documents?knowledgeBaseId={id}
GET    /api/documents/{id}
POST   /api/documents
PUT    /api/documents/{id}
DELETE /api/documents/{id}

GET    /api/system/users
GET    /api/system/roles
GET    /api/system/departments
GET    /api/system/organizations
GET    /api/system/menus

GET    /api/settings/appearance
PUT    /api/settings/appearance
```

## 架构原则
- Controller 不承载复杂业务逻辑
- Application 定义用例契约
- Domain 维护领域状态规则
- Infrastructure 实现 EF Core、认证等技术能力
- 列表 DTO 不携带正文大字段
- 文件、全文搜索、向量检索和模型能力后续全部通过抽象接口接入

## 开发路线

### 阶段 1：基础平台（进行中）
- [x] 工程架构
- [x] 主题 / 明暗 / 语言 / 水印 / 字号
- [x] JWT 登录
- [x] RBAC 基础模型
- [x] 用户/角色/部门/组织/菜单基础查询
- [x] 知识库 CRUD
- [x] 文档目录树
- [x] Markdown 文档创建、编辑、保存、删除
- [ ] 用户管理完整 CRUD
- [ ] 角色及菜单授权
- [ ] 部门、组织机构树形维护
- [ ] 菜单动态路由与按钮权限
- [ ] 标签、收藏、最近访问
- [ ] 登录日志、操作审计日志
- [ ] EF Core Migration 正式升级体系

### 阶段 2：专业知识管理
- 文档版本与 Diff
- 文档级权限
- 评论与附件
- Word/PDF/Markdown 导入
- 全文搜索

### 阶段 3：平台化
- API Key / Webhook / Open API
- Redis
- Meilisearch / OpenSearch
- S3 / MinIO 对象存储
- Docker 完整部署

### 阶段 4：智能知识能力
智能能力保持可插拔：Chunk、Embedding、Vector DB、Rerank、RAG、语义检索、知识问答、知识关系图谱。

## 安全建议
1. 修改默认管理员密码。
2. 使用环境变量或安全配置中心注入 JWT Key。
3. 限制 CORS 来源并启用 HTTPS。
4. 增加登录失败限流/锁定。
5. 增加登录日志和操作审计。
6. 正式数据库切换 EF Core Migration。
