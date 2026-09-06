# KnowledgeBase

一套基于 **.NET 10 + Vue 3 + TypeScript** 构建的企业级知识库管理平台。产品强调专业、克制、工程化和长期可维护性，不把 AI 对话框作为视觉中心。

## 当前版本

`v0.4.0`

当前已经具备从登录、权限、知识库创建到文档编辑保存的完整基础链路，并进入可持续扩展的系统管理阶段。

## 技术栈

- 后端：.NET 10 / ASP.NET Core Web API / DDD + Clean Architecture / EF Core 10 / SQLite / JWT
- 前端：Vue 3 / TypeScript / Vite / Pinia / Vue Router / Axios / Element Plus

## 已实现

### 身份认证与权限
- JWT 登录、Bearer Token 自动注入、401 自动退出
- PBKDF2-SHA256 密码哈希
- 用户 CRUD、启停用、重置密码
- 角色 CRUD、用户角色分配
- 菜单与权限标识模型
- 角色菜单授权
- 当前用户权限画像接口 `/api/system/profile`
- 前端 `permission` Store，可用于菜单/按钮权限控制
- 默认超级管理员不可删除

默认开发账号：`admin / Admin123!`。生产部署必须修改默认密码与 `Jwt:Key`。

### 组织管理
- 部门树数据 CRUD
- 组织机构树数据 CRUD
- 排序与启停状态
- 菜单管理模型支持目录、菜单、按钮三类权限节点

### 知识库与文档
- 知识库 CRUD
- 知识库工作区
- 文档目录树、父子层级
- Markdown 新建、编辑、保存、删除
- `Kb_Document` / `Kb_DocumentContent` 分表
- 目录列表不加载 Markdown 正文，打开文档时才按需加载

### 审计与安全
- `Sys_AuditLog` 审计日志领域模型
- 审计日志查询接口
- 登录日志/操作日志统一按 Category + Action 设计，可继续通过中间件和业务拦截器补齐自动写入
- 修改密码接口

### 界面个性化
- 深色 / 浅色 / 跟随系统
- 简体中文 / English 基础切换
- 字号 12~18px
- 水印、自定义水印内容
- 紧凑模式
- 配置即时生效并本地持久化

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

默认 SQLite 数据库为 `knowledgebase.db`。当前开发阶段仍使用 `EnsureCreatedAsync`；若从早期版本升级且缺少新表，可删除开发数据库后重新启动。正式版本将切换为 EF Core Migration，避免删库升级。

## 主要 API

- `POST /api/auth/login`
- `GET /api/system/profile`
- `GET|POST|PUT|DELETE /api/system/users`
- `GET|POST|PUT|DELETE /api/system/roles`
- `GET|POST|PUT|DELETE /api/system/departments`
- `GET|POST|PUT|DELETE /api/system/organizations`
- `GET|POST|PUT|DELETE /api/system/menus`
- `POST /api/system/change-password`
- `GET /api/system/audit-logs`
- `GET|POST|PUT|DELETE /api/knowledge-bases`
- `GET|POST|PUT|DELETE /api/documents`
- `GET|PUT /api/settings/appearance`

## 架构原则

1. Controller 不直接操作 EF Core DbContext。
2. 文档目录与正文分离，杜绝工作区一次加载全部正文。
3. 权限从系统级向知识库、空间、文档级逐层扩展。
4. 文件存储、搜索、Embedding、LLM 保持抽象，可独立替换。
5. 前端配置、表格、树和操作都连接真实 API，不做假页面。
6. 所有关键管理与知识资产操作逐步纳入审计。

## 下一阶段

- EF Core Migration 正式数据库升级体系
- 系统管理页完善部门/组织/菜单树形编辑
- 动态路由真正按角色菜单生成
- 按钮级 `v-permission` 指令
- 登录日志与操作日志自动拦截记录
- 标签、收藏、最近访问、附件
- 文档版本、Diff、回滚
- 全文检索 / Meilisearch
- API Key、Webhook、开放 API
