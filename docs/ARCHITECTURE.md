# KnowledgeBase 架构说明

## 原则

- 后端采用 DDD + Clean Architecture。
- Controller 不直接操作 EF Core DbContext。
- 列表接口禁止返回文档正文，正文按需加载。
- 所有时间持久化使用 UTC，前端按用户时区展示。
- 权限设计按系统、知识库、空间、文档四级逐步扩展。
- 文件存储、全文搜索、Embedding、LLM 均通过抽象接口接入。

## 第一阶段

1. 用户、角色、菜单、组织机构与权限。
2. 知识库、空间、目录、文档与 Markdown 编辑。
3. 标签、收藏、最近访问、审计日志。
4. 深色/浅色主题与统一后台交互规范。

## 后续阶段

- 文档版本与 Diff。
- Meilisearch 全文检索。
- API Key / Webhook / Open API。
- RAG、语义检索、知识关系图谱。
