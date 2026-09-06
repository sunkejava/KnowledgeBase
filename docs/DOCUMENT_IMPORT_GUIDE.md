# KnowledgeBase 常见文档导入说明

适用版本：`v0.14.3+`

KnowledgeBase 将外部文档统一解析为可编辑、可检索、可版本化的 Markdown 文档，再进入现有知识资产体系。

## 1. 支持格式

| 格式 | 扩展名 | 解析方式 | 当前保留内容 |
| --- | --- | --- | --- |
| 纯文本 | `.txt` | 内置文本解析器 | 正文、换行 |
| Markdown | `.md` `.markdown` | 原样读取 | Markdown 原结构 |
| HTML | `.html` `.htm` | 内置 HTML→Markdown | 标题、列表、段落、纯文本 |
| PDF | `.pdf` | PdfPig | 文本型 PDF 正文、分页提示 |
| Word Open XML | `.docx` | 直接读取 Open XML | 正文、Heading1~Heading6 |
| Word 97-2003 | `.doc` | LibreOffice headless | 正文文本 |
| Excel 97-2003 | `.xls` | ExcelDataReader | 工作表、单元格、Markdown 表格 |
| Excel Open XML | `.xlsx` | ExcelDataReader | 工作表、单元格、Markdown 表格 |
| CSV | `.csv` | 内置 CSV 解析器 | 表头、记录、Markdown 表格 |
| Markdown ZIP | `.zip` | 后台导入任务 | Markdown 文档及目录层级 |

统一接口：

```http
POST /api/content-exchange/knowledge-bases/{knowledgeBaseId}/document
Content-Type: multipart/form-data
```

可选参数：

```text
parentId
```

指定后，导入文档会创建为该文档的子文档。

支持格式查询：

```http
GET /api/content-exchange/supported-formats
```

---

## 2. 前端入口

### 知识库工作区

进入：

```text
我的知识库 → 进入知识库 → 导入
```

支持：

```text
TXT
Markdown
HTML
PDF
DOC
DOCX
XLS
XLSX
CSV
```

行为：

- 当前没有选中文档：导入为知识库根文档。
- 当前选中文档：导入为当前文档的子文档。
- 导入成功后自动刷新目录并打开新文档。

### 数据交换

进入：

```text
平台 → 数据交换 → 导入常见文档
```

支持一次选择多个文件。

前端会逐个提交，单个文件失败不会阻止后续文件继续导入，并汇总成功数量与失败文件名。

Markdown ZIP 仍使用异步导入任务，适合大批量知识迁移。

---

## 3. TXT 编码处理

文本导入支持：

```text
UTF-8
UTF-8 BOM
UTF-16 LE
UTF-16 BE
GBK / GB18030 fallback
```

解析顺序：

1. 检测 BOM。
2. 严格 UTF-8 解码。
3. UTF-8 无法解码时回退 GB18030。

这主要解决 Windows 历史 TXT、中文资料导入乱码问题。

---

## 4. PDF

PDF 使用：

```text
PdfPig 0.1.16
```

当前定位是**文本型 PDF 导入**。

会：

- 提取每页可选择文字。
- 多页 PDF 增加“第 N 页”Markdown 二级标题。
- 将正文写入知识文档。

不会自动做：

- OCR。
- 扫描图片文字识别。
- 原 PDF 页面布局 1:1 复刻。
- 图片提取。

如果 PDF 是扫描件或纯图片，后端会明确返回：

```text
PDF 未提取到可用文本。该文件可能是扫描件或纯图片 PDF，请使用 OCR 后再导入。
```

后续 OCR 应作为可选能力接入，不应让普通 PDF 导入强依赖 OCR 环境。

---

## 5. DOCX

DOCX 不依赖 Microsoft Office。

后端直接读取：

```text
word/document.xml
```

当前支持：

- 普通段落。
- Heading1 ~ Heading6。
- 基础换行。

标题会转换为：

```markdown
# 一级标题
## 二级标题
### 三级标题
```

当前尚未完整转换：

- 图片。
- 浮动对象。
- SmartArt。
- 页眉页脚。
- 复杂表格样式。
- 批注/修订。

后续可继续读取 DOCX relationship/media/table 节点完善。

---

## 6. DOC（Word 97-2003）

老式 `.doc` 是 OLE 二进制格式，与 `.docx` 完全不同。

为避免：

- Microsoft Office COM 依赖。
- Windows-only 服务。
- 老旧 HWPF/ScratchPad 包。
- 不清晰的第三方授权依赖。

当前使用：

```text
LibreOffice headless
```

流程：

```text
.doc
  ↓
临时文件
  ↓
LibreOffice --headless --convert-to txt:Text
  ↓
TXT
  ↓
Markdown 文档
```

### Docker

后端 Docker 镜像已经安装：

```text
libreoffice-writer
```

因此标准 Docker 部署不需要额外处理。

### Windows 本地运行

推荐安装 LibreOffice。

如果：

```text
soffice.exe
```

没有加入 PATH，可配置：

```json
"DocumentImport": {
  "LibreOfficePath": "C:\\Program Files\\LibreOffice\\program\\soffice.exe"
}
```

未安装时只有 `.doc` 受影响，TXT/PDF/DOCX/Excel 等格式不受影响。

---

## 7. Excel

`.xls` 和 `.xlsx` 使用：

```text
ExcelDataReader 3.9.0
```

每个 Sheet 转换成：

```markdown
## Sheet1

| 姓名 | 部门 | 状态 |
| --- | --- | --- |
| 张三 | 研发 | 在职 |
| 李四 | 运维 | 在职 |
```

规则：

- 每个 Sheet 独立二级标题。
- 第一行作为 Markdown 表头。
- 其余行作为数据行。
- 自动处理不同列数。
- `|` 自动转义。
- 单元格换行转换为空格。
- 日期和数字转换为可读文本。
- `.xls` 历史编码通过 `System.Text.Encoding.CodePages` 支持。

目前目标是知识检索与可读性，而不是还原 Excel 的：

- 单元格样式。
- 颜色。
- 合并单元格视觉结构。
- 图表。
- 宏。
- 公式计算引擎。

---

## 8. CSV

内置 CSV 解析器支持：

- 英文逗号分隔。
- 双引号包裹字段。
- 字段内逗号。
- `""` 双引号转义。
- 字段内换行。

再统一转换成 Markdown 表格。

---

## 9. 权限

统一文档导入不会绕过资源权限。

要求：

```text
当前用户对知识库拥有 Editor / Manager
```

如果指定：

```text
parentId
```

还要求当前用户拥有目标父文档编辑权限。

超级管理员保持全资源访问。

---

## 10. 文件大小

统一单文档接口当前限制：

```text
100 MB
```

Markdown ZIP：

```text
200 MB
```

大量文件或超大文件后续建议继续扩展现有 `Sys_ImportTask + ImportTaskWorker`，不要让大型解析任务长期占用 HTTP 请求。

---

## 11. 架构约束

业务页面不得为每个格式分别实现接口。

统一调用：

```text
exchangeApi.importDocument
或
documentApi.importDocument
```

后端统一入口：

```text
IContentExchangeService.ImportDocumentAsync
```

解析逻辑只能集中在内容交换层或后续独立的 `IDocumentParser` 实现中，禁止散落在 Controller。

后续格式继续扩展时建议演进为：

```text
IDocumentParser
├─ TextDocumentParser
├─ HtmlDocumentParser
├─ PdfDocumentParser
├─ DocxDocumentParser
├─ LegacyDocDocumentParser
├─ ExcelDocumentParser
└─ CsvDocumentParser
```

Controller 与知识库业务不需要感知解析库细节。
