using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ExcelDataReader;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>常见文档、Markdown、HTML、Office、PDF、ZIP 导入导出与版本 Diff 服务。</summary>
public sealed class ContentExchangeService(
    KnowledgeDbContext db,
    IDocumentService documents,
    IConfiguration configuration) : IContentExchangeService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".markdown", ".html", ".htm", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv"
    };

    /// <summary>导出单篇 Markdown 文档。</summary>
    public async Task<ExportDocumentDto?> ExportMarkdownAsync(Guid documentId, CancellationToken ct)
    {
        var row = await (
            from d in db.Documents.AsNoTracking()
            join c in db.DocumentContents.AsNoTracking() on d.Id equals c.DocumentId
            where d.Id == documentId
            select new { d.Id, d.Title, d.Slug, c.Markdown }
        ).FirstOrDefaultAsync(ct);

        return row is null ? null : new(row.Id, row.Title, row.Slug, row.Markdown, $"{Sanitize(row.Title)}.md");
    }

    /// <summary>导入单篇 Markdown 文档。</summary>
    public async Task<ImportMarkdownResultDto> ImportMarkdownAsync(
        Guid knowledgeBaseId,
        Guid? parentId,
        string fileName,
        string markdown,
        CancellationToken ct)
    {
        var title = Path.GetFileNameWithoutExtension(fileName).Trim();
        if (string.IsNullOrWhiteSpace(title)) title = "导入文档";
        title = RemoveExportIdentitySuffix(title);

        var slug = $"import-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..50];
        var created = await documents.CreateAsync(new(knowledgeBaseId, parentId, title, slug, markdown), ct);
        return new(created.Id, created.Title, created.Slug);
    }

    /// <summary>导入 HTML，并转换为适合继续编辑的基础 Markdown。</summary>
    public Task<ImportMarkdownResultDto> ImportHtmlAsync(
        Guid knowledgeBaseId,
        Guid? parentId,
        string fileName,
        string html,
        CancellationToken ct)
        => ImportMarkdownAsync(knowledgeBaseId, parentId, fileName, HtmlToMarkdown(html), ct);

    /// <summary>
    /// 导入 DOCX。直接读取 Office Open XML 包中的 word/document.xml，避免依赖本机安装 Microsoft Office。
    /// 当前保留普通段落、Heading1~Heading6 标题层级和基础换行。
    /// </summary>
    public async Task<ImportMarkdownResultDto> ImportDocxAsync(
        Guid knowledgeBaseId,
        Guid? parentId,
        string fileName,
        Stream docxStream,
        CancellationToken ct)
    {
        using var archive = new ZipArchive(docxStream, ZipArchiveMode.Read, true, Encoding.UTF8);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("DOCX 中缺少 word/document.xml，文件可能已损坏或不是有效 DOCX。");

        await using var documentStream = entry.Open();
        var xml = await XDocument.LoadAsync(documentStream, LoadOptions.None, ct);
        var markdown = ConvertDocxXmlToMarkdown(xml);
        return await ImportMarkdownAsync(knowledgeBaseId, parentId, fileName, markdown, ct);
    }

    /// <summary>
    /// 统一导入常见知识文档。
    /// TXT/Markdown/HTML/CSV 直接解析文本；PDF 使用 PdfPig；XLS/XLSX 使用 ExcelDataReader；
    /// 老式 DOC 通过 LibreOffice headless 转换为文本，避免依赖 Microsoft Office COM。
    /// </summary>
    public async Task<ImportMarkdownResultDto> ImportDocumentAsync(
        Guid knowledgeBaseId,
        Guid? parentId,
        string fileName,
        Stream fileStream,
        CancellationToken ct)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
            throw new NotSupportedException($"不支持的文件格式：{extension}。支持 TXT、Markdown、HTML、PDF、DOC、DOCX、XLS、XLSX、CSV。");

        if (extension == ".docx")
            return await ImportDocxAsync(knowledgeBaseId, parentId, fileName, fileStream, ct);

        string markdown = extension switch
        {
            ".md" or ".markdown" => await ReadTextAsync(fileStream, ct),
            ".txt" => NormalizePlainText(await ReadTextAsync(fileStream, ct)),
            ".html" or ".htm" => HtmlToMarkdown(await ReadTextAsync(fileStream, ct)),
            ".csv" => CsvToMarkdown(await ReadTextAsync(fileStream, ct)),
            ".pdf" => ExtractPdfMarkdown(fileStream),
            ".doc" => await ExtractLegacyWordMarkdownAsync(fileStream, fileName, ct),
            ".xls" or ".xlsx" => ExtractWorkbookMarkdown(fileStream),
            _ => throw new NotSupportedException($"不支持的文件格式：{extension}")
        };

        if (string.IsNullOrWhiteSpace(markdown))
            throw new InvalidDataException(extension == ".pdf"
                ? "PDF 未提取到可用文本。该文件可能是扫描件或纯图片 PDF，请使用 OCR 后再导入。"
                : "文件未解析出可导入的正文内容。");

        return await ImportMarkdownAsync(knowledgeBaseId, parentId, fileName, markdown, ct);
    }

    /// <summary>
    /// 导入 Markdown ZIP，并根据目录路径恢复文档父子关系。
    /// 导出格式为 Parent-id/Child-id.md 时，目录名会自动关联已创建的父文档。
    /// </summary>
    public async Task<ImportZipResultDto> ImportMarkdownZipAsync(
        Guid knowledgeBaseId,
        Stream zipStream,
        Func<int, int, int, int, CancellationToken, Task>? progress,
        CancellationToken ct)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, true, Encoding.UTF8);
        var candidates = archive.Entries
            .Where(IsMarkdownEntry)
            .OrderBy(entry => GetDepth(entry.FullName))
            .ThenBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = candidates.Count;
        var imported = new List<ImportMarkdownResultDto>();
        var skipped = archive.Entries.Count - candidates.Count;
        var processed = 0;
        var pathDocumentMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in candidates)
        {
            ct.ThrowIfCancellationRequested();

            if (entry.Length > 20 * 1024 * 1024)
            {
                skipped++;
                processed++;
                if (progress is not null) await progress(total, processed, imported.Count, skipped, ct);
                continue;
            }

            var normalizedPath = NormalizeZipPath(entry.FullName);
            var directory = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/') ?? string.Empty;
            Guid? parentId = null;
            if (!string.IsNullOrWhiteSpace(directory))
            {
                var parentKey = directory.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
                if (pathDocumentMap.TryGetValue(parentKey, out var mappedParentId)) parentId = mappedParentId;
            }

            using var reader = new StreamReader(entry.Open(), Encoding.UTF8, true);
            var markdown = await reader.ReadToEndAsync(ct);
            var created = await ImportMarkdownAsync(knowledgeBaseId, parentId, Path.GetFileName(normalizedPath), markdown, ct);
            imported.Add(created);

            var ownKey = Path.GetFileNameWithoutExtension(normalizedPath);
            pathDocumentMap[ownKey] = created.DocumentId;
            processed++;
            if (progress is not null) await progress(total, processed, imported.Count, skipped, ct);
        }

        return new(archive.Entries.Count, imported.Count, skipped, imported);
    }

    /// <summary>对比历史版本和当前正文。</summary>
    public async Task<DocumentDiffDto?> DiffVersionToCurrentAsync(Guid versionId, CancellationToken ct)
    {
        var version = await db.DocumentVersions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == versionId, ct);
        if (version is null) return null;

        var current = await db.DocumentContents.AsNoTracking()
            .Where(x => x.DocumentId == version.DocumentId)
            .Select(x => x.Markdown)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return new(version.DocumentId, version.VersionNumber, BuildDiff(version.Markdown, current));
    }

    /// <summary>读取文本文件，并兼容 UTF-8、UTF-16 BOM 以及常见中文 GBK/GB18030 编码。</summary>
    private static async Task<string> ReadTextAsync(Stream stream, CancellationToken ct)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, ct);
        var bytes = memory.ToArray();
        if (bytes.Length == 0) return string.Empty;

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding("GB18030").GetString(bytes);
        }
    }

    /// <summary>提取文本型 PDF，并按页保留分隔标题。</summary>
    private static string ExtractPdfMarkdown(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page, true).Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (document.NumberOfPages > 1)
                builder.Append("## 第 ").Append(page.Number).AppendLine(" 页").AppendLine();
            builder.AppendLine(text).AppendLine();
        }
        return NormalizePlainText(builder.ToString());
    }

    /// <summary>
    /// 提取 Word 97-2003 DOC 正文。
    /// 使用 LibreOffice headless 做兼容转换，Docker 镜像默认内置；Windows 可通过 DocumentImport:LibreOfficePath 指定 soffice.exe。
    /// </summary>
    private async Task<string> ExtractLegacyWordMarkdownAsync(Stream stream, string fileName, CancellationToken ct)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "KnowledgeBase", "doc-import", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var inputPath = Path.Combine(tempDirectory, Sanitize(Path.GetFileName(fileName)));
        var outputPath = Path.Combine(tempDirectory, Path.GetFileNameWithoutExtension(inputPath) + ".txt");

        try
        {
            await using (var output = File.Create(inputPath))
                await stream.CopyToAsync(output, ct);

            var executable = ResolveLibreOfficeExecutable();
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--headless");
            startInfo.ArgumentList.Add("--convert-to");
            startInfo.ArgumentList.Add("txt:Text");
            startInfo.ArgumentList.Add("--outdir");
            startInfo.ArgumentList.Add(tempDirectory);
            startInfo.ArgumentList.Add(inputPath);

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("无法启动 LibreOffice 文档转换进程。");
            await process.WaitForExitAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);
            if (process.ExitCode != 0 || !File.Exists(outputPath))
                throw new InvalidDataException($"DOC 转换失败。请确认 LibreOffice 可用。{(string.IsNullOrWhiteSpace(error) ? string.Empty : $" 详情：{error.Trim()}")}");

            await using var converted = File.OpenRead(outputPath);
            return NormalizePlainText(await ReadTextAsync(converted, ct));
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidDataException(
                "当前环境未找到 LibreOffice。DOC(Word 97-2003) 导入需要 LibreOffice headless；Windows 可配置 DocumentImport:LibreOfficePath 指向 soffice.exe。",
                ex);
        }
        finally
        {
            try { Directory.Delete(tempDirectory, true); } catch { /* 临时目录清理由系统后续回收，不影响主业务。 */ }
        }
    }

    /// <summary>解析 LibreOffice 可执行文件位置。</summary>
    private string ResolveLibreOfficeExecutable()
    {
        var configured = configuration["DocumentImport:LibreOfficePath"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        if (OperatingSystem.IsWindows())
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreOffice", "program", "soffice.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "LibreOffice", "program", "soffice.exe")
            };
            var found = candidates.FirstOrDefault(File.Exists);
            if (!string.IsNullOrWhiteSpace(found)) return found;
            return "soffice.exe";
        }

        return "libreoffice";
    }

    /// <summary>读取 XLS/XLSX，并把每个工作表转换为 Markdown 表格。</summary>
    private static string ExtractWorkbookMarkdown(Stream stream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var builder = new StringBuilder();

        do
        {
            var sheetName = string.IsNullOrWhiteSpace(reader.Name) ? "工作表" : reader.Name;
            var rows = new List<string[]>();
            var maxColumns = 0;

            while (reader.Read())
            {
                maxColumns = Math.Max(maxColumns, reader.FieldCount);
                var values = new string[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                    values[i] = EscapeMarkdownCell(FormatExcelValue(reader.GetValue(i)));
                rows.Add(values);
            }

            if (rows.Count == 0 || maxColumns == 0) continue;
            builder.Append("## ").AppendLine(sheetName).AppendLine();
            AppendMarkdownRow(builder, PadRow(rows[0], maxColumns));
            AppendMarkdownRow(builder, Enumerable.Repeat("---", maxColumns));
            foreach (var row in rows.Skip(1)) AppendMarkdownRow(builder, PadRow(row, maxColumns));
            builder.AppendLine();
        } while (reader.NextResult());

        return builder.ToString().Trim();
    }

    private static IEnumerable<string> PadRow(IReadOnlyList<string> row, int count)
        => Enumerable.Range(0, count).Select(i => i < row.Count ? row[i] : string.Empty);

    private static string FormatExcelValue(object? value)
        => value switch
        {
            null => string.Empty,
            DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss"),
            double number => number.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
            float number => number.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
            decimal number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };

    /// <summary>将 CSV 转换为 Markdown 表格，支持双引号转义和字段内逗号。</summary>
    private static string CsvToMarkdown(string csv)
    {
        var rows = ParseCsv(csv).Where(row => row.Count > 0).ToList();
        if (rows.Count == 0) return string.Empty;
        var maxColumns = rows.Max(x => x.Count);
        var builder = new StringBuilder();

        AppendMarkdownRow(builder, NormalizeCsvRow(rows[0], maxColumns));
        AppendMarkdownRow(builder, Enumerable.Repeat("---", maxColumns));
        foreach (var row in rows.Skip(1)) AppendMarkdownRow(builder, NormalizeCsvRow(row, maxColumns));
        return builder.ToString().Trim();
    }

    private static IEnumerable<string> NormalizeCsvRow(IReadOnlyList<string> row, int count)
        => Enumerable.Range(0, count).Select(i => EscapeMarkdownCell(i < row.Count ? row[i] : string.Empty));

    private static List<List<string>> ParseCsv(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var ch = csv[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else quoted = !quoted;
            }
            else if (ch == ',' && !quoted)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if ((ch == '\r' || ch == '\n') && !quoted)
            {
                if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                row.Add(field.ToString());
                field.Clear();
                if (row.Any(value => !string.IsNullOrWhiteSpace(value))) rows.Add(row);
                row = new List<string>();
            }
            else field.Append(ch);
        }

        row.Add(field.ToString());
        if (row.Any(value => !string.IsNullOrWhiteSpace(value))) rows.Add(row);
        return rows;
    }

    private static void AppendMarkdownRow(StringBuilder builder, IEnumerable<string> values)
        => builder.Append("| ").Append(string.Join(" | ", values)).AppendLine(" |");

    private static string EscapeMarkdownCell(string? value)
        => (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ").Trim();

    private static string NormalizePlainText(string text)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\0", string.Empty);
        text = Regex.Replace(text, "[ \\t]+\\n", "\n");
        text = Regex.Replace(text, "\\n{3,}", "\n\n");
        return text.Trim();
    }

    private static string ConvertDocxXmlToMarkdown(XDocument xml)
    {
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var builder = new StringBuilder();

        foreach (var paragraph in xml.Descendants(w + "p"))
        {
            var style = paragraph
                .Element(w + "pPr")?
                .Element(w + "pStyle")?
                .Attribute(w + "val")?
                .Value ?? string.Empty;

            var text = string.Concat(paragraph.Descendants(w + "t").Select(x => x.Value)).Trim();
            if (text.Length == 0)
            {
                if (builder.Length > 0 && !builder.ToString().EndsWith("\n\n", StringComparison.Ordinal)) builder.AppendLine();
                continue;
            }

            var headingLevel = ParseHeadingLevel(style);
            if (headingLevel > 0)
                builder.Append(new string('#', headingLevel)).Append(' ').AppendLine(text).AppendLine();
            else
                builder.AppendLine(text).AppendLine();
        }

        return Regex.Replace(builder.ToString().Trim(), "\\n{3,}", "\n\n");
    }

    private static int ParseHeadingLevel(string style)
    {
        if (string.IsNullOrWhiteSpace(style)) return 0;
        var match = Regex.Match(style, "Heading([1-6])", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var level) ? level : 0;
    }

    private static bool IsMarkdownEntry(ZipArchiveEntry entry)
    {
        if (entry.Length == 0) return false;
        var ext = Path.GetExtension(entry.FullName);
        return ext.Equals(".md", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".markdown", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetDepth(string path)
        => NormalizeZipPath(path).Count(c => c == '/');

    private static string NormalizeZipPath(string path)
        => path.Replace('\\', '/').TrimStart('/');

    private static string RemoveExportIdentitySuffix(string title)
        => Regex.Replace(title, "-[0-9a-fA-F]{32}$", string.Empty);

    private static string HtmlToMarkdown(string html)
    {
        var text = html ?? string.Empty;
        text = Regex.Replace(text, "<script[\\s\\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<style[\\s\\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<h1[^>]*>([\\s\\S]*?)</h1>", "# $1\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<h2[^>]*>([\\s\\S]*?)</h2>", "## $1\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<h3[^>]*>([\\s\\S]*?)</h3>", "### $1\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<li[^>]*>([\\s\\S]*?)</li>", "- $1\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<(br|/p|/div|/section)>\s*", "\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", string.Empty);
        text = WebUtility.HtmlDecode(text);
        return NormalizePlainText(text);
    }

    private static IReadOnlyList<DiffLineDto> BuildDiff(string oldText, string newText)
    {
        var a = oldText.Replace("\r", string.Empty).Split('\n').Take(2000).ToArray();
        var b = newText.Replace("\r", string.Empty).Split('\n').Take(2000).ToArray();
        var n = a.Length;
        var m = b.Length;
        var dp = new int[n + 1, m + 1];
        for (var i = n - 1; i >= 0; i--)
            for (var j = m - 1; j >= 0; j--)
                dp[i, j] = a[i] == b[j] ? dp[i + 1, j + 1] + 1 : Math.Max(dp[i + 1, j], dp[i, j + 1]);

        var result = new List<DiffLineDto>();
        var x = 0;
        var y = 0;
        var oldLine = 1;
        var newLine = 1;
        while (x < n || y < m)
        {
            if (x < n && y < m && a[x] == b[y])
            {
                result.Add(new("equal", oldLine++, newLine++, a[x]));
                x++;
                y++;
            }
            else if (y < m && (x == n || dp[x, y + 1] >= dp[x + 1, y]))
            {
                result.Add(new("add", 0, newLine++, b[y++]));
            }
            else if (x < n)
            {
                result.Add(new("delete", oldLine++, 0, a[x++]));
            }
        }

        return result;
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "document" : name;
    }
}
