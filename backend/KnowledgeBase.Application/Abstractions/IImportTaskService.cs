using KnowledgeBase.Contracts.Common;
using KnowledgeBase.Contracts.Knowledge;

namespace KnowledgeBase.Application.Abstractions;

/// <summary>
/// 服务端导入任务服务。负责上传文件落盘、任务状态维护、后台处理、取消、重试与历史清理。
/// </summary>
public interface IImportTaskService
{
    Task<ImportTaskDto> CreateZipAsync(Guid userId, Guid knowledgeBaseId, string fileName, Stream stream, CancellationToken ct);
    Task<PageResult<ImportTaskDto>> GetPageAsync(Guid userId, int page, int pageSize, CancellationToken ct);
    Task<bool> CancelAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct);
    Task<bool> RetryAsync(Guid taskId, Guid userId, bool isSuperAdmin, CancellationToken ct);
    Task<int> CleanupAsync(Guid userId, bool isSuperAdmin, int olderThanDays, CancellationToken ct);
    Task ProcessNextPendingAsync(CancellationToken ct);
}
