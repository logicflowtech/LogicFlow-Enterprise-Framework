namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class CompanyProfileSyncOptions
{
    public const string SectionName = "CompanyProfileSync";

    public bool ScheduleEnabled { get; set; }
    public bool UseLocalSynonym { get; set; } = true;
    public string LocalSynonymName { get; set; } = "[dbo].[syn_Company]";
    public string SourceConnectionStringName { get; set; } = "CompanyProfileSource";
    public string? SourceConnectionString { get; set; }
    public string SourceObjectName { get; set; } = "[dbo].[OSUSR_1sw_Company]";
    public int ScheduleMinutes { get; set; } = 60;
    public int BatchSize { get; set; } = 1000;
    public int CommandTimeoutSeconds { get; set; } = 120;
}
