namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class AddressSyncOptions
{
    public const string SectionName = "AddressSync";

    public string SourceConnectionStringName { get; set; } = "CompanyProfileSource";
    public string? SourceConnectionString { get; set; }
    public string SourceObjectName { get; set; } = "[dbo].[OSUSR_Z5Z_ADDRESS]";
    public int BatchSize { get; set; } = 1000;
    public int CommandTimeoutSeconds { get; set; } = 120;
}
