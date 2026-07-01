using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Application.Services;
using LogicFlowEnterpriseFramework.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
var syncService = provider.GetRequiredService<ICompanyUserSyncService>();

Console.WriteLine($"Running company user sync for source company {sourceCompanyId}...");
var result = await syncService.RunSyncAsync(sourceCompanyId);
Console.WriteLine($"Completed. Last processed rows: {result.LastProcessedRows}.");
Console.WriteLine(result.LastRunMessage);

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
