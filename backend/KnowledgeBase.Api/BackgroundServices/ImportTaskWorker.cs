using KnowledgeBase.Application.Abstractions;

namespace KnowledgeBase.Api.BackgroundServices;

/// <summary>
/// 知识库导入任务后台执行器。按队列顺序处理待执行任务，避免大文件导入阻塞 HTTP 请求。
/// </summary>
public sealed class ImportTaskWorker(IServiceScopeFactory scopeFactory, ILogger<ImportTaskWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IImportTaskService>();
                await service.ProcessNextPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "导入任务后台处理异常");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
