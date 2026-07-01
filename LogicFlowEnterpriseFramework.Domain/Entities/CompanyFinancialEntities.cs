namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class CompanyProfileFinancialDetail : BaseEntity
{
    public Guid CompanyProfileId { get; set; }
    public CompanyProfile CompanyProfile { get; set; } = null!;
    public long MigratedId { get; set; }
    public long? LegacyProjectId { get; set; }
    public int? FinancialYear { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public int? ProjectStatusId { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public CompanyProfileEquityStructure? EquityStructure { get; set; }
    public CompanyProfileFinancialPerformanceRecord? FinancialPerformanceRecord { get; set; }
    public CompanyProfilePaidUpCapital? PaidUpCapital { get; set; }
    public CompanyProfileLoan? Loan { get; set; }
    public CompanyProfileTotalFinancing? TotalFinancing { get; set; }
    public ICollection<CompanyProfileOtherSource> OtherSources { get; set; } = new List<CompanyProfileOtherSource>();
}

public sealed class CompanyProfileAuthorizedCapital : BaseEntity
{
    public Guid CompanyProfileId { get; set; }
    public CompanyProfile CompanyProfile { get; set; } = null!;
    public long MigratedCompanyId { get; set; }
    public decimal? AuthorizedCapital { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfileEquityStructure : BaseEntity
{
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public decimal? BumiRm { get; set; }
    public decimal? BumiPercent { get; set; }
    public decimal? NonBumiRm { get; set; }
    public decimal? NonBumiPercent { get; set; }
    public decimal? ForeignRm { get; set; }
    public decimal? ForeignPercent { get; set; }
    public decimal? TotalRm { get; set; }
    public decimal? TotalPercent { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfileFinancialPerformanceRecord : BaseEntity
{
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public decimal? Revenue { get; set; }
    public decimal? Profit { get; set; }
    public decimal? TaxableExpenditure { get; set; }
    public decimal? ExportSales { get; set; }
    public decimal? LocalSales { get; set; }
    public decimal? TotalAsset { get; set; }
    public decimal? ShareholderFund { get; set; }
    public int? EmployeeCount { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfilePaidUpCapital : BaseEntity
{
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public decimal? TotalPaidUpCapital { get; set; }
    public decimal? TotalReserves { get; set; }
    public decimal? TotalShareholderFund { get; set; }
    public decimal? TotalRmMalaysianIndividuals { get; set; }
    public decimal? TotalPercentMalaysianIndividuals { get; set; }
    public decimal? TotalRmForeignCompany { get; set; }
    public decimal? TotalPercentForeignCompany { get; set; }
    public decimal? TotalRmCompanyMalaysia { get; set; }
    public decimal? TotalPercentCompanyMalaysia { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public CompanyProfilePaidUpCapitalMalaysianIndividuals? MalaysianIndividuals { get; set; }
    public ICollection<CompanyProfilePaidUpCapitalForeignCompany> ForeignCompanies { get; set; } = new List<CompanyProfilePaidUpCapitalForeignCompany>();
    public ICollection<CompanyProfilePaidUpCapitalCompanyMalaysia> CompaniesMalaysia { get; set; } = new List<CompanyProfilePaidUpCapitalCompanyMalaysia>();
}

public sealed class CompanyProfilePaidUpCapitalMalaysianIndividuals : BaseEntity
{
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public decimal? BumiputeraRm { get; set; }
    public decimal? BumiputeraPercent { get; set; }
    public decimal? NonBumiputeraRm { get; set; }
    public decimal? NonBumiputeraPercent { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfilePaidUpCapitalForeignCompany : BaseEntity
{
    public long MigratedId { get; set; }
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public string? CompanyName { get; set; }
    public Guid? CountryId { get; set; }
    public long? LegacyCountryId { get; set; }
    public decimal? AmountRm { get; set; }
    public decimal? AmountPercent { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public ICollection<CompanyProfileUltimateParentHoldingCompany> UltimateParents { get; set; } = new List<CompanyProfileUltimateParentHoldingCompany>();
}

public sealed class CompanyProfilePaidUpCapitalCompanyMalaysia : BaseEntity
{
    public long MigratedId { get; set; }
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public string? CompanyName { get; set; }
    public decimal? AmountRm { get; set; }
    public decimal? AmountPercent { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public ICollection<CompanyProfileCompanyIncorporated> CompanyIncorporatedEntries { get; set; } = new List<CompanyProfileCompanyIncorporated>();
}

public sealed class CompanyProfileCompanyIncorporated : BaseEntity
{
    public long MigratedId { get; set; }
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public Guid PaidUpCapitalCompanyMalaysiaEntryId { get; set; }
    public CompanyProfilePaidUpCapitalCompanyMalaysia PaidUpCapitalCompanyMalaysiaEntry { get; set; } = null!;
    public int? LocalCompanyTypeId { get; set; }
    public decimal? BumiPercent { get; set; }
    public decimal? NonBumiPercent { get; set; }
    public Guid? ForeignCountryId { get; set; }
    public long? LegacyForeignCountryId { get; set; }
    public decimal? ForeignPercent { get; set; }
    public decimal? TotalPercent { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public ICollection<CompanyProfileCompanyIncorporatedCountry> Countries { get; set; } = new List<CompanyProfileCompanyIncorporatedCountry>();
}

public sealed class CompanyProfileCompanyIncorporatedCountry : BaseEntity
{
    public long MigratedId { get; set; }
    public Guid CompanyIncorporatedEntryId { get; set; }
    public CompanyProfileCompanyIncorporated CompanyIncorporatedEntry { get; set; } = null!;
    public Guid? CountryId { get; set; }
    public long? LegacyCountryId { get; set; }
    public decimal? CountryPercent { get; set; }
    public decimal? AmountRm { get; set; }
    public decimal? PercentOverTotal { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfileLoan : BaseEntity
{
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public decimal? TotalRmLoan { get; set; }
    public decimal? TotalLoanPercent { get; set; }
    public string? DomesticDescription { get; set; }
    public string? ForeignDescription { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public ICollection<CompanyProfileLoanDomestic> DomesticBreakdowns { get; set; } = new List<CompanyProfileLoanDomestic>();
    public ICollection<CompanyProfileLoanForeign> ForeignBreakdowns { get; set; } = new List<CompanyProfileLoanForeign>();
}

public sealed class CompanyProfileLoanDomestic : BaseEntity
{
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public decimal? AmountRm { get; set; }
    public decimal? AmountPercent { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfileLoanForeign : BaseEntity
{
    public long MigratedId { get; set; }
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public Guid? CountryId { get; set; }
    public long? LegacyCountryId { get; set; }
    public decimal? AmountRm { get; set; }
    public decimal? AmountPercent { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfileTotalFinancing : BaseEntity
{
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public decimal? TotalPaidUpCapital { get; set; }
    public decimal? TotalReserve { get; set; }
    public decimal? TotalLoan { get; set; }
    public decimal? TotalRmOtherSources { get; set; }
    public decimal? TotalPercentOtherSources { get; set; }
    public decimal? TotalFinancingRm { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfileOtherSource : BaseEntity
{
    public long MigratedId { get; set; }
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public string? OtherSources { get; set; }
    public decimal? AmountRm { get; set; }
    public decimal? AmountPercent { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfileUltimateParentHoldingCompany : BaseEntity
{
    public long MigratedId { get; set; }
    public Guid FinancialDetailsId { get; set; }
    public CompanyProfileFinancialDetail FinancialDetails { get; set; } = null!;
    public Guid PaidUpCapitalForeignCompanyEntryId { get; set; }
    public CompanyProfilePaidUpCapitalForeignCompany PaidUpCapitalForeignCompanyEntry { get; set; } = null!;
    public string? UltimateCompany { get; set; }
    public Guid? CountryId { get; set; }
    public long? LegacyCountryId { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
