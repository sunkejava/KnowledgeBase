using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KnowledgeBase.Infrastructure.Services;

/// <summary>
/// Meilisearch 搜索实现。通过 HTTP API 接入，不绑定第三方 SDK，便于后续独立升级 Meilisearch 服务端。
/// </summary>
public sealed class MeilisearchKnowledgeSearchService(
    KnowledgeDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IKnowledgeSearchService
{
    private readonly string _endpoint = (configuration["Search:Meilisearch:Endpoint"] ?? "http://127.0.0.1:7700").TrimEnd('/');
    private readonly string _apiKey = configuration["Search:Meilisearch:ApiKey"] ?? string.Empty;
    private readonly string _indexName = configuration["Search:Meilisearch:IndexName"] ?? "knowledge_documents";

    public string ProviderName => "meilisearch";

    /// <summary>
    /// 使用 Meilisearch 执行全文检索，并通过知识库 ID 过滤确保资源权限不越界。
    /// </summary>
    public async Task<PageResult<SearchResultDto>> SearchAsync(
        string keyword,
        Guid userId,
        bool isSuperAdmin,
        Guid? knowledgeBaseId,
        PageQuery query,
        CancellationToken ct)
    {
        keyword = (keyword ?? string.Empty).Trim();
        if (keyword.Length == 0)
            return new PageResult<SearchResultDto>([], 0, query.NormalizedPage, query.NormalizedPageSize);

        var allowedIds = new List<Guid>();
        if (!isSuperAdmin)
        {
            allowedIds = await db.KnowledgeBaseMembers.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.KnowledgeBaseId)
                .ToListAsync(ct);
            if (allowedIds.Count == 0)
                return new PageResult<SearchResultDto>([], 0, query.NormalizedPage, query.NormalizedPageSize);
        }

        var filters = new List<string>();
        if (knowledgeBaseId.HasValue)
        {
            if (!isSuperAdmin && !allowedIds.Contains(knowledgeBaseId.Value))
                return new PageResult<SearchResultDto>([], 0, query.NormalizedPage, query.NormalizedPageSize);
            filters.Add($"knowledgeBaseId = '{knowledgeBaseId.Value:D}'");
        }
        else if (!isSuperAdmin)
        {
            filters.Add("knowledgeBaseId IN [" + string.Join(',', allowedIds.Select(x => $"'{x:D}'")) + "]");
        }

        using var client = CreateClient();
        var payload = new
        {
            q = keyword,
            offset = (query.NormalizedPage - 1) * query.NormalizedPageSize,
            limit = query.NormalizedPageSize,
            filter = filters.Count == 0 ? null : string.Join(" AND ", filters)
        };

        using var response = await client.PostAsJsonAsync($"{IndexUrl}/search", payload, ct);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var root = document.RootElement;
        var total = root.TryGetProperty("estimatedTotalHits", out var totalNode) ? totalNode.GetInt64() : 0;
        var items = new List<SearchResultDto>();

        if (root.TryGetProperty("hits", out var hits))
        {
            foreach (var hit in hits.EnumerateArray())
            {
                var id = Guid.Parse(hit.GetProperty("id").GetString()!);
                var kbId = Guid.Parse(hit.GetProperty("knowledgeBaseId").GetString()!);
                var title = hit.GetProperty("title").GetString() ?? string.Empty;
                var markdown = hit.TryGetProperty("markdown", out var markdownNode) ? markdownNode.GetString() ?? string.Empty : string.Empty;
                var updatedAt = hit.TryGetProperty("updatedAt", out var updatedNode) && DateTimeOffset.TryParse(updatedNode.GetString(), out var parsed)
                    ? parsed
                    : DateTimeOffset.MinValue;
                items.Add(new SearchResultDto(id, kbId, title, BuildSnippet(markdown, keyword), updatedAt));
            }
        }

        return new PageResult<SearchResultDto>(items, total, query.NormalizedPage, query.NormalizedPageSize);
    }

    /// <summary>新增或更新单篇文档索引，并等待 Meilisearch 内部任务真正执行完成。</summary>
    public async Task UpsertDocumentAsync(Guid documentId, CancellationToken ct)
    {
        var raw = await (
            from document in db.Documents.AsNoTracking()
            join content in db.DocumentContents.AsNoTracking() on document.Id equals content.DocumentId
            where document.Id == documentId
            select new
            {
                document.Id,
                document.KnowledgeBaseId,
                document.Title,
                content.Markdown,
                document.UpdatedAt
            }).FirstOrDefaultAsync(ct);

        if (raw is null)
        {
            await DeleteDocumentAsync(documentId, ct);
            return;
        }

        var payload = new[]
        {
            new
            {
                id = raw.Id.ToString("D"),
                knowledgeBaseId = raw.KnowledgeBaseId.ToString("D"),
                title = raw.Title,
                markdown = raw.Markdown,
                updatedAt = raw.UpdatedAt.ToString("O")
            }
        };

        using var client = CreateClient();
        await EnsureIndexAsync(client, configureWhenCreated: true, ct);
        var taskUid = await SendTaskAsync(client, HttpMethod.Put, $"{IndexUrl}/documents?primaryKey=id", payload, ct);
        await WaitTaskAsync(client, taskUid, ct);
    }

    /// <summary>删除单篇文档索引，并等待 Meilisearch 内部删除任务完成。</summary>
    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken ct)
    {
        using var client = CreateClient();
        var taskUid = await SendTaskAsync(
            client,
            HttpMethod.Delete,
            $"{IndexUrl}/documents/{documentId:D}",
            null,
            ct,
            allowNotFound: true);
        if (taskUid >= 0) await WaitTaskAsync(client, taskUid, ct);
    }

    /// <summary>
    /// 清空并重新写入全部文档索引，同时配置可检索字段和知识库过滤字段。
    /// 每个 Meilisearch 异步任务都会等待到 succeeded，只有真实完成后本地索引任务才会标记 Completed。
    /// </summary>
    public async Task RebuildIndexAsync(CancellationToken ct)
    {
        // EF Core 查询阶段只读取数据库原始类型，Guid/DateTimeOffset 格式化统一放到内存中执行，避免 SQLite 翻译差异。
        var rawRows = await (
            from document in db.Documents.AsNoTracking()
            join content in db.DocumentContents.AsNoTracking() on document.Id equals content.DocumentId
            select new
            {
                document.Id,
                document.KnowledgeBaseId,
                document.Title,
                content.Markdown,
                document.UpdatedAt
            }).ToListAsync(ct);

        var rows = rawRows.Select(row => new
        {
            id = row.Id.ToString("D"),
            knowledgeBaseId = row.KnowledgeBaseId.ToString("D"),
            title = row.Title,
            markdown = row.Markdown,
            updatedAt = row.UpdatedAt.ToString("O")
        }).ToList();

        using var client = CreateClient();
        await EnsureIndexAsync(client, configureWhenCreated: false, ct);

        var clearTask = await SendTaskAsync(client, HttpMethod.Delete, $"{IndexUrl}/documents", null, ct, allowNotFound: true);
        if (clearTask >= 0) await WaitTaskAsync(client, clearTask, ct);

        var searchableTask = await SendTaskAsync(
            client,
            HttpMethod.Put,
            $"{IndexUrl}/settings/searchable-attributes",
            new[] { "title", "markdown" },
            ct);
        await WaitTaskAsync(client, searchableTask, ct);

        var filterableTask = await SendTaskAsync(
            client,
            HttpMethod.Put,
            $"{IndexUrl}/settings/filterable-attributes",
            new[] { "knowledgeBaseId" },
            ct);
        await WaitTaskAsync(client, filterableTask, ct);

        const int batchSize = 500;
        for (var i = 0; i < rows.Count; i += batchSize)
        {
            var batch = rows.Skip(i).Take(batchSize).ToArray();
            var batchTask = await SendTaskAsync(client, HttpMethod.Put, $"{IndexUrl}/documents?primaryKey=id", batch, ct);
            await WaitTaskAsync(client, batchTask, ct);
        }
    }

    private string IndexUrl => $"{_endpoint}/indexes/{Uri.EscapeDataString(_indexName)}";

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("meilisearch");
        if (!string.IsNullOrWhiteSpace(_apiKey))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return client;
    }

    /// <summary>
    /// 确认索引存在。首次创建时同步配置检索字段和权限过滤字段，避免增量写入先创建出不可过滤的索引。
    /// </summary>
    private async Task EnsureIndexAsync(HttpClient client, bool configureWhenCreated, CancellationToken ct)
    {
        using var response = await client.GetAsync(IndexUrl, ct);
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
            return;
        }

        var createTask = await SendTaskAsync(client, HttpMethod.Post, $"{_endpoint}/indexes", new { uid = _indexName, primaryKey = "id" }, ct);
        await WaitTaskAsync(client, createTask, ct);

        if (!configureWhenCreated) return;

        var searchableTask = await SendTaskAsync(
            client,
            HttpMethod.Put,
            $"{IndexUrl}/settings/searchable-attributes",
            new[] { "title", "markdown" },
            ct);
        await WaitTaskAsync(client, searchableTask, ct);

        var filterableTask = await SendTaskAsync(
            client,
            HttpMethod.Put,
            $"{IndexUrl}/settings/filterable-attributes",
            new[] { "knowledgeBaseId" },
            ct);
        await WaitTaskAsync(client, filterableTask, ct);
    }

    /// <summary>发送会产生 Meilisearch taskUid 的写操作。</summary>
    private static async Task<long> SendTaskAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body,
        CancellationToken ct,
        bool allowNotFound = false)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request, ct);
        if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound) return -1;
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        if (!json.RootElement.TryGetProperty("taskUid", out var uidNode) || !uidNode.TryGetInt64(out var taskUid))
            throw new InvalidDataException("Meilisearch 响应中缺少 taskUid，无法确认索引操作是否真正完成。");
        return taskUid;
    }

    /// <summary>等待指定 Meilisearch 内部任务完成，失败或取消时直接抛出异常。</summary>
    private async Task WaitTaskAsync(HttpClient client, long taskUid, CancellationToken ct)
    {
        if (taskUid < 0) return;
        var deadline = DateTimeOffset.UtcNow.AddMinutes(5);

        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await client.GetAsync($"{_endpoint}/tasks/{taskUid}", ct);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            var root = json.RootElement;
            var status = root.TryGetProperty("status", out var statusNode) ? statusNode.GetString() : null;

            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase)) return;
            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "canceled", StringComparison.OrdinalIgnoreCase))
            {
                var message = root.TryGetProperty("error", out var errorNode)
                              && errorNode.TryGetProperty("message", out var messageNode)
                    ? messageNode.GetString()
                    : null;
                throw new InvalidOperationException($"Meilisearch 任务 {taskUid} 执行失败：{message ?? status ?? "未知错误"}");
            }

            await Task.Delay(200, ct);
        }

        throw new TimeoutException($"等待 Meilisearch 任务 {taskUid} 完成超时。");
    }

    private static string BuildSnippet(string text, string keyword)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return text[..Math.Min(text.Length, 180)];
        var start = Math.Max(0, index - 70);
        var length = Math.Min(text.Length - start, 180);
        return text.Substring(start, length).Replace("\r", " ").Replace("\n", " ");
    }
}
