namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class CompanyFinancialDataSyncOptions
{
    public const string SectionName = "CompanyFinancialDataSync";

    public string SourceConnectionStringName { get; set; } = "CompanyProfileSource";
    public string? SourceConnectionString { get; set; }
    public string ProjectSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_PROJECT]";
    public string ProjectFinancingSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_PROJECTFINANCING]";
    public string FinancingStructureSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_FINANCINGSTRUCTURE]";
    public string AuthorizedCapitalSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_AUTHORIZEDCAPITAL1]";
    public string EquityStructureSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_EQUITYSTRUCTURE]";
    public string FinancialPerformanceSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_FINANCIALPERFORMANCERECORD]";
    public string PaidUpCapitalSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_PUC_PAIDUPCAPITAL]";
    public string MalaysianIndividualsSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_PUC_MALAYSIANINDIVIDUALS]";
    public string ForeignCompanySourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_PUC_FOREIGNCOMPANY]";
    public string CompanyMalaysiaSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_PUC_COMPANYMALAYSIA]";
    public string LoanSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_LOAN]";
    public string LoanForeignSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_LOAN_FOREIGN]";
    public string TotalFinancingSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_TOTALFINANCING]";
    public string OtherSourcesSourceObjectName { get; set; } = "[dbo].[OSUSR_LPP_COMPFIN_OTHERSOURCES]";
    public int BatchSize { get; set; } = 1000;
    public int CommandTimeoutSeconds { get; set; } = 120;
}
