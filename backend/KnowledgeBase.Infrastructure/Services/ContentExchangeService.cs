using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>Markdown、HTML、DOCX、ZIP 导入导出与版本 Diff 服务。</summary>
public sealed class ContentExchangeService(KnowledgeDbContext db, IDocumentService documents) : IContentExchangeService
{
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
    /// 导入 DOCX。直接读取 Office Open XML 包中的 word/document.xml，避免引入重量级 Office 运行时依赖。
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
        text = Regex.Replace(text, "[ \\t]+\\n", "\n");
        text = Regex.Replace(text, "\\n{3,}", "\n\n");
        return text.Trim();
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
