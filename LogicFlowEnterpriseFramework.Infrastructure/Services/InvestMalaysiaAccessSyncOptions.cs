namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class InvestMalaysiaAccessSyncOptions
{
    public const string SectionName = "InvestMalaysiaAccessSync";

    public string SourceConnectionStringName { get; set; } = "CompanyProfileSource";
    public string? SourceConnectionString { get; set; }
    public string UserSourceObjectName { get; set; } = "[dbo].[OSSYS_USER]";
    public string GroupSourceObjectName { get; set; } = "[dbo].[OSSYS_GROUP]";
    public string RoleSourceObjectName { get; set; } = "[dbo].[OSSYS_ROLE]";
    public string GroupUserSourceObjectName { get; set; } = "[dbo].[OSSYS_GROUP_USER]";
    public string GroupRoleSourceObjectName { get; set; } = "[dbo].[OSSYS_GROUP_ROLE]";
    public string UserRoleSourceObjectName { get; set; } = "[dbo].[OSSYS_USER_ROLE]";
    public string ContactPersonSourceObjectName { get; set; } = "[dbo].[OSUSR_1sw_ContactPerson]";
    public int BatchSize { get; set; } = 1000;
    public int CommandTimeoutSeconds { get; set; } = 120;
}
