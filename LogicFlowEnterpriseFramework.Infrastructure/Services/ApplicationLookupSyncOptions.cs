namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class ApplicationLookupSyncOptions
{
    public const string SectionName = "ApplicationLookupSync";

    public string SourceConnectionStringName { get; set; } = "CompanyProfileSource";
    public string? SourceConnectionString { get; set; }
    public string ApplicationCategorySourceObjectName { get; set; } = "[dbo].[OSUSR_D22_APPLICATIONCATEGORY]";
    public string ApplicationForSourceObjectName { get; set; } = "[dbo].[OSUSR_D22_APPLICATIONFOR]";
    public string ApplicationTypeSourceObjectName { get; set; } = "[dbo].[OSUSR_D22_APPLICATIONTYPE]";
    public string ApplicationStatusSourceObjectName { get; set; } = "[dbo].[OSUSR_D22_APPLICATIONSTATUS]";
    public string ApplicationForCategorySourceObjectName { get; set; } = "[dbo].[OSUSR_D22_APPLICATIONFORCATEGORY]";
    public string ApplicationForApplicationTypeSourceObjectName { get; set; } = "[dbo].[OSUSR_D22_APPLICATIONFORAPPLICATIONTYPE]";
    public int BatchSize { get; set; } = 1000;
    public int CommandTimeoutSeconds { get; set; } = 120;
}
