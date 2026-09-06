# SQLite 工程规范

> 本规范适用于所有使用 EF Core + SQLite 的 .NET 项目。
>
> 重点目标：避免 `DateTimeOffset`、并发、分页、Migration、文件路径和大数据量查询在 SQLite 下产生运行时问题。

---

## 1. DateTimeOffset 是重点风险项

EF Core SQLite provider 对 `DateTimeOffset` 的排序和范围比较支持有限。

禁止直接写：

```csharp
query.OrderBy(x => x.CreatedAt);
query.OrderByDescending(x => x.UpdatedAt);
query.Where(x => x.CreatedAt < cutoff);
query.Where(x => x.UpdatedAt >= beginTime);
```

典型异常：

```text
SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses.
Convert the values to a supported type, or use LINQ to Objects to order the results on the client side.
```

---

## 2. 中小数据量处理方式

先让 SQLite 完成：

```text
普通字段过滤
权限过滤
Join
Select
```

然后：

```text
ToListAsync
LINQ to Objects DateTimeOffset 排序
分页
```

KnowledgeBase 已封装：

```csharp
ToSqliteSafeDateTimeOffsetPageAsync(...)
ToSqliteSafeNullableDateTimeOffsetPageAsync(...)
ToSqliteSafeDateTimeOffsetListAsync(...)
FirstOrDefaultSqliteSafeDateTimeOffsetAsync(...)
```

文件：

```text
backend/KnowledgeBase.Infrastructure/Common/QueryablePagingExtensions.cs
```

---

## 3. 大数据量正确方案

如果表数据可能达到几十万、百万级，禁止为了 DateTimeOffset 排序把全部记录加载进内存。

推荐同时保存：

```csharp
public DateTimeOffset CreatedAt { get; private set; }
public long CreatedAtUnixMs { get; private set; }
```

写入：

```csharp
CreatedAt = DateTimeOffset.UtcNow;
CreatedAtUnixMs = CreatedAt.ToUnixTimeMilliseconds();
```

查询：

```csharp
var rows = await query
    .OrderByDescending(x => x.CreatedAtUnixMs)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(ct);
```

时间范围：

```csharp
var beginMs = begin.ToUnixTimeMilliseconds();
var endMs = end.ToUnixTimeMilliseconds();

query = query.Where(x =>
    x.CreatedAtUnixMs >= beginMs &&
    x.CreatedAtUnixMs < endMs);
```

这样 SQLite 实际比较的是：

```text
INTEGER
```

可正常创建索引并高效分页。

推荐索引：

```csharp
entity.HasIndex(x => x.CreatedAtUnixMs);
```

---

## 4. 不建议把 DateTimeOffset 全局粗暴转换为 TEXT 后排序

SQLite 的 TEXT 时间排序只有在格式完全统一、时区完全统一时才可能保持时间顺序。

如果混合：

```text
+08:00
Z
+09:00
```

字符串排序并不等价于绝对时间排序。

因此业务上真正需要高效时间排序时，优先：

```text
UTC Unix milliseconds -> INTEGER
```

---

## 5. 时间统一原则

数据库业务时间推荐：

```csharp
DateTimeOffset.UtcNow
```

UI 展示时再转换成本地时间。

不要在数据库中混合保存：

```text
DateTime.Now
DateTime.UtcNow
DateTimeOffset.Now
DateTimeOffset.UtcNow
```

统一使用：

```csharp
DateTimeOffset.UtcNow
```

对于北京时间展示：

```text
Asia/Shanghai / UTC+08:00
```

应在应用层或前端格式化，不改变数据库排序基准。

---

## 6. SQLite 分页原则

安全字段可以直接：

```csharp
Where
OrderBy
Skip
Take
```

例如：

```text
string
int
long
Guid 等值过滤
```

DateTimeOffset 排序则按本规范处理。

---

## 7. SQLite 清理任务注意事项

错误：

```csharp
await db.Tasks
    .Where(x => x.CreatedAt < cutoff)
    .ToListAsync(ct);
```

中小数据量：

```csharp
var rows = await db.Tasks
    .Where(x => x.Status == "Completed")
    .ToListAsync(ct);

rows = rows
    .Where(x => x.CreatedAt < cutoff)
    .ToList();
```

大数据量：

```csharp
query.Where(x => x.CreatedAtUnixMs < cutoffUnixMs)
```

---

## 8. SQLite 后台队列领取任务

错误：

```csharp
await db.Tasks
    .OrderBy(x => x.CreatedAt)
    .FirstOrDefaultAsync(x => x.Status == "Pending", ct);
```

中小队列：

```csharp
await db.Tasks
    .Where(x => x.Status == "Pending")
    .FirstOrDefaultSqliteSafeDateTimeOffsetAsync(
        x => x.CreatedAt,
        descending: false,
        ct);
```

大队列：

```csharp
await db.Tasks
    .Where(x => x.Status == "Pending")
    .OrderBy(x => x.CreatedAtUnixMs)
    .FirstOrDefaultAsync(ct);
```

---

## 9. Migration 必须使用 EF Core Migration

禁止正式项目长期依赖：

```csharp
Database.EnsureCreatedAsync()
```

正确：

```csharp
await db.Database.MigrateAsync();
```

生产升级前：

1. 备份 SQLite 文件；
2. 执行 Migration；
3. 验证 Schema；
4. 验证关键接口；
5. 再开放流量。

---

## 10. SQLite 并发写入

SQLite 更适合：

```text
单机
中小规模
读多写少
```

不适合高并发多节点写入。

后台 Worker 推荐控制并发度，避免多个大任务同时：

```text
写 SQLite
写磁盘
执行大事务
```

KnowledgeBase 当前导入、导出、搜索索引 Worker 默认顺序执行，就是基于这一原则。

---

## 11. 事务尽量短

不要在数据库事务中执行：

```text
远程 HTTP
大文件复制
TTS/AI
压缩大型 ZIP
长时间 CPU 任务
```

推荐：

```text
读取任务 -> 标记 Running -> SaveChanges
执行外部工作
更新 Completed/Failed -> SaveChanges
```

---

## 12. 文件不要保存到 SQLite BLOB，除非数据非常小

知识库附件、ZIP、导出结果推荐：

```text
数据库保存元数据/object key
文件保存到 IFileStorage
```

不要把几十 MB 文件直接塞进 SQLite。

---

## 13. CI 强制检查

KnowledgeBase 已加入：

```text
scripts/check_sqlite_datetimeoffset.py
```

GitHub Actions 会执行：

```bash
python3 scripts/check_sqlite_datetimeoffset.py
```

发现常见的不安全写法后直接失败。

当前检查字段：

```text
CreatedAt
UpdatedAt
LastViewedAt
AccessedAt
StartedAt
CompletedAt
ReadAt
ExpiresAt
LastAccessAt
```

新项目如果新增其他 `DateTimeOffset` 字段，也应同步加入检查列表。

---

## 14. Code Review 检查清单

使用 SQLite 时，每次 Review 必须确认：

- [ ] 是否存在 `OrderBy(DateTimeOffset)`；
- [ ] 是否存在 `Where(DateTimeOffset < > <= >=)`；
- [ ] 大数据量时间排序是否使用 Unix long 字段；
- [ ] 时间是否统一使用 UTC；
- [ ] 是否使用 Migration；
- [ ] 是否有超长事务；
- [ ] 是否把大文件直接写入数据库；
- [ ] 后台任务是否控制并发；
- [ ] 是否建立必要索引；
- [ ] 分页是否在数据量可控的层面执行。

---

## 15. 所有后续 .NET + SQLite 项目默认遵守

后续任何采用：

```text
.NET
EF Core
SQLite
```

的项目，都应默认套用本规范，尤其禁止重新引入：

```csharp
.OrderBy(x => x.SomeDateTimeOffset)
.OrderByDescending(x => x.SomeDateTimeOffset)
```

发现时间排序需求时先判断数据规模：

```text
中小规模 -> LINQ to Objects 安全排序
大规模   -> Unix long 持久化排序字段
```
