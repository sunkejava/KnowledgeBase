using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 服务端导出任务服务。创建任务后由后台 Worker 异步处理，避免大数据导出阻塞 HTTP 请求。
/// </summary>
public interface IExportTaskService
{
    Task<ExportTaskDto> CreateAsync(Guid userId, Guid knowledgeBaseId, string format, CancellationToken ct);
    Task<PageResult<ExportTaskDto>> GetPageAsync(Guid userId, int page, int pageSize, CancellationToken ct);
    Task<(Stream Stream, string FileName)?> OpenFileAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct);
    Task<bool> CancelAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct);
    Task<bool> RetryAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct);
    Task<int> CleanupAsync(Guid userId, bool isSuperAdmin, int olderThanDays, CancellationToken ct);
    Task ProcessNextPendingAsync(CancellationToken ct);
}
