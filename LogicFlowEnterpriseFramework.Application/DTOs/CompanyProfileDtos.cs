namespace LogicFlowEnterpriseFramework.Application.DTOs;

public sealed record CompanyProfileListItemResponse(
    Guid Id,
    long? MigratedId,
    string? CompanyName,
    string? RegistrationNo,
    string? NewSsmCompanyRegNo,
    string? Email,
    string? TelephoneNo,
    string? Website,
    bool? IsCompanyLocal,
    int? CompanyApprovalStatus,
    DateTime? SourceModifiedDateTime,
    DateTime LastSyncedAt);

public sealed record CompanyProfileListResponse(
    IReadOnlyList<CompanyProfileListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    string? SearchTerm);

public sealed record CompanyProfileDetailResponse(
    Guid Id,
    long? MigratedId,
    string? CompanyName,
    string? RegistrationNo,
    DateTime? RegistrationDate,
    DateTime? DateOfIncorporation,
    string? TelephoneNo,
    string? FaxNo,
    string? Website,
    string? Email,
    string? IncomeTaxNo,
    string? EpfNo,
    string? SocsoNo,
    int? UserId,
    long? CompanySignatureId,
    int? CompanyType,
    bool? IsCompanyCertified,
    int? CompanyApprovalStatus,
    bool? IsPaid,
    bool? IsCompanyLocal,
    int? CreatedBySourceUserId,
    DateTime? SourceCreatedDateTime,
    int? ModifiedBySourceUserId,
    DateTime? SourceModifiedDateTime,
    Guid? AddressId,
    long? LegacyAddressId,
    string? BackgroundDescription1,
    string? NewSsmCompanyRegNo,
    int? CompanyStatusId,
    int? TotalEmployment,
    int? AnnualClosingDateDay,
    int? AnnualClosingDateMonth,
    string? AprNo,
    string? NonCode,
    DateTime LastSyncedAt);

public sealed record CompanyProfileIrpmAddressResponse(
    Guid? AddressId,
    long? LegacyAddressId,
    string? AddressLine1,
    string? AddressLine2,
    string? AddressLine3,
    string? Postcode,
    string? City,
    string? State,
    string? Country);

public sealed record CompanyProfileIrpmDirectorResponse(
    long MigratedId,
    string? Name,
    string? Designation,
    string? Nationality,
    decimal? SharePercentage);

public sealed record CompanyProfileIrpmContactResponse(
    Guid ApplicationUserId,
    long? LegacyContactPersonId,
    string? FullName,
    string? Designation,
    string? Email,
    string? PhoneNumber,
    string? UserName);

public sealed record CompanyProfileIrpmAuthorizedPersonResponse(
    long MigratedId,
    string? FullName,
    string? Designation,
    string? Email,
    string? TelephoneNo,
    string? IdentityNumber,
    bool? IsCertified,
    bool? IsPinVerified,
    bool? IsDigiCertPaid,
    bool IsDeletedInSource);

public sealed record CompanyProfileIrpmDocumentResponse(
    long MigratedId,
    string? FileName,
    string? FileType,
    bool HasFileContent,
    DateTime? SourceCreatedAt,
    DateTime LastSyncedAt);

public sealed record CompanyProfileIrpmResponse(
    Guid Id,
    long? MigratedId,
    string? CompanyName,
    string? RegistrationNo,
    DateTime? RegistrationDate,
    DateTime? DateOfIncorporation,
    string? TelephoneNo,
    string? FaxNo,
    string? Website,
    string? Email,
    string? IncomeTaxNo,
    string? EpfNo,
    string? SocsoNo,
    long? CompanySignatureId,
    int? CompanyType,
    bool? IsCompanyCertified,
    int? CompanyApprovalStatus,
    bool? IsPaid,
    bool? IsCompanyLocal,
    DateTime? SourceCreatedDateTime,
    DateTime? SourceModifiedDateTime,
    string? BackgroundDescription1,
    string? NewSsmCompanyRegNo,
    int? CompanyStatusId,
    int? TotalEmployment,
    int? AnnualClosingDateDay,
    int? AnnualClosingDateMonth,
    string? AprNo,
    string? NonCode,
    DateTime LastSyncedAt,
    CompanyProfileIrpmAddressResponse? Address,
    IReadOnlyList<CompanyProfileIrpmDirectorResponse> Directors,
    IReadOnlyList<CompanyProfileIrpmContactResponse> ContactPersons,
    IReadOnlyList<CompanyProfileIrpmAuthorizedPersonResponse> AuthorizedPersons,
    IReadOnlyList<CompanyProfileIrpmDocumentResponse> Documents);

public sealed record CompanyProfileFinancialDetailsSummaryResponse(
    Guid FinancialDetailsId,
    long MigratedId,
    long? LegacyProjectId,
    int? FinancialYear,
    DateTime? EffectiveDate,
    int? ProjectStatusId,
    DateTime LastSyncedAt);

public sealed record CompanyProfileFinancialAmountResponse(
    decimal? AmountRm,
    decimal? AmountPercent);

public sealed record CompanyProfileFinancialNamedAmountResponse(
    long? MigratedId,
    string? Name,
    string? Country,
    decimal? AmountRm,
    decimal? AmountPercent);

public sealed record CompanyProfileFinancialCompanyIncorporatedCountryResponse(
    long MigratedId,
    string? Country,
    decimal? CountryPercent,
    decimal? AmountRm,
    decimal? PercentOverTotal);

public sealed record CompanyProfileFinancialCompanyIncorporatedResponse(
    long MigratedId,
    string? CompanyName,
    int? LocalCompanyTypeId,
    decimal? BumiPercent,
    decimal? NonBumiPercent,
    string? ForeignCountry,
    decimal? ForeignPercent,
    decimal? TotalPercent,
    IReadOnlyList<CompanyProfileFinancialCompanyIncorporatedCountryResponse> Countries);

public sealed record CompanyProfileFinancialUltimateParentResponse(
    long MigratedId,
    string? ForeignCompanyName,
    string? UltimateCompany,
    string? Country);

public sealed record CompanyProfileFinancialSelectedDetailResponse(
    Guid FinancialDetailsId,
    long MigratedId,
    long? LegacyProjectId,
    int? FinancialYear,
    DateTime? EffectiveDate,
    int? ProjectStatusId,
    decimal? AuthorizedCapital,
    decimal? TotalPaidUpCapital,
    decimal? TotalReserves,
    decimal? TotalShareholderFund,
    decimal? TotalRmMalaysianIndividuals,
    decimal? TotalPercentMalaysianIndividuals,
    decimal? TotalRmForeignCompany,
    decimal? TotalPercentForeignCompany,
    decimal? TotalRmCompanyMalaysia,
    decimal? TotalPercentCompanyMalaysia,
    decimal? MalaysianBumiputeraRm,
    decimal? MalaysianBumiputeraPercent,
    decimal? MalaysianNonBumiputeraRm,
    decimal? MalaysianNonBumiputeraPercent,
    decimal? EquityBumiRm,
    decimal? EquityBumiPercent,
    decimal? EquityNonBumiRm,
    decimal? EquityNonBumiPercent,
    decimal? EquityForeignRm,
    decimal? EquityForeignPercent,
    decimal? EquityTotalRm,
    decimal? EquityTotalPercent,
    decimal? Revenue,
    decimal? Profit,
    decimal? TaxableExpenditure,
    decimal? ExportSales,
    decimal? LocalSales,
    decimal? TotalAsset,
    decimal? ShareholderFund,
    int? EmployeeCount,
    decimal? TotalRmLoan,
    decimal? TotalLoanPercent,
    string? DomesticDescription,
    string? ForeignDescription,
    decimal? TotalFinancingPaidUpCapital,
    decimal? TotalFinancingReserve,
    decimal? TotalFinancingLoan,
    decimal? TotalFinancingOtherSourcesRm,
    decimal? TotalFinancingOtherSourcesPercent,
    decimal? TotalFinancingRm,
    IReadOnlyList<CompanyProfileFinancialNamedAmountResponse> ForeignCompanies,
    IReadOnlyList<CompanyProfileFinancialNamedAmountResponse> CompaniesMalaysia,
    IReadOnlyList<CompanyProfileFinancialCompanyIncorporatedResponse> CompanyIncorporated,
    IReadOnlyList<CompanyProfileFinancialUltimateParentResponse> UltimateParents,
    IReadOnlyList<CompanyProfileFinancialAmountResponse> LoanDomestics,
    IReadOnlyList<CompanyProfileFinancialNamedAmountResponse> LoanForeigns,
    IReadOnlyList<CompanyProfileFinancialNamedAmountResponse> OtherSources);

public sealed record CompanyProfileFinancialDetailsResponse(
    Guid CompanyId,
    long? CompanyMigratedId,
    string? CompanyName,
    string? RegistrationNo,
    string? NewSsmCompanyRegNo,
    string? Sector,
    DateTime LastSyncedAt,
    IReadOnlyList<CompanyProfileFinancialDetailsSummaryResponse> AvailableStatements,
    CompanyProfileFinancialSelectedDetailResponse? SelectedStatement);
