using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Application.Services;
using LogicFlowEnterpriseFramework.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var runApplicationLookupSync = args.Any(arg => string.Equals(arg, "--application-lookups", StringComparison.OrdinalIgnoreCase));
var sourceCompanyId = args.Length > 0 && long.TryParse(args[0], out var parsedCompanyId)
    ? parsedCompanyId
    : 10001L;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
}));
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<ITenantProvider, NullTenantProvider>();
services.AddSingleton<ICurrentUserService, NullCurrentUserService>();
services.AddApplicationServices();
services.AddInfrastructureServices(configuration);

await using var provider = services.BuildServiceProvider();
if (runApplicationLookupSync)
{
    var lookupSyncService = provider.GetRequiredService<IApplicationLookupSyncService>();
    var syncKeys = new[]
    {
        "application-categories",
        "application-fors",
        "application-types",
        "application-statuses",
        "application-category-fors",
        "application-for-types"
    };

    foreach (var syncKey in syncKeys)
    {
        Console.WriteLine($"Running {syncKey}...");
        var result = await lookupSyncService.RunAsync(syncKey);
        Console.WriteLine($"Completed {syncKey}. Rows: {result.LastProcessedRows}. Message: {result.LastRunMessage}");
    }

    return;
}

var syncService = provider.GetRequiredService<ICompanyUserSyncService>();

Console.WriteLine($"Running company user sync for source company {sourceCompanyId}...");
var syncResult = await syncService.RunSyncAsync(sourceCompanyId);
Console.WriteLine($"Completed. Last processed rows: {syncResult.LastProcessedRows}.");
Console.WriteLine(syncResult.LastRunMessage);

file sealed class NullTenantProvider : ITenantProvider
{
    public Guid? TenantId => null;
}

file sealed class NullCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? TenantId => null;
    public string? UserName => null;
}
