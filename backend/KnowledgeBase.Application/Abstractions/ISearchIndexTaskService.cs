using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 搜索索引任务服务。负责创建、查询、重试和执行持久化索引重建任务。
/// </summary>
public interface ISearchIndexTaskService
{
    Task<SearchIndexTaskDto> CreateAsync(Guid userId, CancellationToken ct);
    Task<PageResult<SearchIndexTaskDto>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    Task<bool> RetryAsync(Guid taskId, CancellationToken ct);
    Task<int> CleanupAsync(int retentionDays, CancellationToken ct);
    Task ProcessNextPendingAsync(CancellationToken ct);
    SearchProviderStatusDto GetProviderStatus();
}
