using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/company-profiles")]
public sealed class CompanyProfilesController(ApplicationDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.CompanyProfilesRead)]
    public async Task<ActionResult<ApiResponse<CompanyProfileListResponse>>> Get(
        [FromQuery] string? searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = Math.Clamp(pageSize, 10, 100);
        searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var migratedIdSearch = long.TryParse(searchTerm, out var parsedMigratedId) ? parsedMigratedId : (long?)null;

        var query = await ApplyCompanyScopeAsync(dbContext.CompanyProfiles.AsNoTracking(), cancellationToken);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(company =>
                (migratedIdSearch.HasValue && company.MigratedId == migratedIdSearch.Value) ||
                (company.CompanyName != null && company.CompanyName.Contains(searchTerm)) ||
                (company.RegistrationNo != null && company.RegistrationNo.Contains(searchTerm)) ||
                (company.NewSsmCompanyRegNo != null && company.NewSsmCompanyRegNo.Contains(searchTerm)) ||
                (company.Email != null && company.Email.Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(company => company.CompanyName ?? string.Empty)
            .ThenBy(company => company.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(company => new CompanyProfileListItemResponse(
                company.Id,
                company.MigratedId,
                company.CompanyName,
                company.RegistrationNo,
                company.NewSsmCompanyRegNo,
                company.Email,
                company.TelephoneNo,
                company.Website,
                company.IsCompanyLocal,
                company.CompanyApprovalStatus,
                company.SourceModifiedDateTime,
                company.LastSyncedAt))
            .ToListAsync(cancellationToken);

        var result = new CompanyProfileListResponse(items, pageNumber, pageSize, totalCount, searchTerm);
        return Ok(ApiResponse<CompanyProfileListResponse>.Success(result));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.CompanyProfilesRead)]
    public async Task<ActionResult<ApiResponse<CompanyProfileDetailResponse>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var companyQuery = await ApplyCompanyScopeAsync(dbContext.CompanyProfiles.AsNoTracking(), cancellationToken);

        var company = await companyQuery
            .AsNoTracking()
            .Where(company => company.Id == id)
            .Select(company => new CompanyProfileDetailResponse(
                company.Id,
                company.MigratedId,
                company.CompanyName,
                company.RegistrationNo,
                company.RegistrationDate,
                company.DateOfIncorporation,
                company.TelephoneNo,
                company.FaxNo,
                company.Website,
                company.Email,
                company.IncomeTaxNo,
                company.EpfNo,
                company.SocsoNo,
                company.UserId,
                company.CompanySignatureId,
                company.CompanyType,
                company.IsCompanyCertified,
                company.CompanyApprovalStatus,
                company.IsPaid,
                company.IsCompanyLocal,
                company.CreatedBySourceUserId,
                company.SourceCreatedDateTime,
                company.ModifiedBySourceUserId,
                company.SourceModifiedDateTime,
                company.AddressId,
                company.LegacyAddressId,
                company.BackgroundDescription1,
                company.NewSsmCompanyRegNo,
                company.CompanyStatusId,
                company.TotalEmployment,
                company.AnnualClosingDateDay,
                company.AnnualClosingDateMonth,
                company.AprNo,
                company.NonCode,
                company.LastSyncedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            return NotFound(ApiResponse<CompanyProfileDetailResponse>.Failure("Company profile not found."));
        }

        return Ok(ApiResponse<CompanyProfileDetailResponse>.Success(company));
    }

    [HttpGet("{id:guid}/irpm")]
    [HasPermission(Permissions.CompanyProfilesRead)]
    public async Task<ActionResult<ApiResponse<CompanyProfileIrpmResponse>>> GetIrpmProfile(Guid id, CancellationToken cancellationToken = default)
    {
        var companyQuery = await ApplyCompanyScopeAsync(dbContext.CompanyProfiles.AsNoTracking(), cancellationToken);

        var company = await companyQuery
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.MigratedId,
                item.CompanyName,
                item.RegistrationNo,
                item.RegistrationDate,
                item.DateOfIncorporation,
                item.TelephoneNo,
                item.FaxNo,
                item.Website,
                item.Email,
                item.IncomeTaxNo,
                item.EpfNo,
                item.SocsoNo,
                item.CompanySignatureId,
                item.CompanyType,
                item.IsCompanyCertified,
                item.CompanyApprovalStatus,
                item.IsPaid,
                item.IsCompanyLocal,
                item.SourceCreatedDateTime,
                item.SourceModifiedDateTime,
                item.BackgroundDescription1,
                item.NewSsmCompanyRegNo,
                item.CompanyStatusId,
                item.TotalEmployment,
                item.AnnualClosingDateDay,
                item.AnnualClosingDateMonth,
                item.AprNo,
                item.NonCode,
                item.LastSyncedAt,
                item.AddressId,
                item.LegacyAddressId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            return NotFound(ApiResponse<CompanyProfileIrpmResponse>.Failure("Company profile not found."));
        }

        var addressData = company.AddressId.HasValue
            ? await (
                from item in dbContext.Addresses.AsNoTracking()
                join country in dbContext.LookupCountries.AsNoTracking() on item.CountryId equals country.Id into countryJoin
                from country in countryJoin.DefaultIfEmpty()
                join state in dbContext.LookupStates.AsNoTracking() on item.StateId equals state.Id into stateJoin
                from state in stateJoin.DefaultIfEmpty()
                join city in dbContext.LookupCities.AsNoTracking() on item.CityId equals city.Id into cityJoin
                from city in cityJoin.DefaultIfEmpty()
                where item.Id == company.AddressId.Value
                select new
                {
                    item.Id,
                    item.MigratedId,
                    item.AddressLine1,
                    item.AddressLine2,
                    item.AddressLine3,
                    item.Postcode,
                    item.CityName,
                    item.StateName,
                    item.CountryName,
                    CountryMigratedId = country != null ? country.MigratedId : null,
                    LookupCountryName = country != null ? country.Name : null,
                    LookupStateName = state != null ? state.Name : null,
                    LookupCityName = city != null ? city.Name : null
                })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var address = addressData is null
            ? null
            : new CompanyProfileIrpmAddressResponse(
                addressData.Id,
                addressData.MigratedId,
                addressData.AddressLine1,
                addressData.AddressLine2,
                addressData.AddressLine3,
                addressData.Postcode,
                string.IsNullOrWhiteSpace(addressData.CityName) ? addressData.LookupCityName : addressData.CityName,
                string.IsNullOrWhiteSpace(addressData.StateName) ? addressData.LookupStateName : addressData.StateName,
                !string.IsNullOrWhiteSpace(addressData.CountryName)
                    ? addressData.CountryName
                    : addressData.CountryMigratedId == 132
                        ? "Malaysia"
                        : string.IsNullOrWhiteSpace(addressData.LookupCountryName) || addressData.LookupCountryName.StartsWith("Legacy Country ")
                            ? null
                            : addressData.LookupCountryName);

        var directors = await dbContext.CompanyBoardDirectors
            .AsNoTracking()
            .Where(item => item.CompanyProfileId == company.Id && !item.IsDeleted)
            .OrderBy(item => item.Name ?? string.Empty)
            .Select(item => new CompanyProfileIrpmDirectorResponse(
                item.MigratedId,
                item.Name,
                null,
                item.LegacyNationalityId.HasValue ? $"Nationality {item.LegacyNationalityId.Value}" : null,
                item.SharePercentage))
            .ToListAsync(cancellationToken);

        var contacts = await (
                from assignment in dbContext.CompanyProfileUserAssignments.AsNoTracking()
                join user in dbContext.Users.AsNoTracking() on assignment.ApplicationUserId equals user.Id
                join profile in dbContext.UserProfiles.AsNoTracking() on user.Id equals profile.ApplicationUserId into profileJoin
                from profile in profileJoin.DefaultIfEmpty()
                where assignment.CompanyProfileId == company.Id
                    && assignment.IsActive
                    && !assignment.IsDeleted
                orderby user.FullName
                select new CompanyProfileIrpmContactResponse(
                    assignment.ApplicationUserId,
                    assignment.LegacyContactPersonId,
                    user.FullName,
                    profile != null
                        ? (!string.IsNullOrWhiteSpace(profile.CustomDesignationName)
                            ? profile.CustomDesignationName
                            : profile.TitleDisplayName)
                        : null,
                    user.Email,
                    user.PhoneNumber,
                    user.UserName))
            .ToListAsync(cancellationToken);

        var authorizedPersons = await dbContext.CompanyAuthorizedPersons
            .AsNoTracking()
            .Where(item => item.CompanyProfileId == company.Id && !item.IsDeleted)
            .OrderBy(item => item.FullName ?? string.Empty)
            .Select(item => new CompanyProfileIrpmAuthorizedPersonResponse(
                item.MigratedId,
                item.FullName,
                item.Designation,
                item.Email,
                item.TelephoneNo,
                item.IdentityNumber,
                item.IsCertified,
                item.IsPinVerified,
                item.IsDigiCertPaid,
                item.IsDeletedInSource))
            .ToListAsync(cancellationToken);

        var documents = await dbContext.CompanyAttachmentDocuments
            .AsNoTracking()
            .Where(item => item.CompanyProfileId == company.Id && !item.IsDeleted)
            .OrderBy(item => item.FileName ?? string.Empty)
            .Select(item => new CompanyProfileIrpmDocumentResponse(
                item.MigratedId,
                item.FileName,
                item.FileType,
                item.FileContent != null && item.FileContent.Length > 0,
                item.SourceCreatedAt,
                item.LastSyncedAt))
            .ToListAsync(cancellationToken);

        var result = new CompanyProfileIrpmResponse(
            company.Id,
            company.MigratedId,
            company.CompanyName,
            company.RegistrationNo,
            company.RegistrationDate,
            company.DateOfIncorporation,
            company.TelephoneNo,
            company.FaxNo,
            company.Website,
            company.Email,
            company.IncomeTaxNo,
            company.EpfNo,
            company.SocsoNo,
            company.CompanySignatureId,
            company.CompanyType,
            company.IsCompanyCertified,
            company.CompanyApprovalStatus,
            company.IsPaid,
            company.IsCompanyLocal,
            company.SourceCreatedDateTime,
            company.SourceModifiedDateTime,
            company.BackgroundDescription1,
            company.NewSsmCompanyRegNo,
            company.CompanyStatusId,
            company.TotalEmployment,
            company.AnnualClosingDateDay,
            company.AnnualClosingDateMonth,
            company.AprNo,
            company.NonCode,
            company.LastSyncedAt,
            address,
            directors,
            contacts,
            authorizedPersons,
            documents);

        return Ok(ApiResponse<CompanyProfileIrpmResponse>.Success(result));
    }

    [HttpGet("{id:guid}/financial-details")]
    [HasPermission(Permissions.CompanyProfilesRead)]
    public async Task<ActionResult<ApiResponse<CompanyProfileFinancialDetailsResponse>>> GetFinancialDetails(
        Guid id,
        [FromQuery] Guid? financialDetailId = null,
        CancellationToken cancellationToken = default)
    {
        var companyQuery = await ApplyCompanyScopeAsync(dbContext.CompanyProfiles.AsNoTracking(), cancellationToken);

        var company = await companyQuery
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.MigratedId,
                item.CompanyName,
                item.RegistrationNo,
                item.NewSsmCompanyRegNo,
                item.LastSyncedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            return NotFound(ApiResponse<CompanyProfileFinancialDetailsResponse>.Failure("Company profile not found."));
        }

        var availableStatements = await dbContext.CompanyProfileFinancialDetails
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.CompanyProfileId == id && !item.IsDeleted)
            .OrderByDescending(item => item.FinancialYear ?? int.MinValue)
            .ThenByDescending(item => item.EffectiveDate ?? DateTime.MinValue)
            .ThenByDescending(item => item.LastSyncedAt)
            .Select(item => new CompanyProfileFinancialDetailsSummaryResponse(
                item.Id,
                item.MigratedId,
                item.LegacyProjectId,
                item.FinancialYear,
                item.EffectiveDate,
                item.ProjectStatusId,
                item.LastSyncedAt))
            .ToListAsync(cancellationToken);

        var selectedFinancialDetailKey = financialDetailId.HasValue && availableStatements.Any(item => item.FinancialDetailsId == financialDetailId.Value)
            ? financialDetailId.Value
            : availableStatements.FirstOrDefault()?.FinancialDetailsId;

        CompanyProfileFinancialSelectedDetailResponse? selectedStatement = null;

        if (selectedFinancialDetailKey.HasValue)
        {
            var detail = await dbContext.CompanyProfileFinancialDetails
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == selectedFinancialDetailKey.Value && item.CompanyProfileId == id && !item.IsDeleted, cancellationToken);

            if (detail is not null)
            {
                var countryMap = await dbContext.LookupCountries
                    .AsNoTracking()
                    .Where(item => item.IsActive)
                    .Select(item => new { item.Id, item.Name })
                    .ToDictionaryAsync(item => item.Id, item => item.Name ?? string.Empty, cancellationToken);

                string? ResolveCountry(Guid? countryId, long? legacyCountryId)
                {
                    if (countryId.HasValue && countryMap.TryGetValue(countryId.Value, out var name))
                    {
                        return name;
                    }

                    return legacyCountryId.HasValue ? $"Country {legacyCountryId.Value}" : null;
                }

                var authorizedCapital = await dbContext.CompanyProfileAuthorizedCapitals
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(item => item.CompanyProfileId == detail.CompanyProfileId && !item.IsDeleted)
                    .OrderByDescending(item => item.LastSyncedAt)
                    .Select(item => item.AuthorizedCapital)
                    .FirstOrDefaultAsync(cancellationToken);

                var equityStructure = await dbContext.CompanyProfileEquityStructures
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted, cancellationToken);

                var financialPerformance = await dbContext.CompanyProfileFinancialPerformanceRecords
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted, cancellationToken);

                var paidUpCapital = await dbContext.CompanyProfilePaidUpCapitals
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted, cancellationToken);

                var malaysianIndividuals = await dbContext.CompanyProfilePaidUpCapitalMalaysianIndividuals
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted, cancellationToken);

                var foreignCompanies = await dbContext.CompanyProfilePaidUpCapitalForeignCompanies
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted)
                    .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                    .ThenBy(item => item.CompanyName)
                    .ToListAsync(cancellationToken);

                var companiesMalaysia = await dbContext.CompanyProfilePaidUpCapitalCompaniesMalaysia
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted)
                    .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                    .ThenBy(item => item.CompanyName)
                    .ToListAsync(cancellationToken);

                var companyMalaysiaIds = companiesMalaysia.Select(item => item.Id).ToList();
                var foreignCompanyIds = foreignCompanies.Select(item => item.Id).ToList();

                var incorporatedEntries = companyMalaysiaIds.Count == 0
                    ? []
                    : await dbContext.CompanyProfileCompanyIncorporatedEntries
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .Where(item => item.FinancialDetailsId == detail.Id && companyMalaysiaIds.Contains(item.PaidUpCapitalCompanyMalaysiaEntryId) && !item.IsDeleted)
                        .OrderBy(item => item.MigratedId)
                        .ToListAsync(cancellationToken);

                var incorporatedEntryIds = incorporatedEntries.Select(item => item.Id).ToList();

                var incorporatedCountries = incorporatedEntryIds.Count == 0
                    ? []
                    : await dbContext.CompanyProfileCompanyIncorporatedCountries
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .Where(item => incorporatedEntryIds.Contains(item.CompanyIncorporatedEntryId) && !item.IsDeleted)
                        .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                        .ThenBy(item => item.MigratedId)
                        .ToListAsync(cancellationToken);

                var ultimateParents = foreignCompanyIds.Count == 0
                    ? []
                    : await dbContext.CompanyProfileUltimateParentHoldingCompanies
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .Where(item => item.FinancialDetailsId == detail.Id && foreignCompanyIds.Contains(item.PaidUpCapitalForeignCompanyEntryId) && !item.IsDeleted)
                        .OrderBy(item => item.UltimateCompany)
                        .ToListAsync(cancellationToken);

                var loan = await dbContext.CompanyProfileLoans
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted, cancellationToken);

                var loanDomestics = await dbContext.CompanyProfileLoanDomestics
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted)
                    .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                    .ToListAsync(cancellationToken);

                var loanForeigns = await dbContext.CompanyProfileLoanForeigns
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted)
                    .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                    .ToListAsync(cancellationToken);

                var totalFinancing = await dbContext.CompanyProfileTotalFinancings
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted, cancellationToken);

                var otherSources = await dbContext.CompanyProfileOtherSources
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(item => item.FinancialDetailsId == detail.Id && !item.IsDeleted)
                    .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                    .ThenBy(item => item.OtherSources)
                    .ToListAsync(cancellationToken);

                selectedStatement = new CompanyProfileFinancialSelectedDetailResponse(
                    detail.Id,
                    detail.MigratedId,
                    detail.LegacyProjectId,
                    detail.FinancialYear,
                    detail.EffectiveDate,
                    detail.ProjectStatusId,
                    authorizedCapital,
                    paidUpCapital?.TotalPaidUpCapital,
                    paidUpCapital?.TotalReserves,
                    paidUpCapital?.TotalShareholderFund,
                    paidUpCapital?.TotalRmMalaysianIndividuals,
                    paidUpCapital?.TotalPercentMalaysianIndividuals,
                    paidUpCapital?.TotalRmForeignCompany,
                    paidUpCapital?.TotalPercentForeignCompany,
                    paidUpCapital?.TotalRmCompanyMalaysia,
                    paidUpCapital?.TotalPercentCompanyMalaysia,
                    malaysianIndividuals?.BumiputeraRm,
                    malaysianIndividuals?.BumiputeraPercent,
                    malaysianIndividuals?.NonBumiputeraRm,
                    malaysianIndividuals?.NonBumiputeraPercent,
                    equityStructure?.BumiRm,
                    equityStructure?.BumiPercent,
                    equityStructure?.NonBumiRm,
                    equityStructure?.NonBumiPercent,
                    equityStructure?.ForeignRm,
                    equityStructure?.ForeignPercent,
                    equityStructure?.TotalRm,
                    equityStructure?.TotalPercent,
                    financialPerformance?.Revenue,
                    financialPerformance?.Profit,
                    financialPerformance?.TaxableExpenditure,
                    financialPerformance?.ExportSales,
                    financialPerformance?.LocalSales,
                    financialPerformance?.TotalAsset,
                    financialPerformance?.ShareholderFund,
                    financialPerformance?.EmployeeCount,
                    loan?.TotalRmLoan,
                    loan?.TotalLoanPercent,
                    loan?.DomesticDescription,
                    loan?.ForeignDescription,
                    totalFinancing?.TotalPaidUpCapital,
                    totalFinancing?.TotalReserve,
                    totalFinancing?.TotalLoan,
                    totalFinancing?.TotalRmOtherSources,
                    totalFinancing?.TotalPercentOtherSources,
                    totalFinancing?.TotalFinancingRm,
                    foreignCompanies
                        .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                        .ThenBy(item => item.CompanyName)
                        .Select(item => new CompanyProfileFinancialNamedAmountResponse(
                            item.MigratedId,
                            item.CompanyName,
                            ResolveCountry(item.CountryId, item.LegacyCountryId),
                            item.AmountRm,
                            item.AmountPercent))
                        .ToList(),
                    companiesMalaysia
                        .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                        .ThenBy(item => item.CompanyName)
                        .Select(item => new CompanyProfileFinancialNamedAmountResponse(
                            item.MigratedId,
                            item.CompanyName,
                            null,
                            item.AmountRm,
                            item.AmountPercent))
                        .ToList(),
                    companiesMalaysia
                        .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                        .ThenBy(item => item.CompanyName)
                        .Select(item =>
                        {
                            var incorporated = incorporatedEntries
                                .Where(entry => entry.PaidUpCapitalCompanyMalaysiaEntryId == item.Id)
                                .OrderBy(entry => entry.MigratedId)
                                .FirstOrDefault();

                            return incorporated is null
                                ? null
                                : new CompanyProfileFinancialCompanyIncorporatedResponse(
                                    incorporated.MigratedId,
                                    item.CompanyName,
                                    incorporated.LocalCompanyTypeId,
                                    incorporated.BumiPercent,
                                    incorporated.NonBumiPercent,
                                    ResolveCountry(incorporated.ForeignCountryId, incorporated.LegacyForeignCountryId),
                                    incorporated.ForeignPercent,
                                    incorporated.TotalPercent,
                                    incorporatedCountries
                                        .Where(country => country.CompanyIncorporatedEntryId == incorporated.Id)
                                        .OrderByDescending(country => country.AmountRm ?? decimal.MinValue)
                                        .ThenBy(country => country.MigratedId)
                                        .Select(country => new CompanyProfileFinancialCompanyIncorporatedCountryResponse(
                                            country.MigratedId,
                                            ResolveCountry(country.CountryId, country.LegacyCountryId),
                                            country.CountryPercent,
                                            country.AmountRm,
                                            country.PercentOverTotal))
                                        .ToList());
                        })
                        .Where(item => item is not null)
                        .Select(item => item!)
                        .ToList(),
                    foreignCompanies
                        .OrderBy(item => item.CompanyName)
                        .SelectMany(item => ultimateParents
                            .Where(parent => parent.PaidUpCapitalForeignCompanyEntryId == item.Id)
                            .Select(parent => new CompanyProfileFinancialUltimateParentResponse(
                                parent.MigratedId,
                                item.CompanyName,
                                parent.UltimateCompany,
                                ResolveCountry(parent.CountryId, parent.LegacyCountryId))))
                        .ToList(),
                    loanDomestics
                        .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                        .Select(item => new CompanyProfileFinancialAmountResponse(
                            item.AmountRm,
                            item.AmountPercent))
                        .ToList(),
                    loanForeigns
                        .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                        .Select(item => new CompanyProfileFinancialNamedAmountResponse(
                            item.MigratedId,
                            null,
                            ResolveCountry(item.CountryId, item.LegacyCountryId),
                            item.AmountRm,
                            item.AmountPercent))
                        .ToList(),
                    otherSources
                        .OrderByDescending(item => item.AmountRm ?? decimal.MinValue)
                        .ThenBy(item => item.OtherSources)
                        .Select(item => new CompanyProfileFinancialNamedAmountResponse(
                            item.MigratedId,
                            item.OtherSources,
                            null,
                            item.AmountRm,
                            item.AmountPercent))
                        .ToList());
            }
        }

        var result = new CompanyProfileFinancialDetailsResponse(
            company.Id,
            company.MigratedId,
            company.CompanyName,
            company.RegistrationNo,
            company.NewSsmCompanyRegNo,
            null,
            company.LastSyncedAt,
            availableStatements,
            selectedStatement);

        return Ok(ApiResponse<CompanyProfileFinancialDetailsResponse>.Success(result));
    }

    [HttpGet("applicant-applications")]
    [HasPermission(Permissions.CompanyProfilesRead)]
    public async Task<ActionResult<ApiResponse<ApplicantApplicationListResponse>>> GetApplicantApplications(
        CancellationToken cancellationToken = default)
    {
        var companyQuery = await ApplyCompanyScopeAsync(dbContext.CompanyProfiles.AsNoTracking(), cancellationToken);

        var companyScope = companyQuery.Select(company => new
        {
            company.Id,
            company.CompanyName,
            company.RegistrationNo
        });

        var rows = await (
                from company in companyScope
                join detail in dbContext.CompanyProfileFinancialDetails.IgnoreQueryFilters().AsNoTracking()
                    on company.Id equals detail.CompanyProfileId
                where !detail.IsDeleted
                orderby detail.FinancialYear descending,
                        detail.EffectiveDate descending,
                        detail.LastSyncedAt descending,
                        company.CompanyName,
                        company.Id
                select new ApplicantApplicationListItemResponse(
                    company.Id,
                    company.CompanyName,
                    company.RegistrationNo,
                    detail.Id,
                    detail.MigratedId,
                    detail.LegacyProjectId,
                    detail.FinancialYear,
                    detail.EffectiveDate,
                    detail.ProjectStatusId,
                    detail.LastSyncedAt))
            .ToListAsync(cancellationToken);

        var companyCount = await companyScope.CountAsync(cancellationToken);

        var result = new ApplicantApplicationListResponse(
            rows,
            companyCount,
            rows.Count,
            rows.Where(item => item.EffectiveDate.HasValue)
                .OrderByDescending(item => item.EffectiveDate)
                .Select(item => item.EffectiveDate)
                .FirstOrDefault());

        return Ok(ApiResponse<ApplicantApplicationListResponse>.Success(result));
    }

    [HttpGet("{id:guid}/documents/{migratedDocumentId:long}/download")]
    [HasPermission(Permissions.CompanyProfilesRead)]
    public async Task<IActionResult> DownloadDocument(Guid id, long migratedDocumentId, CancellationToken cancellationToken = default)
    {
        var companyQuery = await ApplyCompanyScopeAsync(dbContext.CompanyProfiles.AsNoTracking(), cancellationToken);
        var company = await companyQuery
            .Where(item => item.Id == id)
            .Select(item => new { item.Id, item.CompanyName, item.MigratedId })
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            return NotFound("Company profile not found.");
        }

        var document = await dbContext.CompanyAttachmentDocuments
            .AsNoTracking()
            .Where(item => item.CompanyProfileId == id && item.MigratedId == migratedDocumentId && !item.IsDeleted)
            .Select(item => new
            {
                item.MigratedId,
                item.FileName,
                item.FileType,
                item.FileContent,
                item.SourceCreatedAt,
                item.LastSyncedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return NotFound("Document not found.");
        }

        if (document.FileContent is { Length: > 0 })
        {
            var downloadName = string.IsNullOrWhiteSpace(document.FileName)
                ? $"document-{document.MigratedId}"
                : document.FileName;
            var contentType = string.IsNullOrWhiteSpace(document.FileType)
                ? "application/octet-stream"
                : document.FileType;
            return File(document.FileContent, contentType, downloadName);
        }

        var fallbackName = string.IsNullOrWhiteSpace(document.FileName)
            ? $"document-{document.MigratedId}-metadata.txt"
            : $"{Path.GetFileNameWithoutExtension(document.FileName)}-metadata.txt";
        var fallbackContent = $"""
            IRPM Document Metadata Receipt
            Company: {company.CompanyName ?? "Unknown Company"}
            Source Company ID: {company.MigratedId?.ToString() ?? "N/A"}
            Document Migrated ID: {document.MigratedId}
            File Name: {document.FileName ?? "Not provided"}
            File Type: {document.FileType ?? "Not provided"}
            Source Created At: {(document.SourceCreatedAt.HasValue ? document.SourceCreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Not provided")}
            Last Synced At: {document.LastSyncedAt:yyyy-MM-dd HH:mm:ss}
            
            Note:
            The original attachment binary is not available in the framework database for this migrated record.
            This receipt was generated so the document row remains downloadable from IRPM.
            """;

        return File(Encoding.UTF8.GetBytes(fallbackContent), "text/plain; charset=utf-8", fallbackName);
    }

    private async Task<IQueryable<Domain.Entities.CompanyProfile>> ApplyCompanyScopeAsync(
        IQueryable<Domain.Entities.CompanyProfile> query,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return query.Where(_ => false);
        }

        var assignedCompanyIds = await dbContext.CompanyProfileUserAssignments
            .AsNoTracking()
            .Where(assignment => assignment.ApplicationUserId == currentUserService.UserId.Value && assignment.IsActive)
            .Select(assignment => assignment.CompanyProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (assignedCompanyIds.Count == 0)
        {
            return query;
        }

        return query.Where(company => assignedCompanyIds.Contains(company.Id));
    }
}
