using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Api.Workflow.Runtime;

public sealed class WorkflowBackgroundProcessingService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<WorkflowBackgroundProcessingOptions> options,
    ILogger<WorkflowBackgroundProcessingService> logger) : BackgroundService
{
    private readonly WorkflowBackgroundProcessingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Workflow background processing is disabled.");
            return;
        }

        if (_options.PollIntervalSeconds <= 0)
        {
            logger.LogWarning("Workflow background processing is enabled, but PollIntervalSeconds is invalid: {PollIntervalSeconds}", _options.PollIntervalSeconds);
            return;
        }

        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var runtimeService = scope.ServiceProvider.GetRequiredService<WorkflowRuntimeService>();

            var timersProcessed = await runtimeService.ProcessDueTimersAsync(_options.TimerBatchSize, cancellationToken);
            var outboxProcessed = await runtimeService.ProcessOutboxAsync(_options.OutboxBatchSize, cancellationToken);

            if (timersProcessed > 0 || outboxProcessed > 0)
            {
                logger.LogInformation(
                    "Workflow background processing completed. Timers processed: {TimersProcessed}. Outbox processed: {OutboxProcessed}.",
                    timersProcessed,
                    outboxProcessed);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Workflow background processing cycle failed.");
        }
    }
}
