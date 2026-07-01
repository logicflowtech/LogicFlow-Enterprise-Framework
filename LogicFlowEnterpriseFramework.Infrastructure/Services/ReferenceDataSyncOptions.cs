namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class ReferenceDataSyncOptions
{
    public const string SectionName = "ReferenceDataSync";

    public string SourceConnectionStringName { get; set; } = "CompanyProfileSource";
    public string? SourceConnectionString { get; set; }
    public string TitleSourceObjectName { get; set; } = "[dbo].[OSUSR_P6Z_TITLE]";
    public string IdentificationTypeSourceObjectName { get; set; } = "[dbo].[OSUSR_P6Z_IDENTIFICATIONTYPE]";
    public string GeoSourceObjectName { get; set; } = "[dbo].[OSUSR_Z5Z_ADDRESS]";
    public int BatchSize { get; set; } = 1000;
    public int CommandTimeoutSeconds { get; set; } = 120;
    public List<ReferenceCountryMappingOptions> CountryMappings { get; set; } = [];
}

public sealed class ReferenceCountryMappingOptions
{
    public long MigratedId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
