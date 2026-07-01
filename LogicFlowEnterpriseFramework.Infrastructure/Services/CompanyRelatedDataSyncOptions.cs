namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class CompanyRelatedDataSyncOptions
{
    public const string SectionName = "CompanyRelatedDataSync";

    public string SourceConnectionStringName { get; set; } = "CompanyProfileSource";
    public string? SourceConnectionString { get; set; }
    public string AuthorizedPersonSourceObjectName { get; set; } = "[dbo].[OSUSR_1sw_AuthorizedPerson]";
    public string BoardDirectorSourceObjectName { get; set; } = "[dbo].[OSUSR_1sw_BoardOfDirector]";
    public string AttachmentDocumentSourceObjectName { get; set; } = "[dbo].[OSUSR_1sw_CompanyAttachmentDocument]";
    public int BatchSize { get; set; } = 1000;
    public int CommandTimeoutSeconds { get; set; } = 120;
}
