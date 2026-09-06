using KnowledgeBase.Application.Abstractions;

namespace KnowledgeBase.Api.BackgroundServices;

/// <summary>
/// 搜索索引后台任务。顺序执行索引重建，避免多个全量索引任务同时运行。
/// </summary>
public sealed class SearchIndexTaskWorker(IServiceScopeFactory scopeFactory, ILogger<SearchIndexTaskWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ISearchIndexTaskService>();
                await service.ProcessNextPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "执行搜索索引后台任务时发生异常");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
