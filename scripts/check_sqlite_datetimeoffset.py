#!/usr/bin/env python3
"""SQLite DateTimeOffset 查询静态检查。

SQLite 的 EF Core provider 不能可靠翻译 DateTimeOffset 的排序和范围比较。
本脚本用于阻止业务代码再次直接对常见 DateTimeOffset 字段执行 ORDER BY 或 IQueryable 范围比较。

规则：
1. 禁止 EF 查询链直接 OrderBy/OrderByDescending/ThenBy/ThenByDescending 常见时间字段；
2. 禁止 IQueryable 查询变量直接对常见时间字段使用 <、>、<=、>=；
3. 已经 ToListAsync 后的 List/Enumerable 内存排序和范围比较允许；
4. 需要排序时优先使用 QueryablePagingExtensions 中的 SQLite 安全扩展；
5. 超大数据量场景应新增 Unix 毫秒 long 排序列，在数据库层排序分页。
"""

from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
BACKEND = ROOT / "backend"

TIME_FIELDS = (
    "CreatedAt",
    "UpdatedAt",
    "LastViewedAt",
    "AccessedAt",
    "StartedAt",
    "CompletedAt",
    "ReadAt",
    "ExpiresAt",
    "LastAccessAt",
)
FIELD_GROUP = "|".join(TIME_FIELDS)

ORDER_RE = re.compile(
    rf"\b(?:OrderBy|OrderByDescending|ThenBy|ThenByDescending)\s*\([^\n;]*\.({FIELD_GROUP})\b"
)
QUERY_ORDER_RE = re.compile(rf"\borderby\s+[^\n;]*\.({FIELD_GROUP})\b", re.IGNORECASE)
WHERE_COMPARE_RE = re.compile(
    rf"\.Where\s*\([^\n;]*\.({FIELD_GROUP})\s*(?:<=|>=|<|>)"
)

# 明确表示已经物化到内存的常用局部变量名称。此类 Enumerable 操作是本项目推荐的 SQLite 兼容处理方式。
MATERIALIZED_PREFIXES = (
    "rows.",
    "items.",
    "candidates.",
    "comments.",
    "users.",
    "links.",
    "roles.",
)

# SQLite 安全扩展方法内部本身必须使用 LINQ-to-Objects 排序，因此不参与规则扫描。
EXCLUDED_FILES = {
    pathlib.Path("backend/KnowledgeBase.Infrastructure/Common/QueryablePagingExtensions.cs"),
}

violations: list[tuple[pathlib.Path, int, str]] = []

for path in BACKEND.rglob("*.cs"):
    relative = path.relative_to(ROOT)
    if "Migrations" in path.parts or relative in EXCLUDED_FILES:
        continue

    for line_no, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        stripped = line.strip()
        if stripped.startswith("//") or stripped.startswith("///"):
            continue

        order_match = ORDER_RE.search(line) or QUERY_ORDER_RE.search(line)
        where_match = WHERE_COMPARE_RE.search(line)

        # `candidates.Where(...)`、`rows.OrderBy(...)` 等是 ToListAsync 后的 Enumerable 操作，不属于 SQLite SQL 翻译。
        is_materialized_operation = stripped.startswith(MATERIALIZED_PREFIXES) or any(
            f" {prefix}" in stripped for prefix in MATERIALIZED_PREFIXES
        )

        if order_match and not is_materialized_operation:
            violations.append((relative, line_no, stripped))
        elif where_match and not is_materialized_operation:
            violations.append((relative, line_no, stripped))

if violations:
    print("发现 SQLite 不安全的 DateTimeOffset IQueryable 查询：")
    for path, line_no, line in violations:
        print(f"  {path}:{line_no}: {line}")
    print("\n中小数据量请使用 SQLite 安全扩展，或先 ToListAsync 后使用 LINQ-to-Objects；")
    print("大数据量请增加 Unix 毫秒 long 排序/过滤列后在数据库层处理。")
    sys.exit(1)

print("SQLite DateTimeOffset 查询检查通过。")
