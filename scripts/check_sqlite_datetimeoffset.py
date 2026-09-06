#!/usr/bin/env python3
"""SQLite DateTimeOffset 查询静态检查。

SQLite 的 EF Core provider 不能可靠翻译 DateTimeOffset 的排序和范围比较。
本脚本用于阻止业务代码再次直接对常见 DateTimeOffset 字段执行 ORDER BY 或 Where 范围比较。

规则：
1. 禁止 OrderBy/OrderByDescending/ThenBy/ThenByDescending 直接引用常见时间字段；
2. 禁止 Where 表达式直接对常见时间字段使用 <、>、<=、>=；
3. 需要排序时使用 QueryablePagingExtensions 中的 SQLite 安全扩展；
4. 超大数据量场景应新增 Unix 毫秒 long 排序列，在数据库层排序分页。
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

violations: list[tuple[pathlib.Path, int, str]] = []

for path in BACKEND.rglob("*.cs"):
    # Migration 是数据库历史脚本，不参与业务 LINQ 查询检查。
    if "Migrations" in path.parts:
        continue

    for line_no, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        stripped = line.strip()
        if stripped.startswith("//") or stripped.startswith("///"):
            continue
        if ORDER_RE.search(line) or QUERY_ORDER_RE.search(line) or WHERE_COMPARE_RE.search(line):
            violations.append((path.relative_to(ROOT), line_no, stripped))

if violations:
    print("发现 SQLite 不安全的 DateTimeOffset LINQ 查询：")
    for path, line_no, line in violations:
        print(f"  {path}:{line_no}: {line}")
    print("\n请使用 ToSqliteSafeDateTimeOffsetPageAsync / ToSqliteSafeDateTimeOffsetListAsync，")
    print("或为大数据量表增加 Unix 毫秒 long 排序列后在数据库层排序。")
    sys.exit(1)

print("SQLite DateTimeOffset 查询检查通过。")
