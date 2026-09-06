# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。项目强调工程化、可维护性与长期扩展能力，界面采用克制的专业科技风，不以“AI 聊天页”作为产品中心。

## 当前版本

`v0.3.0`

当前已从工程骨架推进到可登录、可维护知识库、具备基础 RBAC 数据模型和界面个性化能力的第一阶段可用版本。

## 技术栈

### 后端

- .NET 10 / ASP.NET Core Web API
- DDD + Clean Architecture 分层
- Entity Framework Core 10
- SQLite（开发默认，可扩展 PostgreSQL / MySQL / SQL Server）
- JWT Bearer Authentication
- PBKDF2-SHA256 密码哈希
- OpenAPI

### 前端

- Vue 3
- TypeScript
- Vite
- Pinia
- Vue Router
- Axios
- Element Plus
- Lucide Icons

## 已实现功能

### 身份认证与权限基础

- JWT 登录认证
- 前端登录态持久化
- Axios 自动附加 Bearer Token
- 401 自动清理登录态并返回登录页
- RBAC 基础实体：用户、角色、用户角色、菜单、角色菜单
- 部门、组织机构基础实体
- 超级管理员保护的系统基础查询接口
- 首次启动自动初始化管理员

默认开发账号：

```text
账号：admin
密码：Admin123!
```

> 首次部署后请尽快实现/使用密码修改功能并更换默认密码；生产环境必须替换 `Jwt:Key`。

### 知识库

- 知识库真实 API 列表
- 创建知识库
- 编辑知识库
- 删除知识库（二次确认）
- 前端搜索过滤
- 列表不加载文档正文，为后续大数据量场景保留性能空间

### 界面个性化

- 深色主题
- 浅色主题
- 跟随系统
- 简体中文 / English 切换基础
- 12 ~ 18px 全局字号
- 自定义水印开关与内容
- 紧凑模式
- localStorage 即时持久化
- 后端账号级界面偏好设置接口

### 系统管理

已具备系统管理入口和真实查询接口：

- 用户
- 角色
- 部门
- 组织机构
- 菜单

当前阶段以基础模型与查询为主，下一阶段继续完成新增、编辑、删除、树形维护、用户角色分配、角色菜单授权和按钮级权限。

## 项目结构

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
├─ Directory.Build.props
└─ KnowledgeBase.slnx
```

## 本地启动

### 1. 启动后端

要求安装 .NET 10 SDK。

```bash
cd backend
dotnet restore
dotnet run --project KnowledgeBase.Api/KnowledgeBase.Api.csproj
```

默认数据库为 SQLite，首次启动会自动创建数据库结构并初始化默认管理员。

后端健康检查：

```text
GET /api/health
```

### 2. 启动前端

要求 Node.js 22+。

```bash
cd frontend
npm install
npm run dev
```

前端默认请求：

```text
http://localhost:5000/api
```

需要调整时可配置：

```text
VITE_API_BASE_URL=http://localhost:xxxx/api
```

## 主要 API

```text
POST   /api/auth/login

GET    /api/knowledge-bases
GET    /api/knowledge-bases/{id}
POST   /api/knowledge-bases
PUT    /api/knowledge-bases/{id}
DELETE /api/knowledge-bases/{id}

GET    /api/system/users
GET    /api/system/roles
GET    /api/system/departments
GET    /api/system/organizations
GET    /api/system/menus

GET    /api/settings/appearance
PUT    /api/settings/appearance
```

除登录、健康检查等公开接口外，业务接口逐步统一要求 JWT 身份认证。

## 架构约束

- Controller 不承载复杂业务逻辑。
- 领域对象维护自身状态规则。
- Application 层定义业务能力契约。
- Infrastructure 层负责 EF Core、认证、文件、搜索等技术实现。
- 文档列表与文档正文分离，避免知识量增长后工作台加载全部正文。
- 所有外部可替换能力逐步通过抽象接口接入。

## 开发路线

### 阶段 1：基础平台（进行中）

- [x] 工程架构
- [x] 主题 / 明暗 / 语言 / 水印 / 字号
- [x] JWT 登录
- [x] RBAC 基础模型
- [x] 用户/角色/部门/组织/菜单基础查询
- [x] 知识库 CRUD
- [ ] 用户管理完整 CRUD
- [ ] 角色及菜单授权
- [ ] 部门、组织机构树形维护
- [ ] 菜单动态路由与按钮权限
- [ ] 文档目录树
- [ ] Markdown 编辑器与文档正文保存
- [ ] 标签、收藏、最近访问
- [ ] 登录日志、操作审计日志

### 阶段 2：专业知识管理

- 文档版本与 Diff
- 文档级权限
- 评论与附件
- 导入/导出
- 全文搜索

### 阶段 3：平台化

- API Key
- Webhook
- Open API
- Redis
- Meilisearch / OpenSearch
- 对象存储
- Docker 完整部署

### 阶段 4：智能知识能力

智能能力保持可插拔，不与基础知识管理强耦合：

- Chunk
- Embedding
- Vector DB
- Rerank
- RAG
- 语义检索
- 知识问答
- 知识关系图谱

## 安全说明

当前默认账号和 JWT Key 仅面向开发初始化。生产环境应至少完成：

1. 修改默认管理员密码。
2. 使用环境变量或安全配置中心注入 JWT Key。
3. 配置严格 CORS 来源。
4. 启用 HTTPS。
5. 增加登录限流、失败锁定和审计日志。
6. 使用 EF Core Migration 管理正式数据库版本。

## License

后续根据项目交付方式补充。
