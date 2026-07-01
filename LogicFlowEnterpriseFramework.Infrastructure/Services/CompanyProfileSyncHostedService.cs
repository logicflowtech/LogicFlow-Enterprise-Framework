using LogicFlowEnterpriseFramework.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class CompanyProfileSyncHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<CompanyProfileSyncOptions> options,
    ILogger<CompanyProfileSyncHostedService> logger) : BackgroundService
{
    private readonly CompanyProfileSyncOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.ScheduleEnabled)
        {
            logger.LogInformation("Company profile sync scheduler is disabled.");
            return;
        }

        if (_options.ScheduleMinutes <= 0)
        {
            logger.LogWarning("Company profile sync scheduler is enabled, but ScheduleMinutes is invalid: {ScheduleMinutes}", _options.ScheduleMinutes);
            return;
        }

        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.ScheduleMinutes));
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
            var syncService = scope.ServiceProvider.GetRequiredService<ICompanyProfileSyncService>();
            var status = await syncService.RunSyncAsync(cancellationToken);
            logger.LogInformation(
                "Company profile sync completed. Success: {Success}. Rows: {Rows}. Message: {Message}",
                status.LastRunSucceeded,
                status.LastProcessedRows,
                status.LastRunMessage);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Scheduled company profile sync failed.");
        }
    }
}
