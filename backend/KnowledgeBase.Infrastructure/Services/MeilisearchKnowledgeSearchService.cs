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
            filter = filters.Count == 0 ? null : string.Join(" AND ", filters),
            attributesToHighlight = new[] { "title", "markdown" },
            highlightPreTag = "<mark>",
            highlightPostTag = "</mark>"
        };

        using var response = await client.PostAsJsonAsync($"{_endpoint}/indexes/{Uri.EscapeDataString(_indexName)}/search", payload, ct);
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

    /// <summary>
    /// 清空并重新写入全部文档索引，同时配置可检索字段和知识库过滤字段。
    /// </summary>
    public async Task RebuildIndexAsync(CancellationToken ct)
    {
        var rows = await (
            from document in db.Documents.AsNoTracking()
            join content in db.DocumentContents.AsNoTracking() on document.Id equals content.DocumentId
            select new
            {
                id = document.Id.ToString("D"),
                knowledgeBaseId = document.KnowledgeBaseId.ToString("D"),
                title = document.Title,
                markdown = content.Markdown,
                updatedAt = document.UpdatedAt.ToString("O")
            }).ToListAsync(ct);

        using var client = CreateClient();
        await SendAndEnsureAsync(client, HttpMethod.Delete, $"{_endpoint}/indexes/{Uri.EscapeDataString(_indexName)}/documents", null, ct, allowNotFound: true);
        await SendAndEnsureAsync(client, HttpMethod.Put, $"{_endpoint}/indexes/{Uri.EscapeDataString(_indexName)}/settings/searchable-attributes", new[] { "title", "markdown" }, ct);
        await SendAndEnsureAsync(client, HttpMethod.Put, $"{_endpoint}/indexes/{Uri.EscapeDataString(_indexName)}/settings/filterable-attributes", new[] { "knowledgeBaseId" }, ct);

        const int batchSize = 500;
        for (var i = 0; i < rows.Count; i += batchSize)
        {
            var batch = rows.Skip(i).Take(batchSize).ToArray();
            await SendAndEnsureAsync(client, HttpMethod.Put,
                $"{_endpoint}/indexes/{Uri.EscapeDataString(_indexName)}/documents?primaryKey=id", batch, ct);
        }
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("meilisearch");
        if (!string.IsNullOrWhiteSpace(_apiKey))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return client;
    }

    private static async Task SendAndEnsureAsync(HttpClient client, HttpMethod method, string url, object? body, CancellationToken ct, bool allowNotFound = false)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request, ct);
        if (allowNotFound && response.StatusCode == System.Net.HttpStatusCode.NotFound) return;
        response.EnsureSuccessStatusCode();
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
