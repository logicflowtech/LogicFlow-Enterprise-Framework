using System.Data;
using System.Data.Common;
using System.Text.Json;
using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/applications")]
public sealed class ApplicationTemplatesController(ApplicationDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("templates/{applicationCode}")]
    [HasPermission(Permissions.ApplicantApplicationsRead)]
    public async Task<ActionResult<ApiResponse<ApplicantApplicationTemplateResponse>>> GetTemplate(
        string applicationCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(applicationCode))
        {
            return BadRequest(ApiResponse<ApplicantApplicationTemplateResponse>.Failure("Application code is required."));
        }

        var normalizedCode = applicationCode.Trim().ToLowerInvariant();
        var template = await TryLoadTemplateFromDatabaseAsync(normalizedCode, cancellationToken)
            ?? BuildFallbackTemplate(normalizedCode);

        return Ok(ApiResponse<ApplicantApplicationTemplateResponse>.Success(template));
    }

    [HttpGet("startup/{applicationCode}")]
    [HasPermission(Permissions.ApplicantApplicationsRead)]
    public async Task<ActionResult<ApiResponse<ApplicantApplicationStartupResponse>>> GetStartup(
        string applicationCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(applicationCode))
        {
            return BadRequest(ApiResponse<ApplicantApplicationStartupResponse>.Failure("Application code is required."));
        }

        var normalizedCode = applicationCode.Trim().ToLowerInvariant();
        var template = await TryLoadTemplateFromDatabaseAsync(normalizedCode, cancellationToken)
            ?? BuildFallbackTemplate(normalizedCode);

        var response = await BuildStartupResponseAsync(template, cancellationToken);
        return Ok(ApiResponse<ApplicantApplicationStartupResponse>.Success(response));
    }

    [HttpPost]
    [HasPermission(Permissions.ApplicantApplicationsRead)]
    public async Task<ActionResult<ApiResponse<CreateApplicantApplicationResponse>>> CreateApplicantApplication(
        [FromBody] CreateApplicantApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApplicationCode))
        {
            return BadRequest(ApiResponse<CreateApplicantApplicationResponse>.Failure("Application code is required."));
        }

        if (string.IsNullOrWhiteSpace(request.SelectedApplicationValue))
        {
            return BadRequest(ApiResponse<CreateApplicantApplicationResponse>.Failure("Application selection is required."));
        }

        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized(ApiResponse<CreateApplicantApplicationResponse>.Failure("User context is not available."));
        }

        var normalizedCode = request.ApplicationCode.Trim().ToLowerInvariant();
        if (!await ApplicantTemplateTablesExistAsync(cancellationToken))
        {
            return BadRequest(ApiResponse<CreateApplicantApplicationResponse>.Failure("Applicant application tables are not available."));
        }

        var template = await LoadTemplateDefinitionAsync(normalizedCode, cancellationToken);
        if (template is null)
        {
            return NotFound(ApiResponse<CreateApplicantApplicationResponse>.Failure("Application template not found."));
        }

        var assignedCompany = await GetAssignedCompanyAsync(cancellationToken);
        if (assignedCompany is null)
        {
            return BadRequest(ApiResponse<CreateApplicantApplicationResponse>.Failure("No assigned company profile was found for the current user."));
        }

        var applicationId = Guid.NewGuid();
        var orderedSections = template.Sections
            .OrderBy(section => section.DisplayOrder)
            .Select(section => new CreatedSectionRow(
                Guid.NewGuid(),
                section.TemplateSectionId,
                section.SectionCode,
                section.Title,
                section.FormDefinitionVersionId))
            .ToArray();

        var firstVisibleSection = template.Sections
            .Where(section => section.IsVisible)
            .OrderBy(section => section.DisplayOrder)
            .FirstOrDefault();
        var firstCreatedSection = firstVisibleSection is null
            ? null
            : orderedSections.FirstOrDefault(section => string.Equals(section.SectionCode, firstVisibleSection.SectionCode, StringComparison.OrdinalIgnoreCase));

        var startupPayload = new
        {
            request.SelectedApplicationValue,
            request.SectorValue,
            ExemptionTypeValues = request.ExemptionTypeValues.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            ApplicationTypeSelections = request.ApplicationTypeSelections
                .Where(item => !string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase),
            MarketValues = request.MarketValues.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            request.MainIndustryValue
        };

        var applicationNo = BuildApplicationNumber(template.TemplateCode);
        var userIdText = currentUserService.UserId.Value.ToString();
        var startupDataJson = JsonSerializer.Serialize(startupPayload);
        var versionSnapshotJson = JsonSerializer.Serialize(new
        {
            template.ApplicationCode,
            template.TemplateCode,
            template.TemplateName,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await InsertApplicantApplicationAsync(
                connection,
                transaction,
                applicationId,
                applicationNo,
                userIdText,
                assignedCompany.Value,
                template,
                firstCreatedSection,
                firstVisibleSection,
                startupDataJson,
                versionSnapshotJson,
                cancellationToken);

            foreach (var section in orderedSections)
            {
                await InsertApplicantApplicationSectionAsync(
                    connection,
                    transaction,
                    applicationId,
                    section,
                    firstVisibleSection,
                    cancellationToken);
            }

            await InsertApplicantApplicationAuditLogAsync(
                connection,
                transaction,
                applicationId,
                "Created",
                userIdText,
                "Application initiated from startup intake form.",
                startupDataJson,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var response = new CreateApplicantApplicationResponse(
            applicationId,
            applicationNo,
            template.ApplicationCode,
            template.TemplateCode,
            template.TemplateName,
            firstVisibleSection?.SectionCode);

        return Ok(ApiResponse<CreateApplicantApplicationResponse>.Success(response));
    }

    [HttpGet("{applicationId:guid}/company-section")]
    [HasPermission(Permissions.ApplicantApplicationsRead)]
    public async Task<ActionResult<ApiResponse<ApplicantApplicationCompanySectionResponse>>> GetCompanySection(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized(ApiResponse<ApplicantApplicationCompanySectionResponse>.Failure("User context is not available."));
        }

        if (!await ApplicantCompanySectionTablesExistAsync(cancellationToken))
        {
            return BadRequest(ApiResponse<ApplicantApplicationCompanySectionResponse>.Failure("Application company section tables are not available."));
        }

        var application = await ReadOwnedApplicantApplicationAsync(applicationId, currentUserService.UserId.Value, cancellationToken);
        if (application is null)
        {
            return NotFound(ApiResponse<ApplicantApplicationCompanySectionResponse>.Failure("Applicant application not found."));
        }

        var assignedCompanies = await LoadAssignedCompanyOptionsAsync(currentUserService.UserId.Value, application.CompanyProfileId, cancellationToken);
        if (assignedCompanies.Count == 0)
        {
            return BadRequest(ApiResponse<ApplicantApplicationCompanySectionResponse>.Failure("No assigned company profile was found for the current user."));
        }

        var selectedCompanyProfileId = assignedCompanies.FirstOrDefault(item => item.IsSelected)?.CompanyProfileId
            ?? assignedCompanies.First().CompanyProfileId;

        var existingProfile = await LoadApplicationCompanyProfileAsync(applicationId, cancellationToken);
        if (existingProfile is null)
        {
            await PullAndPersistCompanySectionAsync(applicationId, selectedCompanyProfileId, cancellationToken);
        }

        var response = await BuildCompanySectionResponseAsync(applicationId, selectedCompanyProfileId, cancellationToken);
        return Ok(ApiResponse<ApplicantApplicationCompanySectionResponse>.Success(response));
    }

    [HttpPost("{applicationId:guid}/company-section/select-company")]
    [HasPermission(Permissions.ApplicantApplicationsRead)]
    public async Task<ActionResult<ApiResponse<ApplicantApplicationCompanySectionResponse>>> SelectCompanySectionCompany(
        Guid applicationId,
        [FromBody] SelectApplicantApplicationCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized(ApiResponse<ApplicantApplicationCompanySectionResponse>.Failure("User context is not available."));
        }

        if (request.CompanyProfileId == Guid.Empty)
        {
            return BadRequest(ApiResponse<ApplicantApplicationCompanySectionResponse>.Failure("Company selection is required."));
        }

        if (!await ApplicantCompanySectionTablesExistAsync(cancellationToken))
        {
            return BadRequest(ApiResponse<ApplicantApplicationCompanySectionResponse>.Failure("Application company section tables are not available."));
        }

        var application = await ReadOwnedApplicantApplicationAsync(applicationId, currentUserService.UserId.Value, cancellationToken);
        if (application is null)
        {
            return NotFound(ApiResponse<ApplicantApplicationCompanySectionResponse>.Failure("Applicant application not found."));
        }

        var allowedCompanyIds = await dbContext.CompanyProfileUserAssignments
            .AsNoTracking()
            .Where(assignment => assignment.ApplicationUserId == currentUserService.UserId.Value && assignment.IsActive && !assignment.IsDeleted)
            .Select(assignment => assignment.CompanyProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!allowedCompanyIds.Contains(request.CompanyProfileId))
        {
            return BadRequest(ApiResponse<ApplicantApplicationCompanySectionResponse>.Failure("The selected company is not assigned to the current user."));
        }

        await PullAndPersistCompanySectionAsync(applicationId, request.CompanyProfileId, cancellationToken);
        var response = await BuildCompanySectionResponseAsync(applicationId, request.CompanyProfileId, cancellationToken);
        return Ok(ApiResponse<ApplicantApplicationCompanySectionResponse>.Success(response));
    }

    private async Task<bool> ApplicantCompanySectionTablesExistAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CAST(
                CASE
                    WHEN OBJECT_ID(N'dbo.ApplicationCompanyProfiles', N'U') IS NOT NULL
                     AND OBJECT_ID(N'dbo.ApplicationCompanyDirectors', N'U') IS NOT NULL
                     AND OBJECT_ID(N'dbo.ApplicationCompanyContactPersons', N'U') IS NOT NULL
                    THEN 1
                    ELSE 0
                END AS int) AS [Value]
            """;

        var result = await dbContext.Database.SqlQueryRaw<int>(sql).FirstAsync(cancellationToken);
        return result == 1;
    }

    private async Task<ApplicantApplicationRow?> ReadOwnedApplicantApplicationAsync(Guid applicationId, Guid userId, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1)
                Id,
                CompanyProfileId
            FROM dbo.ApplicantApplications
            WHERE Id = @Id
              AND ApplicantUserId = @ApplicantUserId
              AND IsDeleted = 0;
            """;

        AddParameter(command, "@Id", applicationId);
        AddParameter(command, "@ApplicantUserId", userId.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ApplicantApplicationRow(
            reader.GetGuid(0),
            reader.IsDBNull(1) ? null : reader.GetGuid(1));
    }

    private async Task<IReadOnlyList<ApplicantApplicationCompanyOptionResponse>> LoadAssignedCompanyOptionsAsync(
        Guid userId,
        Guid? selectedCompanyProfileId,
        CancellationToken cancellationToken)
    {
        var rows = await (
                from assignment in dbContext.CompanyProfileUserAssignments.AsNoTracking()
                join company in dbContext.CompanyProfiles.AsNoTracking() on assignment.CompanyProfileId equals company.Id
                where assignment.ApplicationUserId == userId
                    && assignment.IsActive
                    && !assignment.IsDeleted
                orderby company.CompanyName, company.Id
                select new
                {
                    company.Id,
                    company.MigratedId,
                    company.CompanyName,
                    company.RegistrationNo
                })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows
            .Select(company => new ApplicantApplicationCompanyOptionResponse(
                company.Id,
                company.MigratedId,
                company.CompanyName ?? "Unnamed Company",
                company.RegistrationNo,
                selectedCompanyProfileId.HasValue && company.Id == selectedCompanyProfileId.Value))
            .ToArray();
    }

    private async Task PullAndPersistCompanySectionAsync(Guid applicationId, Guid companyProfileId, CancellationToken cancellationToken)
    {
        var source = await LoadCompanySourceSnapshotAsync(companyProfileId, cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException("Selected company profile could not be loaded from the IRPM-synced source.");
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var applicationCompanyProfileId = await UpsertApplicationCompanyProfileAsync(
                connection,
                transaction,
                applicationId,
                source,
                cancellationToken);

            await ReplaceApplicationCompanyDirectorsAsync(
                connection,
                transaction,
                applicationCompanyProfileId,
                source.Directors,
                cancellationToken);

            await ReplaceApplicationCompanyContactPersonsAsync(
                connection,
                transaction,
                applicationCompanyProfileId,
                source.ContactPersons,
                cancellationToken);

            await UpdateApplicantApplicationCompanyProfileAsync(
                connection,
                transaction,
                applicationId,
                companyProfileId,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<ApplicantApplicationCompanySectionResponse> BuildCompanySectionResponseAsync(
        Guid applicationId,
        Guid selectedCompanyProfileId,
        CancellationToken cancellationToken)
    {
        var assignedCompanies = currentUserService.UserId.HasValue
            ? await LoadAssignedCompanyOptionsAsync(currentUserService.UserId.Value, selectedCompanyProfileId, cancellationToken)
            : [];

        var profile = await LoadApplicationCompanyProfileAsync(applicationId, cancellationToken);
        return new ApplicantApplicationCompanySectionResponse(applicationId, assignedCompanies, profile);
    }

    private async Task<CompanySourceSnapshot?> LoadCompanySourceSnapshotAsync(Guid companyProfileId, CancellationToken cancellationToken)
    {
        var source = await dbContext.CompanyProfiles
            .AsNoTracking()
            .Where(item => item.Id == companyProfileId)
            .Select(item => new
            {
                item.Id,
                item.MigratedId,
                item.CompanyName,
                item.RegistrationNo,
                item.DateOfIncorporation,
                item.RegistrationDate,
                item.TelephoneNo,
                item.FaxNo,
                item.Website,
                item.Email,
                item.IncomeTaxNo,
                item.EpfNo,
                item.SocsoNo,
                item.UserId,
                item.CompanySignatureId,
                item.CompanyType,
                item.IsCompanyCertified,
                item.CompanyApprovalStatus,
                item.IsPaid,
                item.IsCompanyLocal,
                item.CreatedBySourceUserId,
                item.SourceCreatedDateTime,
                item.ModifiedBySourceUserId,
                item.SourceModifiedDateTime,
                item.AddressId,
                item.LegacyAddressId,
                item.BackgroundDescription1,
                item.TotalEmployment,
                item.LastSyncedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (source is null)
        {
            return null;
        }

        var address = source.AddressId.HasValue
            ? await dbContext.Addresses
                .AsNoTracking()
                .Where(item => item.Id == source.AddressId.Value)
                .Select(item => new CompanySourceAddress(
                    item.MigratedId,
                    item.AddressLine1,
                    item.AddressLine2,
                    item.AddressLine3,
                    item.CountryName,
                    item.StateName,
                    item.CityName,
                    item.Postcode))
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var directors = await dbContext.CompanyBoardDirectors
            .AsNoTracking()
            .Where(item => item.CompanyProfileId == companyProfileId && !item.IsDeleted)
            .OrderBy(item => item.Name)
            .Select(item => new CompanySourceDirector(
                item.MigratedId,
                item.Name,
                item.LegacyNationalityId,
                item.LegacyNationalityId.HasValue ? $"Nationality {item.LegacyNationalityId.Value}" : null,
                item.SharePercentage,
                item.SourceCreatedByLegacyUserId,
                item.SourceCreatedAt,
                item.SourceUpdatedByLegacyUserId,
                item.SourceUpdatedAt))
            .ToListAsync(cancellationToken);

        var contacts = await (
                from assignment in dbContext.CompanyProfileUserAssignments.AsNoTracking()
                join user in dbContext.Users.AsNoTracking() on assignment.ApplicationUserId equals user.Id
                join profile in dbContext.UserProfiles.AsNoTracking() on user.Id equals profile.ApplicationUserId into profileJoin
                from profile in profileJoin.DefaultIfEmpty()
                where assignment.CompanyProfileId == companyProfileId
                    && assignment.IsActive
                    && !assignment.IsDeleted
                orderby user.FullName, user.Id
                select new CompanySourceContact(
                    assignment.LegacyContactPersonId,
                    profile != null ? profile.TitleDisplayName : null,
                    user.FullName,
                    profile != null
                        ? (!string.IsNullOrWhiteSpace(profile.CustomDesignationName)
                            ? profile.CustomDesignationName
                            : profile.TitleDisplayName)
                        : null,
                    user.Email,
                    user.PhoneNumber))
            .ToListAsync(cancellationToken);

        return new CompanySourceSnapshot(
            source.Id,
            source.MigratedId,
            source.CompanyName,
            source.RegistrationNo,
            source.DateOfIncorporation,
            source.RegistrationDate,
            source.TelephoneNo,
            source.FaxNo,
            source.Website,
            source.Email,
            source.IncomeTaxNo,
            source.EpfNo,
            source.SocsoNo,
            source.UserId,
            source.CompanySignatureId,
            source.CompanyType,
            source.IsCompanyCertified,
            source.CompanyApprovalStatus,
            source.IsPaid,
            source.IsCompanyLocal,
            source.CreatedBySourceUserId,
            source.SourceCreatedDateTime,
            source.ModifiedBySourceUserId,
            source.SourceModifiedDateTime,
            source.LegacyAddressId,
            source.BackgroundDescription1,
            source.TotalEmployment,
            address,
            directors,
            contacts,
            source.LastSyncedAt);
    }

    private async Task<Guid> UpsertApplicationCompanyProfileAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid applicationId,
        CompanySourceSnapshot source,
        CancellationToken cancellationToken)
    {
        Guid? existingId;
        await using (var selectCommand = connection.CreateCommand())
        {
            selectCommand.Transaction = transaction;
            selectCommand.CommandText = """
                SELECT TOP (1) Id
                FROM dbo.ApplicationCompanyProfiles
                WHERE ApplicationId = @ApplicationId
                  AND IsDeleted = 0;
                """;
            AddParameter(selectCommand, "@ApplicationId", applicationId);
            var scalar = await selectCommand.ExecuteScalarAsync(cancellationToken);
            existingId = scalar is Guid guid ? guid : null;
        }

        var profileId = existingId ?? Guid.NewGuid();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = existingId.HasValue
            ? """
                UPDATE dbo.ApplicationCompanyProfiles
                SET
                    LegacyParticularOfCompanyId = @LegacyParticularOfCompanyId,
                    LegacyApplicationId = NULL,
                    LegacyCompanyId = NULL,
                    LegacyAddressId = @LegacyAddressId,
                    CompanyName = @CompanyName,
                    RegistrationNumber = @RegistrationNumber,
                    RegistrationTypeId = @RegistrationTypeId,
                    RegistrationTypeLabel = @RegistrationTypeLabel,
                    DateOfIncorporation = @DateOfIncorporation,
                    RegistrationDate = @RegistrationDate,
                    TelephoneNumber = @TelephoneNumber,
                    FaxNumber = @FaxNumber,
                    Website = @Website,
                    Email = @Email,
                    IncomeTaxNo = @IncomeTaxNo,
                    EpfNo = @EpfNo,
                    SocsoNo = @SocsoNo,
                    LegacyUserId = @LegacyUserId,
                    LegacyCompanySignatureId = @LegacyCompanySignatureId,
                    LegacyCompanyTypeId = @LegacyCompanyTypeId,
                    IsCompanyCertified = @IsCompanyCertified,
                    LegacyCompanyApprovalStatusId = @LegacyCompanyApprovalStatusId,
                    IsPaid = @IsPaid,
                    IsCompanyLocal = @IsCompanyLocal,
                    TotalEmployment = @TotalEmployment,
                    LegacyAgencyBranchId = NULL,
                    CompanyBackground = @CompanyBackground,
                    RegisteredAddress1 = @RegisteredAddress1,
                    RegisteredAddress2 = @RegisteredAddress2,
                    RegisteredAddress3 = @RegisteredAddress3,
                    RegisteredCountryName = @RegisteredCountryName,
                    RegisteredStateName = @RegisteredStateName,
                    RegisteredCityName = @RegisteredCityName,
                    RegisteredPostcode = @RegisteredPostcode,
                    IsCorrespondenceSameAsRegistered = 1,
                    CorrespondenceAddress1 = @RegisteredAddress1,
                    CorrespondenceAddress2 = @RegisteredAddress2,
                    CorrespondenceAddress3 = @RegisteredAddress3,
                    CorrespondenceCountryName = @RegisteredCountryName,
                    CorrespondenceStateName = @RegisteredStateName,
                    CorrespondenceCityName = @RegisteredCityName,
                    CorrespondencePostcode = @RegisteredPostcode,
                    CustomsControlStationCode = NULL,
                    CustomsControlStationName = NULL,
                    SourcePulledAt = SYSUTCDATETIME(),
                    SourceCreatedByLegacyUserId = @SourceCreatedByLegacyUserId,
                    SourceCreatedAt = @SourceCreatedAt,
                    SourceUpdatedByLegacyUserId = @SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt = @SourceUpdatedAt,
                    UpdatedAt = SYSUTCDATETIME(),
                    UpdatedBy = @Actor
                WHERE Id = @Id;
                """
            : """
                INSERT INTO dbo.ApplicationCompanyProfiles
                (
                    Id,
                    ApplicationId,
                    LegacyParticularOfCompanyId,
                    LegacyApplicationId,
                    LegacyCompanyId,
                    LegacyAddressId,
                    CompanyName,
                    RegistrationNumber,
                    RegistrationTypeId,
                    RegistrationTypeLabel,
                    DateOfIncorporation,
                    RegistrationDate,
                    TelephoneNumber,
                    FaxNumber,
                    Website,
                    Email,
                    IncomeTaxNo,
                    EpfNo,
                    SocsoNo,
                    LegacyUserId,
                    LegacyCompanySignatureId,
                    LegacyCompanyTypeId,
                    IsCompanyCertified,
                    LegacyCompanyApprovalStatusId,
                    IsPaid,
                    IsCompanyLocal,
                    TotalEmployment,
                    CompanyBackground,
                    RegisteredAddress1,
                    RegisteredAddress2,
                    RegisteredAddress3,
                    RegisteredCountryName,
                    RegisteredStateName,
                    RegisteredCityName,
                    RegisteredPostcode,
                    IsCorrespondenceSameAsRegistered,
                    CorrespondenceAddress1,
                    CorrespondenceAddress2,
                    CorrespondenceAddress3,
                    CorrespondenceCountryName,
                    CorrespondenceStateName,
                    CorrespondenceCityName,
                    CorrespondencePostcode,
                    SourcePulledAt,
                    SourceCreatedByLegacyUserId,
                    SourceCreatedAt,
                    SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt,
                    CreatedAt,
                    CreatedBy,
                    IsDeleted
                )
                VALUES
                (
                    @Id,
                    @ApplicationId,
                    @LegacyParticularOfCompanyId,
                    NULL,
                    NULL,
                    @LegacyAddressId,
                    @CompanyName,
                    @RegistrationNumber,
                    @RegistrationTypeId,
                    @RegistrationTypeLabel,
                    @DateOfIncorporation,
                    @RegistrationDate,
                    @TelephoneNumber,
                    @FaxNumber,
                    @Website,
                    @Email,
                    @IncomeTaxNo,
                    @EpfNo,
                    @SocsoNo,
                    @LegacyUserId,
                    @LegacyCompanySignatureId,
                    @LegacyCompanyTypeId,
                    @IsCompanyCertified,
                    @LegacyCompanyApprovalStatusId,
                    @IsPaid,
                    @IsCompanyLocal,
                    @TotalEmployment,
                    @CompanyBackground,
                    @RegisteredAddress1,
                    @RegisteredAddress2,
                    @RegisteredAddress3,
                    @RegisteredCountryName,
                    @RegisteredStateName,
                    @RegisteredCityName,
                    @RegisteredPostcode,
                    1,
                    @RegisteredAddress1,
                    @RegisteredAddress2,
                    @RegisteredAddress3,
                    @RegisteredCountryName,
                    @RegisteredStateName,
                    @RegisteredCityName,
                    @RegisteredPostcode,
                    SYSUTCDATETIME(),
                    @SourceCreatedByLegacyUserId,
                    @SourceCreatedAt,
                    @SourceUpdatedByLegacyUserId,
                    @SourceUpdatedAt,
                    SYSUTCDATETIME(),
                    @Actor,
                    0
                );
                """;

        AddParameter(command, "@Id", profileId);
        AddParameter(command, "@ApplicationId", applicationId);
        AddParameter(command, "@LegacyParticularOfCompanyId", source.LegacyParticularOfCompanyId);
        AddParameter(command, "@LegacyAddressId", source.LegacyAddressId);
        AddParameter(command, "@CompanyName", source.CompanyName);
        AddParameter(command, "@RegistrationNumber", source.RegistrationNumber);
        AddParameter(command, "@RegistrationTypeId", source.LegacyCompanyTypeId);
        AddParameter(command, "@RegistrationTypeLabel", source.LegacyCompanyTypeId.HasValue ? $"Legacy Company Type {source.LegacyCompanyTypeId.Value}" : null);
        AddParameter(command, "@DateOfIncorporation", source.DateOfIncorporation);
        AddParameter(command, "@RegistrationDate", source.RegistrationDate);
        AddParameter(command, "@TelephoneNumber", source.TelephoneNumber);
        AddParameter(command, "@FaxNumber", source.FaxNumber);
        AddParameter(command, "@Website", source.Website);
        AddParameter(command, "@Email", source.Email);
        AddParameter(command, "@IncomeTaxNo", source.IncomeTaxNo);
        AddParameter(command, "@EpfNo", source.EpfNo);
        AddParameter(command, "@SocsoNo", source.SocsoNo);
        AddParameter(command, "@LegacyUserId", source.LegacyUserId);
        AddParameter(command, "@LegacyCompanySignatureId", source.LegacyCompanySignatureId);
        AddParameter(command, "@LegacyCompanyTypeId", source.LegacyCompanyTypeId);
        AddParameter(command, "@IsCompanyCertified", source.IsCompanyCertified);
        AddParameter(command, "@LegacyCompanyApprovalStatusId", source.LegacyCompanyApprovalStatusId);
        AddParameter(command, "@IsPaid", source.IsPaid);
        AddParameter(command, "@IsCompanyLocal", source.IsCompanyLocal);
        AddParameter(command, "@TotalEmployment", source.TotalEmployment);
        AddParameter(command, "@CompanyBackground", source.CompanyBackground);
        AddParameter(command, "@RegisteredAddress1", source.Address?.AddressLine1);
        AddParameter(command, "@RegisteredAddress2", source.Address?.AddressLine2);
        AddParameter(command, "@RegisteredAddress3", source.Address?.AddressLine3);
        AddParameter(command, "@RegisteredCountryName", source.Address?.Country);
        AddParameter(command, "@RegisteredStateName", source.Address?.State);
        AddParameter(command, "@RegisteredCityName", source.Address?.City);
        AddParameter(command, "@RegisteredPostcode", source.Address?.Postcode);
        AddParameter(command, "@SourceCreatedByLegacyUserId", source.SourceCreatedByLegacyUserId);
        AddParameter(command, "@SourceCreatedAt", source.SourceCreatedAt);
        AddParameter(command, "@SourceUpdatedByLegacyUserId", source.SourceUpdatedByLegacyUserId);
        AddParameter(command, "@SourceUpdatedAt", source.SourceUpdatedAt);
        AddParameter(command, "@Actor", currentUserService.UserName ?? "system");

        await command.ExecuteNonQueryAsync(cancellationToken);
        return profileId;
    }

    private static async Task ReplaceApplicationCompanyDirectorsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid applicationCompanyProfileId,
        IReadOnlyList<CompanySourceDirector> directors,
        CancellationToken cancellationToken)
    {
        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = """
                DELETE FROM dbo.ApplicationCompanyDirectors
                WHERE ApplicationCompanyProfileId = @ApplicationCompanyProfileId;
                """;
            AddParameter(deleteCommand, "@ApplicationCompanyProfileId", applicationCompanyProfileId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        for (var index = 0; index < directors.Count; index++)
        {
            var item = directors[index];
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = """
                INSERT INTO dbo.ApplicationCompanyDirectors
                (
                    Id,
                    ApplicationCompanyProfileId,
                    LegacyDirectorId,
                    DisplayOrder,
                    DirectorName,
                    LegacyNationalityId,
                    NationalityName,
                    SharesHeldPercent,
                    SourceCreatedByLegacyUserId,
                    SourceCreatedAt,
                    SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt,
                    CreatedAt,
                    CreatedBy,
                    IsDeleted
                )
                VALUES
                (
                    @Id,
                    @ApplicationCompanyProfileId,
                    @LegacyDirectorId,
                    @DisplayOrder,
                    @DirectorName,
                    @LegacyNationalityId,
                    @NationalityName,
                    @SharesHeldPercent,
                    @SourceCreatedByLegacyUserId,
                    @SourceCreatedAt,
                    @SourceUpdatedByLegacyUserId,
                    @SourceUpdatedAt,
                    SYSUTCDATETIME(),
                    @Actor,
                    0
                );
                """;

            AddParameter(insertCommand, "@Id", Guid.NewGuid());
            AddParameter(insertCommand, "@ApplicationCompanyProfileId", applicationCompanyProfileId);
            AddParameter(insertCommand, "@LegacyDirectorId", item.LegacyDirectorId);
            AddParameter(insertCommand, "@DisplayOrder", index + 1);
            AddParameter(insertCommand, "@DirectorName", item.DirectorName);
            AddParameter(insertCommand, "@LegacyNationalityId", item.LegacyNationalityId);
            AddParameter(insertCommand, "@NationalityName", item.NationalityName);
            AddParameter(insertCommand, "@SharesHeldPercent", item.SharesHeldPercent);
            AddParameter(insertCommand, "@SourceCreatedByLegacyUserId", item.SourceCreatedByLegacyUserId);
            AddParameter(insertCommand, "@SourceCreatedAt", item.SourceCreatedAt);
            AddParameter(insertCommand, "@SourceUpdatedByLegacyUserId", item.SourceUpdatedByLegacyUserId);
            AddParameter(insertCommand, "@SourceUpdatedAt", item.SourceUpdatedAt);
            AddParameter(insertCommand, "@Actor", "system");

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task ReplaceApplicationCompanyContactPersonsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid applicationCompanyProfileId,
        IReadOnlyList<CompanySourceContact> contacts,
        CancellationToken cancellationToken)
    {
        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = """
                DELETE FROM dbo.ApplicationCompanyContactPersons
                WHERE ApplicationCompanyProfileId = @ApplicationCompanyProfileId;
                """;
            AddParameter(deleteCommand, "@ApplicationCompanyProfileId", applicationCompanyProfileId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        for (var index = 0; index < contacts.Count; index++)
        {
            var item = contacts[index];
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = """
                INSERT INTO dbo.ApplicationCompanyContactPersons
                (
                    Id,
                    ApplicationCompanyProfileId,
                    LegacyParticularContactPersonId,
                    LegacyContactPersonId,
                    DisplayOrder,
                    Title,
                    FullName,
                    Designation,
                    Email,
                    PhoneNo,
                    CreatedAt,
                    CreatedBy,
                    IsDeleted
                )
                VALUES
                (
                    @Id,
                    @ApplicationCompanyProfileId,
                    @LegacyParticularContactPersonId,
                    @LegacyContactPersonId,
                    @DisplayOrder,
                    @Title,
                    @FullName,
                    @Designation,
                    @Email,
                    @PhoneNo,
                    SYSUTCDATETIME(),
                    @Actor,
                    0
                );
                """;

            AddParameter(insertCommand, "@Id", Guid.NewGuid());
            AddParameter(insertCommand, "@ApplicationCompanyProfileId", applicationCompanyProfileId);
            AddParameter(insertCommand, "@LegacyParticularContactPersonId", item.LegacyContactPersonId);
            AddParameter(insertCommand, "@LegacyContactPersonId", item.LegacyContactPersonId);
            AddParameter(insertCommand, "@DisplayOrder", index + 1);
            AddParameter(insertCommand, "@Title", item.Title);
            AddParameter(insertCommand, "@FullName", item.FullName);
            AddParameter(insertCommand, "@Designation", item.Designation);
            AddParameter(insertCommand, "@Email", item.Email);
            AddParameter(insertCommand, "@PhoneNo", item.PhoneNo);
            AddParameter(insertCommand, "@Actor", "system");

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task UpdateApplicantApplicationCompanyProfileAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid applicationId,
        Guid companyProfileId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE dbo.ApplicantApplications
            SET CompanyProfileId = @CompanyProfileId,
                UpdatedAt = SYSUTCDATETIME(),
                LastSavedAt = SYSUTCDATETIME()
            WHERE Id = @ApplicationId;
            """;

        AddParameter(command, "@ApplicationId", applicationId);
        AddParameter(command, "@CompanyProfileId", companyProfileId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<ApplicantApplicationCompanyProfileResponse?> LoadApplicationCompanyProfileAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        Guid profileId;
        ApplicantApplicationCompanyProfileResponse profile;
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1)
                Id,
                (SELECT TOP (1) CompanyProfileId FROM dbo.ApplicantApplications WHERE Id = @ApplicationId),
                LegacyParticularOfCompanyId,
                CompanyName,
                RegistrationNumber,
                RegistrationTypeLabel,
                DateOfIncorporation,
                RegistrationDate,
                TelephoneNumber,
                FaxNumber,
                Website,
                Email,
                IncomeTaxNo,
                EpfNo,
                SocsoNo,
                TotalEmployment,
                CompanyBackground,
                RegisteredAddress1,
                RegisteredAddress2,
                RegisteredAddress3,
                RegisteredCountryName,
                RegisteredStateName,
                RegisteredCityName,
                RegisteredPostcode,
                IsCorrespondenceSameAsRegistered,
                CorrespondenceAddress1,
                CorrespondenceAddress2,
                CorrespondenceAddress3,
                CorrespondenceCountryName,
                CorrespondenceStateName,
                CorrespondenceCityName,
                CorrespondencePostcode,
                CustomsControlStationName,
                SourcePulledAt
            FROM dbo.ApplicationCompanyProfiles
            WHERE ApplicationId = @ApplicationId
              AND IsDeleted = 0;
            """;
        AddParameter(command, "@ApplicationId", applicationId);

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            profileId = reader.GetGuid(0);
            profile = new ApplicantApplicationCompanyProfileResponse(
                profileId,
                reader.IsDBNull(1) ? Guid.Empty : reader.GetGuid(1),
                reader.IsDBNull(2) ? null : reader.GetInt64(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetString(12),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetString(14),
                reader.IsDBNull(15) ? null : reader.GetInt32(15),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                reader.IsDBNull(17) ? null : reader.GetString(17),
                reader.IsDBNull(18) ? null : reader.GetString(18),
                reader.IsDBNull(19) ? null : reader.GetString(19),
                reader.IsDBNull(20) ? null : reader.GetString(20),
                reader.IsDBNull(21) ? null : reader.GetString(21),
                reader.IsDBNull(22) ? null : reader.GetString(22),
                reader.IsDBNull(23) ? null : reader.GetString(23),
                !reader.IsDBNull(24) && reader.GetBoolean(24),
                reader.IsDBNull(25) ? null : reader.GetString(25),
                reader.IsDBNull(26) ? null : reader.GetString(26),
                reader.IsDBNull(27) ? null : reader.GetString(27),
                reader.IsDBNull(28) ? null : reader.GetString(28),
                reader.IsDBNull(29) ? null : reader.GetString(29),
                reader.IsDBNull(30) ? null : reader.GetString(30),
                reader.IsDBNull(31) ? null : reader.GetString(31),
                reader.IsDBNull(32) ? null : reader.GetString(32),
                reader.IsDBNull(33) ? null : reader.GetDateTime(33),
                [],
                []);
        }

        var directors = await ReadApplicationCompanyDirectorsAsync(connection, profileId, cancellationToken);
        var contacts = await ReadApplicationCompanyContactPersonsAsync(connection, profileId, cancellationToken);
        return profile with { Directors = directors, ContactPersons = contacts };
    }

    private static async Task<IReadOnlyList<ApplicantApplicationCompanyDirectorResponse>> ReadApplicationCompanyDirectorsAsync(
        DbConnection connection,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DirectorName, NationalityName, SharesHeldPercent
            FROM dbo.ApplicationCompanyDirectors
            WHERE ApplicationCompanyProfileId = @ApplicationCompanyProfileId
              AND IsDeleted = 0
            ORDER BY DisplayOrder, DirectorName;
            """;
        AddParameter(command, "@ApplicationCompanyProfileId", profileId);

        var items = new List<ApplicantApplicationCompanyDirectorResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ApplicantApplicationCompanyDirectorResponse(
                reader.IsDBNull(0) ? null : reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetDecimal(2)));
        }

        return items;
    }

    private static async Task<IReadOnlyList<ApplicantApplicationCompanyContactPersonResponse>> ReadApplicationCompanyContactPersonsAsync(
        DbConnection connection,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Title, FullName, Designation, Email, PhoneNo
            FROM dbo.ApplicationCompanyContactPersons
            WHERE ApplicationCompanyProfileId = @ApplicationCompanyProfileId
              AND IsDeleted = 0
            ORDER BY DisplayOrder, FullName;
            """;
        AddParameter(command, "@ApplicationCompanyProfileId", profileId);

        var items = new List<ApplicantApplicationCompanyContactPersonResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ApplicantApplicationCompanyContactPersonResponse(
                reader.IsDBNull(0) ? null : reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4)));
        }

        return items;
    }

    private async Task<ApplicantApplicationTemplateResponse?> TryLoadTemplateFromDatabaseAsync(string applicationCode, CancellationToken cancellationToken)
    {
        var templateDefinition = await LoadTemplateDefinitionAsync(applicationCode, cancellationToken);
        return templateDefinition is null
            ? null
            : new ApplicantApplicationTemplateResponse(
                templateDefinition.ApplicationCode,
                templateDefinition.TemplateCode,
                templateDefinition.TemplateName,
                templateDefinition.Description,
                templateDefinition.Sections
                    .Select(section => new ApplicantApplicationTemplateSectionResponse(
                        section.SectionCode,
                        section.Title,
                        section.DisplayOrder,
                        section.SectionTypeCode,
                        section.SystemRouteKey,
                        section.SystemComponentKey,
                        section.StepIcon,
                        section.IsVisible,
                        section.IsRequired,
                        section.ValidationMode,
                        section.Form))
                    .ToArray());
    }

    private async Task<TemplateDefinition?> LoadTemplateDefinitionAsync(string applicationCode, CancellationToken cancellationToken)
    {
        if (!await ApplicantTemplateTablesExistAsync(cancellationToken))
        {
            return null;
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var templateRow = await ReadTemplateRowAsync(connection, applicationCode, cancellationToken);
        if (templateRow is null)
        {
            return null;
        }

        var sectionRows = await ReadSectionRowsAsync(connection, templateRow.TemplateVersionId, cancellationToken);
        if (sectionRows.Count == 0)
        {
            return null;
        }

        var formVersionIds = sectionRows
            .Where(section => section.FormDefinitionVersionId.HasValue)
            .Select(section => section.FormDefinitionVersionId!.Value)
            .Distinct()
            .ToArray();

        var fieldsByFormVersion = await ReadFieldsByFormVersionAsync(connection, formVersionIds, cancellationToken);
        var sections = sectionRows
            .Select(section =>
            {
                ApplicantApplicationTemplateFormResponse? form = null;
                if (section.FormDefinitionVersionId.HasValue &&
                    !string.IsNullOrWhiteSpace(section.FormCode) &&
                    !string.IsNullOrWhiteSpace(section.FormName) &&
                    section.FormVersionNumber.HasValue)
                {
                    form = new ApplicantApplicationTemplateFormResponse(
                        section.FormDefinitionVersionId.Value,
                        section.FormCode,
                        section.FormName,
                        section.FormVersionNumber.Value,
                        fieldsByFormVersion.GetValueOrDefault(section.FormDefinitionVersionId.Value) ?? []);
                }

                return new TemplateSectionDefinition(
                    section.TemplateSectionId,
                    section.SectionCode,
                    section.Title,
                    section.DisplayOrder,
                    section.SectionTypeCode,
                    section.SystemRouteKey,
                    section.SystemComponentKey,
                    section.StepIcon,
                    section.IsVisible,
                    section.IsRequired,
                    section.ValidationMode,
                    form);
            })
            .OrderBy(section => section.DisplayOrder)
            .ToArray();

        return new TemplateDefinition(
            applicationCode,
            templateRow.TemplateCode,
            templateRow.TemplateName,
            templateRow.Description,
            sections)
        {
            TemplateId = templateRow.TemplateId,
            TemplateVersionId = templateRow.TemplateVersionId,
            ApplicationCategoryId = templateRow.ApplicationCategoryId,
            ApplicationForId = templateRow.ApplicationForId,
            ApplicationTypeId = templateRow.ApplicationTypeId,
            DefaultApplicationStatusId = templateRow.DefaultApplicationStatusId
        };
    }

    private async Task<bool> ApplicantTemplateTablesExistAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CAST(
                CASE
                    WHEN OBJECT_ID(N'dbo.ApplicationTemplates', N'U') IS NOT NULL
                     AND OBJECT_ID(N'dbo.ApplicationTemplateVersions', N'U') IS NOT NULL
                     AND OBJECT_ID(N'dbo.ApplicationTemplateSections', N'U') IS NOT NULL
                     AND OBJECT_ID(N'dbo.ApplicationSectionTypes', N'U') IS NOT NULL
                    THEN 1
                    ELSE 0
                END AS int) AS [Value]
            """;

        var result = await dbContext.Database.SqlQueryRaw<int>(sql).FirstAsync(cancellationToken);
        return result == 1;
    }

    private static async Task<TemplateRow?> ReadTemplateRowAsync(DbConnection connection, string applicationCode, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1)
                t.Id,
                t.Code,
                t.Name,
                t.Description,
                t.ApplicationCategoryId,
                t.ApplicationForId,
                t.ApplicationTypeId,
                t.DefaultApplicationStatusId,
                COALESCE(t.CurrentPublishedVersionId, v.Id) AS TemplateVersionId
            FROM dbo.ApplicationTemplates AS t
            LEFT JOIN dbo.ApplicationTemplateVersions AS v
                ON v.ApplicationTemplateId = t.Id
               AND v.IsDeleted = 0
               AND v.VersionNumber = 1
            WHERE t.IsDeleted = 0
              AND
              (
                  LOWER(t.Code) = @ApplicationCode
                  OR LOWER(REPLACE(t.Code, '-new', '')) = @ApplicationCode
                  OR LOWER(t.Name) LIKE '%' + @ApplicationCode + '%'
              )
            ORDER BY
                CASE
                    WHEN LOWER(REPLACE(t.Code, '-new', '')) = @ApplicationCode THEN 0
                    WHEN LOWER(t.Code) = @ApplicationCode THEN 1
                    ELSE 2
                END,
                t.Name;
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@ApplicationCode";
        parameter.Value = applicationCode;
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TemplateRow(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetGuid(4),
            reader.IsDBNull(5) ? null : reader.GetGuid(5),
            reader.IsDBNull(6) ? null : reader.GetGuid(6),
            reader.IsDBNull(7) ? null : reader.GetGuid(7),
            reader.GetGuid(8));
    }

    private static async Task<List<SectionRow>> ReadSectionRowsAsync(DbConnection connection, Guid templateVersionId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                section.Id,
                section.SectionCode,
                section.Title,
                section.DisplayOrder,
                section.SystemRouteKey,
                section.SystemComponentKey,
                section.StepIcon,
                section.IsVisible,
                section.IsRequired,
                section.ValidationMode,
                section.FormDefinitionVersionId,
                sectionType.Code AS SectionTypeCode,
                formDefinition.Code AS FormCode,
                formDefinition.Name AS FormName,
                formVersion.VersionNumber
            FROM dbo.ApplicationTemplateSections AS section
            INNER JOIN dbo.ApplicationSectionTypes AS sectionType
                ON sectionType.Id = section.ApplicationSectionTypeId
            LEFT JOIN dbo.FormDefinitionVersions AS formVersion
                ON formVersion.Id = section.FormDefinitionVersionId
            LEFT JOIN dbo.FormDefinitions AS formDefinition
                ON formDefinition.Id = formVersion.FormDefinitionId
            WHERE section.ApplicationTemplateVersionId = @TemplateVersionId
              AND section.IsDeleted = 0
            ORDER BY section.DisplayOrder, section.Title;
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@TemplateVersionId";
        parameter.Value = templateVersionId;
        command.Parameters.Add(parameter);

        var sections = new List<SectionRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            sections.Add(new SectionRow(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetBoolean(7),
                reader.GetBoolean(8),
                reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetGuid(10),
                reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetString(12),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetInt32(14)));
        }

        return sections;
    }

    private async Task<ApplicantApplicationStartupResponse> BuildStartupResponseAsync(
        ApplicantApplicationTemplateResponse template,
        CancellationToken cancellationToken)
    {
        if (string.Equals(template.ApplicationCode, "spm", StringComparison.OrdinalIgnoreCase))
        {
            return await BuildSpmStartupResponseAsync(template, cancellationToken);
        }

        return new ApplicantApplicationStartupResponse(
            template.ApplicationCode,
            template.TemplateCode,
            template.TemplateName,
            template.Description,
            [new ApplicantApplicationStartupOptionResponse(template.ApplicationCode, template.TemplateName, true)],
            [],
            [],
            [
                new ApplicantApplicationStartupOptionGroupResponse(
                    "application-type",
                    "Type of Application",
                    [
                        new ApplicantApplicationStartupOptionResponse("new", "New", true),
                        new ApplicantApplicationStartupOptionResponse("extension", "Extension", false),
                        new ApplicantApplicationStartupOptionResponse("additional-quantity", "Additional Quantity", false)
                    ])
            ],
            [],
            [],
            new Dictionary<string, IReadOnlyList<ApplicantApplicationStartupOptionResponse>>(StringComparer.OrdinalIgnoreCase));
    }

    private async Task<ApplicantApplicationStartupResponse> BuildSpmStartupResponseAsync(
        ApplicantApplicationTemplateResponse template,
        CancellationToken cancellationToken)
    {
        var sectorOptions = await LoadSpmSectorOptionsAsync(cancellationToken);
        var mainIndustryOptionsBySector = await LoadSpmMainIndustryOptionsBySectorAsync(cancellationToken);
        var defaultSectorValue = sectorOptions.FirstOrDefault()?.Value ?? string.Empty;
        var mainIndustryOptions = defaultSectorValue.Length > 0 &&
                                  mainIndustryOptionsBySector.TryGetValue(defaultSectorValue, out var sectorIndustries)
            ? sectorIndustries
            : [];

        return new ApplicantApplicationStartupResponse(
            template.ApplicationCode,
            template.TemplateCode,
            template.TemplateName,
            template.Description,
            [new ApplicantApplicationStartupOptionResponse("1", "Confirmation Letter for Exemption (SPM)", true)],
            sectorOptions,
            [new ApplicantApplicationStartupOptionResponse("not-applicable", "Not applicable for SPM startup", true, true)],
            [
                new ApplicantApplicationStartupOptionGroupResponse(
                    "application-type",
                    "Type of Application",
                    await LoadSpmApplicationTypeOptionsAsync(cancellationToken))
            ],
            [new ApplicantApplicationStartupOptionResponse("not-applicable", "Not applicable for SPM startup", true, true)],
            mainIndustryOptions,
            mainIndustryOptionsBySector);
    }

    private async Task<IReadOnlyList<ApplicantApplicationStartupOptionResponse>> LoadSpmSectorOptionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.LegacyId, s.Name
            FROM dbo.ApplicationForSectors AS afs
            INNER JOIN dbo.ApplicationFors AS af
                ON af.Id = afs.ApplicationForId
               AND af.IsDeleted = 0
            INNER JOIN dbo.ApplicationSectors AS s
                ON s.Id = afs.SectorId
               AND s.IsDeleted = 0
            WHERE afs.IsDeleted = 0
              AND af.Name = N'Confirmation Letter for Exemption (SPM)'
              AND s.Name IN (N'Manufacturing', N'Hotel Operator', N'Haulage Operator', N'Aerospace MRO')
            ORDER BY CASE s.Name
                WHEN N'Manufacturing' THEN 1
                WHEN N'Hotel Operator' THEN 2
                WHEN N'Haulage Operator' THEN 3
                WHEN N'Aerospace MRO' THEN 4
                ELSE 99
            END, s.Name;
            """;

        var rows = await dbContext.Database.SqlQueryRaw<StartupLookupRow>(sql).ToListAsync(cancellationToken);
        return rows
            .Select((row, index) => new ApplicantApplicationStartupOptionResponse(
                row.LegacyId.ToString(),
                row.Name,
                index == 0))
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<ApplicantApplicationStartupOptionResponse>>> LoadSpmMainIndustryOptionsBySectorAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.LegacyId AS SectorLegacyId, mi.LegacyId, mi.Name
            FROM dbo.ApplicationSectorIndustries AS asi
            INNER JOIN dbo.ApplicationSectors AS s
                ON s.Id = asi.SectorId
               AND s.IsDeleted = 0
            INNER JOIN dbo.ApplicationMainIndustries AS mi
                ON mi.Id = asi.MainIndustryId
               AND mi.IsDeleted = 0
            WHERE asi.IsDeleted = 0
              AND s.Name IN (N'Manufacturing', N'Hotel Operator', N'Haulage Operator', N'Aerospace MRO')
            ORDER BY CASE s.Name
                WHEN N'Manufacturing' THEN 1
                WHEN N'Hotel Operator' THEN 2
                WHEN N'Haulage Operator' THEN 3
                WHEN N'Aerospace MRO' THEN 4
                ELSE 99
            END, mi.Name;
            """;

        var rows = await dbContext.Database.SqlQueryRaw<StartupSectorIndustryRow>(sql).ToListAsync(cancellationToken);
        return rows
            .GroupBy(row => row.SectorLegacyId.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ApplicantApplicationStartupOptionResponse>)group
                    .Select((row, index) => new ApplicantApplicationStartupOptionResponse(
                        row.LegacyId.ToString(),
                        row.Name,
                        index == 0))
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<ApplicantApplicationStartupOptionResponse>> LoadSpmApplicationTypeOptionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT LegacyId, Name
            FROM dbo.ApplicationTypes
            WHERE IsDeleted = 0
              AND Name IN (N'New', N'Extension', N'Additional Quantity')
            ORDER BY CASE Name
                WHEN N'New' THEN 1
                WHEN N'Extension' THEN 2
                WHEN N'Additional Quantity' THEN 3
                ELSE 99
            END, Name;
            """;

        var rows = await dbContext.Database.SqlQueryRaw<StartupLookupRow>(sql).ToListAsync(cancellationToken);
        return rows
            .Select((row, index) => new ApplicantApplicationStartupOptionResponse(
                row.LegacyId.ToString(),
                row.Name,
                index == 0))
            .ToArray();
    }

    private async Task<Guid?> GetAssignedCompanyAsync(CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return null;
        }

        return await dbContext.CompanyProfileUserAssignments
            .AsNoTracking()
            .Where(assignment => assignment.ApplicationUserId == currentUserService.UserId.Value && assignment.IsActive && !assignment.IsDeleted)
            .OrderBy(assignment => assignment.CreatedAt)
            .Select(assignment => (Guid?)assignment.CompanyProfileId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string BuildApplicationNumber(string templateCode)
    {
        var prefix = string.IsNullOrWhiteSpace(templateCode)
            ? "APP"
            : templateCode.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }

    private static async Task InsertApplicantApplicationAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid applicationId,
        string applicationNo,
        string applicantUserId,
        Guid companyProfileId,
        TemplateDefinition template,
        CreatedSectionRow? firstCreatedSection,
        TemplateSectionDefinition? firstVisibleSection,
        string startupDataJson,
        string versionSnapshotJson,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.ApplicantApplications
            (
                Id,
                ApplicationNo,
                ApplicantUserId,
                CompanyProfileId,
                ApplicationTemplateId,
                ApplicationTemplateVersionId,
                ApplicationCategoryId,
                ApplicationForId,
                ApplicationTypeId,
                ApplicationStatusId,
                CurrentSectionId,
                CurrentSectionCode,
                StartupDataJson,
                VersionSnapshotJson,
                LastSavedAt
            )
            VALUES
            (
                @Id,
                @ApplicationNo,
                @ApplicantUserId,
                @CompanyProfileId,
                @ApplicationTemplateId,
                @ApplicationTemplateVersionId,
                @ApplicationCategoryId,
                @ApplicationForId,
                @ApplicationTypeId,
                @ApplicationStatusId,
                @CurrentSectionId,
                @CurrentSectionCode,
                @StartupDataJson,
                @VersionSnapshotJson,
                SYSUTCDATETIME()
            );
            """;

        AddParameter(command, "@Id", applicationId);
        AddParameter(command, "@ApplicationNo", applicationNo);
        AddParameter(command, "@ApplicantUserId", applicantUserId);
        AddParameter(command, "@CompanyProfileId", companyProfileId);
        AddParameter(command, "@ApplicationTemplateId", template.TemplateId);
        AddParameter(command, "@ApplicationTemplateVersionId", template.TemplateVersionId);
        AddParameter(command, "@ApplicationCategoryId", template.ApplicationCategoryId);
        AddParameter(command, "@ApplicationForId", template.ApplicationForId);
        AddParameter(command, "@ApplicationTypeId", template.ApplicationTypeId);
        AddParameter(command, "@ApplicationStatusId", template.DefaultApplicationStatusId);
        AddParameter(command, "@CurrentSectionId", firstCreatedSection?.SectionId);
        AddParameter(command, "@CurrentSectionCode", firstVisibleSection?.SectionCode);
        AddParameter(command, "@StartupDataJson", startupDataJson);
        AddParameter(command, "@VersionSnapshotJson", versionSnapshotJson);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertApplicantApplicationSectionAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid applicationId,
        CreatedSectionRow section,
        TemplateSectionDefinition? firstVisibleSection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.ApplicantApplicationSections
            (
                Id,
                ApplicantApplicationId,
                ApplicationTemplateSectionId,
                SectionCode,
                SectionTitle,
                FormDefinitionVersionId,
                SectionStatus,
                IsStarted,
                StartedAt
            )
            VALUES
            (
                @Id,
                @ApplicantApplicationId,
                @ApplicationTemplateSectionId,
                @SectionCode,
                @SectionTitle,
                @FormDefinitionVersionId,
                @SectionStatus,
                @IsStarted,
                @StartedAt
            );
            """;

        var isCurrent = firstVisibleSection is not null &&
            string.Equals(firstVisibleSection.SectionCode, section.SectionCode, StringComparison.OrdinalIgnoreCase);

        AddParameter(command, "@Id", section.SectionId);
        AddParameter(command, "@ApplicantApplicationId", applicationId);
        AddParameter(command, "@ApplicationTemplateSectionId", section.TemplateSectionId);
        AddParameter(command, "@SectionCode", section.SectionCode);
        AddParameter(command, "@SectionTitle", section.SectionTitle);
        AddParameter(command, "@FormDefinitionVersionId", section.FormDefinitionVersionId);
        AddParameter(command, "@SectionStatus", isCurrent ? "InProgress" : "NotStarted");
        AddParameter(command, "@IsStarted", isCurrent);
        AddParameter(command, "@StartedAt", isCurrent ? DateTime.UtcNow : null);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertApplicantApplicationAuditLogAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid applicationId,
        string eventType,
        string eventBy,
        string remarks,
        string snapshotJson,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.ApplicantApplicationAuditLogs
            (
                Id,
                ApplicantApplicationId,
                EventType,
                EventBy,
                Remarks,
                SnapshotJson
            )
            VALUES
            (
                @Id,
                @ApplicantApplicationId,
                @EventType,
                @EventBy,
                @Remarks,
                @SnapshotJson
            );
            """;

        AddParameter(command, "@Id", Guid.NewGuid());
        AddParameter(command, "@ApplicantApplicationId", applicationId);
        AddParameter(command, "@EventType", eventType);
        AddParameter(command, "@EventBy", eventBy);
        AddParameter(command, "@Remarks", remarks);
        AddParameter(command, "@SnapshotJson", snapshotJson);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static async Task<Dictionary<Guid, IReadOnlyList<ApplicantApplicationTemplateFieldResponse>>> ReadFieldsByFormVersionAsync(
        DbConnection connection,
        IReadOnlyCollection<Guid> formVersionIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, IReadOnlyList<ApplicantApplicationTemplateFieldResponse>>();
        if (formVersionIds.Count == 0)
        {
            return result;
        }

        var fields = new List<FieldRow>();
        foreach (var formVersionId in formVersionIds)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    field.FormDefinitionVersionId,
                    field.Id,
                    field.FieldCode,
                    field.Label,
                    field.Placeholder,
                    field.HelpText,
                    field.IsRequired,
                    field.DisplayOrder,
                    fieldType.Code AS FieldTypeCode
                FROM dbo.FormFieldDefinitions AS field
                INNER JOIN dbo.FormFieldTypes AS fieldType
                    ON fieldType.Id = field.FormFieldTypeId
                WHERE field.FormDefinitionVersionId = @FormDefinitionVersionId
                  AND field.IsDeleted = 0
                ORDER BY field.DisplayOrder, field.Label;
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@FormDefinitionVersionId";
            parameter.Value = formVersionId;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                fields.Add(new FieldRow(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.GetBoolean(6),
                    reader.GetInt32(7),
                    reader.GetString(8)));
            }
        }

        var optionsByFieldId = await ReadOptionsByFieldIdAsync(connection, fields.Select(field => field.FieldId).Distinct().ToArray(), cancellationToken);

        foreach (var group in fields.GroupBy(field => field.FormDefinitionVersionId))
        {
            result[group.Key] = group
                .Select(field => new ApplicantApplicationTemplateFieldResponse(
                    field.FieldId,
                    field.FieldCode,
                    field.Label,
                    field.FieldTypeCode,
                    field.Placeholder,
                    field.HelpText,
                    field.IsRequired,
                    field.DisplayOrder,
                    optionsByFieldId.GetValueOrDefault(field.FieldId) ?? []))
                .OrderBy(field => field.DisplayOrder)
                .ToArray();
        }

        return result;
    }

    private static async Task<Dictionary<Guid, IReadOnlyList<ApplicantApplicationTemplateFieldOptionResponse>>> ReadOptionsByFieldIdAsync(
        DbConnection connection,
        IReadOnlyCollection<Guid> fieldIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, IReadOnlyList<ApplicantApplicationTemplateFieldOptionResponse>>();
        foreach (var fieldId in fieldIds)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    optionRow.OptionValue,
                    optionRow.OptionLabel,
                    optionRow.DisplayOrder
                FROM dbo.FormFieldOptions AS optionRow
                WHERE optionRow.FormFieldDefinitionId = @FormFieldDefinitionId
                  AND optionRow.IsDeleted = 0
                  AND optionRow.IsActive = 1
                ORDER BY optionRow.DisplayOrder, optionRow.OptionLabel;
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@FormFieldDefinitionId";
            parameter.Value = fieldId;
            command.Parameters.Add(parameter);

            var options = new List<ApplicantApplicationTemplateFieldOptionResponse>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                options.Add(new ApplicantApplicationTemplateFieldOptionResponse(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetInt32(2)));
            }

            result[fieldId] = options;
        }

        return result;
    }

    private static ApplicantApplicationTemplateResponse BuildFallbackTemplate(string applicationCode)
    {
        if (string.Equals(applicationCode, "spm", StringComparison.OrdinalIgnoreCase))
        {
            return new ApplicantApplicationTemplateResponse(
                "spm",
                "SPM-NEW",
                "Confirmation Letter for Exemption (SPM)",
                "Fallback pilot applicant submission template used when database template tables have not been deployed yet.",
                [
                    new ApplicantApplicationTemplateSectionResponse(
                        "SECTION_A",
                        "Section A: Particular of Company",
                        10,
                        "SystemForm",
                        "/irpm/company-profile",
                        "ParticularOfCompany",
                        "building-2",
                        true,
                        true,
                        "OnSave",
                        null),
                    new ApplicantApplicationTemplateSectionResponse(
                        "SECTION_B",
                        "Section B: Project Information",
                        20,
                        "DynamicForm",
                        null,
                        null,
                        "clipboard-text",
                        true,
                        true,
                        "OnSave",
                        new ApplicantApplicationTemplateFormResponse(
                            Guid.Empty,
                            "SPM-PROJECT-DETAILS",
                            "SPM Project Details",
                            1,
                            [
                                new ApplicantApplicationTemplateFieldResponse(Guid.Empty, "project_title", "Project Title", "Text", "Enter project title", "Business-defined title for the exemption request.", true, 10, []),
                                new ApplicantApplicationTemplateFieldResponse(Guid.Empty, "project_description", "Project Description", "TextArea", "Summarise the project", "Brief narrative of the project and exemption rationale.", true, 20, []),
                                new ApplicantApplicationTemplateFieldResponse(Guid.Empty, "expected_investment_amount", "Expected Investment Amount (RM)", "Number", "0.00", "Estimated investment amount for the project.", false, 30, []),
                                new ApplicantApplicationTemplateFieldResponse(Guid.Empty, "expected_implementation_date", "Expected Implementation Date", "Date", null, "Estimated start date for the project implementation.", false, 40, []),
                                new ApplicantApplicationTemplateFieldResponse(Guid.Empty, "project_sector", "Project Sector", "Dropdown", null, "Choose the sector most relevant to the project.", true, 50,
                                [
                                    new ApplicantApplicationTemplateFieldOptionResponse("manufacturing", "Manufacturing", 10),
                                    new ApplicantApplicationTemplateFieldOptionResponse("services", "Services", 20),
                                    new ApplicantApplicationTemplateFieldOptionResponse("logistics", "Logistics", 30),
                                    new ApplicantApplicationTemplateFieldOptionResponse("other", "Other", 40)
                                ]),
                                new ApplicantApplicationTemplateFieldResponse(Guid.Empty, "is_information_true", "I confirm that the information provided is true and complete.", "Checkbox", null, "Required declaration before applicant submission.", true, 60, [])
                            ])),
                    new ApplicantApplicationTemplateSectionResponse(
                        "SECTION_C",
                        "Section C: Attachments",
                        30,
                        "DocumentUpload",
                        "/applications/attachments",
                        "ApplicationAttachments",
                        "paperclip",
                        true,
                        true,
                        "OnSubmit",
                        null),
                    new ApplicantApplicationTemplateSectionResponse(
                        "SECTION_D",
                        "Section D: Review & Submit",
                        40,
                        "ReviewSubmit",
                        "/applications/review",
                        "ApplicationReviewSubmit",
                        "check-circle",
                        true,
                        true,
                        "OnSubmit",
                        null)
                ]);
        }

        var displayName = BuildDisplayName(applicationCode);
        return new ApplicantApplicationTemplateResponse(
            applicationCode,
            applicationCode.ToUpperInvariant(),
            displayName,
            "Fallback configurable shell. Database-backed template metadata is not available for this application yet.",
            [
                new ApplicantApplicationTemplateSectionResponse("SECTION_A", "Section A: Particular of Company", 10, "SystemForm", "/irpm/company-profile", "ParticularOfCompany", "building-2", true, true, "OnSave", null),
                new ApplicantApplicationTemplateSectionResponse("SECTION_B", "Section B: Project Information", 20, "DynamicForm", null, null, "clipboard-text", true, true, "OnSave",
                    new ApplicantApplicationTemplateFormResponse(Guid.Empty, $"{applicationCode.ToUpperInvariant()}-PROJECT-DETAILS", $"{displayName} Project Details", 1,
                    [
                        new ApplicantApplicationTemplateFieldResponse(Guid.Empty, "project_title", "Project Title", "Text", "Enter project title", null, true, 10, []),
                        new ApplicantApplicationTemplateFieldResponse(Guid.Empty, "project_description", "Project Description", "TextArea", "Summarise the project", null, false, 20, [])
                    ])),
                new ApplicantApplicationTemplateSectionResponse("SECTION_C", "Section C: Attachments", 30, "DocumentUpload", "/applications/attachments", "ApplicationAttachments", "paperclip", true, true, "OnSubmit", null),
                new ApplicantApplicationTemplateSectionResponse("SECTION_D", "Section D: Review & Submit", 40, "ReviewSubmit", "/applications/review", "ApplicationReviewSubmit", "check-circle", true, true, "OnSubmit", null)
            ]);
    }

    private static string BuildDisplayName(string applicationCode)
    {
        return string.Join(
            " ",
            applicationCode
                .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private sealed record TemplateRow(
        Guid TemplateId,
        string TemplateCode,
        string TemplateName,
        string? Description,
        Guid? ApplicationCategoryId,
        Guid? ApplicationForId,
        Guid? ApplicationTypeId,
        Guid? DefaultApplicationStatusId,
        Guid TemplateVersionId);

    private sealed record SectionRow(
        Guid TemplateSectionId,
        string SectionCode,
        string Title,
        int DisplayOrder,
        string? SystemRouteKey,
        string? SystemComponentKey,
        string? StepIcon,
        bool IsVisible,
        bool IsRequired,
        string ValidationMode,
        Guid? FormDefinitionVersionId,
        string SectionTypeCode,
        string? FormCode,
        string? FormName,
        int? FormVersionNumber);

    private sealed record TemplateDefinition(
        string ApplicationCode,
        string TemplateCode,
        string TemplateName,
        string? Description,
        IReadOnlyList<TemplateSectionDefinition> Sections)
    {
        public Guid TemplateId { get; init; }
        public Guid TemplateVersionId { get; init; }
        public Guid? ApplicationCategoryId { get; init; }
        public Guid? ApplicationForId { get; init; }
        public Guid? ApplicationTypeId { get; init; }
        public Guid? DefaultApplicationStatusId { get; init; }
    }

    private sealed record TemplateSectionDefinition(
        Guid TemplateSectionId,
        string SectionCode,
        string Title,
        int DisplayOrder,
        string SectionTypeCode,
        string? SystemRouteKey,
        string? SystemComponentKey,
        string? StepIcon,
        bool IsVisible,
        bool IsRequired,
        string ValidationMode,
        ApplicantApplicationTemplateFormResponse? Form)
    {
        public Guid? FormDefinitionVersionId => Form?.FormDefinitionVersionId == Guid.Empty ? null : Form?.FormDefinitionVersionId;
    }

    private sealed record CreatedSectionRow(
        Guid SectionId,
        Guid TemplateSectionId,
        string SectionCode,
        string SectionTitle,
        Guid? FormDefinitionVersionId);

    private sealed record FieldRow(
        Guid FormDefinitionVersionId,
        Guid FieldId,
        string FieldCode,
        string Label,
        string? Placeholder,
        string? HelpText,
        bool IsRequired,
        int DisplayOrder,
        string FieldTypeCode);

    private sealed record ApplicantApplicationRow(
        Guid Id,
        Guid? CompanyProfileId);

    private sealed record CompanySourceAddress(
        long? LegacyAddressId,
        string? AddressLine1,
        string? AddressLine2,
        string? AddressLine3,
        string? Country,
        string? State,
        string? City,
        string? Postcode);

    private sealed record CompanySourceDirector(
        long LegacyDirectorId,
        string? DirectorName,
        long? LegacyNationalityId,
        string? NationalityName,
        decimal? SharesHeldPercent,
        int? SourceCreatedByLegacyUserId,
        DateTime? SourceCreatedAt,
        int? SourceUpdatedByLegacyUserId,
        DateTime? SourceUpdatedAt);

    private sealed record CompanySourceContact(
        long? LegacyContactPersonId,
        string? Title,
        string? FullName,
        string? Designation,
        string? Email,
        string? PhoneNo);

    private sealed record CompanySourceSnapshot(
        Guid CompanyProfileId,
        long? LegacyParticularOfCompanyId,
        string? CompanyName,
        string? RegistrationNumber,
        DateTime? DateOfIncorporation,
        DateTime? RegistrationDate,
        string? TelephoneNumber,
        string? FaxNumber,
        string? Website,
        string? Email,
        string? IncomeTaxNo,
        string? EpfNo,
        string? SocsoNo,
        int? LegacyUserId,
        long? LegacyCompanySignatureId,
        int? LegacyCompanyTypeId,
        bool? IsCompanyCertified,
        int? LegacyCompanyApprovalStatusId,
        bool? IsPaid,
        bool? IsCompanyLocal,
        int? SourceCreatedByLegacyUserId,
        DateTime? SourceCreatedAt,
        int? SourceUpdatedByLegacyUserId,
        DateTime? SourceUpdatedAt,
        long? LegacyAddressId,
        string? CompanyBackground,
        int? TotalEmployment,
        CompanySourceAddress? Address,
        IReadOnlyList<CompanySourceDirector> Directors,
        IReadOnlyList<CompanySourceContact> ContactPersons,
        DateTime LastSyncedAt);

    private sealed record StartupLookupRow(int LegacyId, string Name);
    private sealed record StartupSectorIndustryRow(int SectorLegacyId, int LegacyId, string Name);
}
