using KnowledgeBase.Application.Abstractions;

namespace KnowledgeBase.Api.BackgroundServices;

/// <summary>
/// 导出任务后台处理器。按固定间隔领取一个 Pending 任务处理，避免大导出阻塞接口线程。
/// </summary>
public sealed class ExportTaskWorker(IServiceScopeFactory scopeFactory,ILogger<ExportTaskWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope=scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IExportTaskService>().ProcessNextPendingAsync(stoppingToken);
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"导出任务后台处理异常");}
            await Task.Delay(TimeSpan.FromSeconds(2),stoppingToken);
        }
    }
}
