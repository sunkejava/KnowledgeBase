using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 知识库全文搜索抽象。业务层只依赖该接口，底层可切换 SQLite、Meilisearch 或其他搜索引擎。
/// </summary>
public interface IKnowledgeSearchService
{
    /// <summary>
    /// 按当前用户资源权限执行分页搜索。
    /// </summary>
    Task<PageResult<SearchResultDto>> SearchAsync(
        string keyword,
        Guid userId,
        bool isSuperAdmin,
        Guid? knowledgeBaseId,
        PageQuery query,
        CancellationToken ct);

    /// <summary>
    /// 重建全文索引。SQLite 实现无需外部索引，调用时直接返回完成；外部搜索引擎实现负责重新写入索引。
    /// </summary>
    Task RebuildIndexAsync(CancellationToken ct);

    /// <summary>
    /// 返回当前搜索引擎名称，用于系统状态和前端展示。
    /// </summary>
    string ProviderName { get; }
}
