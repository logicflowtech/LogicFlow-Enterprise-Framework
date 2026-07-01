using System.Data;
using System.Text.RegularExpressions;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class CompanyRelatedDataSyncService(
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    ITenantProvider tenantProvider,
    IOptions<CompanyRelatedDataSyncOptions> options,
    ILogger<CompanyRelatedDataSyncService> logger) : ICompanyRelatedDataSyncService
{
    private const string SyncKey = "company-related-data";
    private const string SyncName = "Company Related Data Sync";
    private const string SyncDescription = "Sync authorized persons, board directors, and attachment documents into local company-linked cache tables.";
    private const string SyncStateSourceSystem = "outsystems";
    private static readonly Regex ObjectNamePattern = new("^[A-Za-z0-9_\\.\\[\\]]+$", RegexOptions.Compiled);
    private readonly CompanyRelatedDataSyncOptions _options = options.Value;

    public async Task<SyncJobSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(cancellationToken);
        return ToSummary(status);
    }

    public async Task<CompanyRelatedDataSyncStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureTargetTablesAsync(localConnection, cancellationToken);
        await EnsureSyncStateRowAsync(localConnection, cancellationToken);

        var state = await ReadStateAsync(localConnection, cancellationToken);
        var localRowCount = await GetLocalRowCountAsync(localConnection, cancellationToken);

        return new CompanyRelatedDataSyncStatusResponse(
            _options.SourceConnectionStringName,
            HasConfiguredSourceConnection(),
            ResolveSourceObjectName(_options.AuthorizedPersonSourceObjectName),
            ResolveSourceObjectName(_options.BoardDirectorSourceObjectName),
            ResolveSourceObjectName(_options.AttachmentDocumentSourceObjectName),
            _options.BatchSize,
            localRowCount,
            ToUtc(state.LastStartedAt),
            ToUtc(state.LastCompletedAt),
            state.LastRunSucceeded,
            state.LastProcessedRows,
            state.LastRunMessage,
            state.LastSourceCompanyId);
    }

    public async Task<CompanyRelatedDataSyncStatusResponse> RunSyncAsync(long? sourceCompanyId = null, CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureTargetTablesAsync(localConnection, cancellationToken);
        await EnsureSyncStateRowAsync(localConnection, cancellationToken);
        await AcquireAppLockAsync(localConnection, cancellationToken);

        try
        {
            await MarkStartedAsync(localConnection, sourceCompanyId, cancellationToken);

            await using var sourceConnection = new SqlConnection(GetSourceConnectionString());
            await sourceConnection.OpenAsync(cancellationToken);

            foreach (var sourceObjectName in GetAllSourceObjectNames())
            {
                await EnsureSourceObjectExistsAsync(sourceConnection, sourceObjectName, cancellationToken);
            }

            var tenantId = await ResolveTenantIdAsync(cancellationToken);
            var companyMap = await LoadCompanyMapAsync(cancellationToken);
            var authorizedPersons = await LoadAuthorizedPersonsAsync(sourceConnection, sourceCompanyId, companyMap, cancellationToken);
            var boardDirectors = await LoadBoardDirectorsAsync(sourceConnection, sourceCompanyId, companyMap, cancellationToken);
            var attachmentDocuments = await LoadAttachmentDocumentsAsync(sourceConnection, sourceCompanyId, companyMap, cancellationToken);

            await using var transaction = (SqlTransaction)await localConnection.BeginTransactionAsync(cancellationToken);
            await MergeAuthorizedPersonsAsync(localConnection, transaction, tenantId, authorizedPersons, sourceCompanyId, cancellationToken);
            await MergeBoardDirectorsAsync(localConnection, transaction, tenantId, boardDirectors, sourceCompanyId, cancellationToken);
            await MergeAttachmentDocumentsAsync(localConnection, transaction, tenantId, attachmentDocuments, sourceCompanyId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var processedRows = authorizedPersons.Rows.Count + boardDirectors.Rows.Count + attachmentDocuments.Rows.Count;
            var message = BuildCompletedMessage(authorizedPersons.Rows.Count, boardDirectors.Rows.Count, attachmentDocuments.Rows.Count, sourceCompanyId);
            await MarkCompletedAsync(localConnection, sourceCompanyId, processedRows, message, cancellationToken);

            return await GetStatusAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Company related data sync failed for source company id {SourceCompanyId}.", sourceCompanyId);
            await TryMarkFailedAsync(localConnection, sourceCompanyId, exception.Message, cancellationToken);
            throw;
        }
        finally
        {
            await ReleaseAppLockAsync(localConnection, cancellationToken);
        }
    }

    private async Task<Dictionary<long, Guid>> LoadCompanyMapAsync(CancellationToken cancellationToken)
    {
        return await dbContext.CompanyProfiles
            .AsNoTracking()
            .Where(company => company.MigratedId.HasValue)
            .ToDictionaryAsync(company => company.MigratedId!.Value, company => company.Id, cancellationToken);
    }

    private async Task<DataTable> LoadAuthorizedPersonsAsync(
        SqlConnection sourceConnection,
        long? sourceCompanyId,
        IReadOnlyDictionary<long, Guid> companyMap,
        CancellationToken cancellationToken)
    {
        var table = CreateAuthorizedPersonTable();
        var query = $"""
            SELECT
                CAST(src.ID AS BIGINT) AS MigratedId,
                CAST(src.COMPANYID AS BIGINT) AS LegacyCompanyId,
                NULLIF(LTRIM(RTRIM(src.FULLNAME)), N'') AS FullName,
                NULLIF(LTRIM(RTRIM(src.DESIGNATION)), N'') AS Designation,
                CAST(src.IDENTITYTYPE AS INT) AS LegacyIdentityTypeId,
                NULLIF(LTRIM(RTRIM(src.IDENTITYNO)), N'') AS IdentityNumber,
                NULLIF(LTRIM(RTRIM(src.EMAIL)), N'') AS Email,
                NULLIF(LTRIM(RTRIM(src.TELEPHONENO)), N'') AS TelephoneNo,
                CAST(src.USERID AS INT) AS LegacyUserId,
                CAST(src.ISDIGICERTPAID AS BIT) AS IsDigiCertPaid,
                CAST(src.ISCERTIFIED AS BIT) AS IsCertified,
                CAST(src.ISPINVERIFIED AS BIT) AS IsPinVerified,
                CAST(COALESCE(src.ISDELETED, 0) AS BIT) AS IsDeletedInSource,
                CAST(src.TITLEID AS INT) AS LegacyTitleId,
                CAST(src.CITIZENSHIP AS BIGINT) AS LegacyCitizenshipId,
                CAST(src.CANEDIT AS BIT) AS CanEdit,
                CAST(src.CREATEDBY AS INT) AS SourceCreatedByLegacyUserId,
                CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END AS SourceCreatedAt,
                CAST(src.MODIFIEDBY AS INT) AS SourceUpdatedByLegacyUserId,
                CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END AS SourceUpdatedAt
            FROM {ResolveSourceObjectName(_options.AuthorizedPersonSourceObjectName)} src
            WHERE src.COMPANYID IS NOT NULL
              AND (@SourceCompanyId IS NULL OR src.COMPANYID = @SourceCompanyId)
            ORDER BY src.ID;
            """;

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@SourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var legacyCompanyId = reader.GetInt64(1);
            if (!companyMap.TryGetValue(legacyCompanyId, out var companyProfileId))
            {
                continue;
            }

            table.Rows.Add(
                companyProfileId,
                reader.GetInt64(0),
                GetNullableString(reader, 2),
                GetNullableString(reader, 3),
                GetNullableInt32(reader, 4),
                GetNullableString(reader, 5),
                GetNullableString(reader, 6),
                GetNullableString(reader, 7),
                GetNullableInt32(reader, 8),
                GetNullableBoolean(reader, 9),
                GetNullableBoolean(reader, 10),
                GetNullableBoolean(reader, 11),
                !reader.IsDBNull(12) && reader.GetBoolean(12),
                GetNullableInt32(reader, 13),
                GetNullableInt64(reader, 14),
                GetNullableBoolean(reader, 15),
                GetNullableInt32(reader, 16),
                GetNullableDateTime(reader, 17),
                GetNullableInt32(reader, 18),
                GetNullableDateTime(reader, 19));
        }

        return table;
    }

    private async Task<DataTable> LoadBoardDirectorsAsync(
        SqlConnection sourceConnection,
        long? sourceCompanyId,
        IReadOnlyDictionary<long, Guid> companyMap,
        CancellationToken cancellationToken)
    {
        var table = CreateBoardDirectorTable();
        var query = $"""
            SELECT
                CAST(src.ID AS BIGINT) AS MigratedId,
                CAST(src.COMPANYID AS BIGINT) AS LegacyCompanyId,
                NULLIF(LTRIM(RTRIM(src.[NAME])), N'') AS [Name],
                CAST(src.NATIONALITY AS BIGINT) AS LegacyNationalityId,
                CAST(src.SHAREPERCENTAGE AS DECIMAL(18,2)) AS SharePercentage,
                CAST(src.CREATEDBY AS INT) AS SourceCreatedByLegacyUserId,
                CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END AS SourceCreatedAt,
                CAST(src.MODIFIEDBY AS INT) AS SourceUpdatedByLegacyUserId,
                CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END AS SourceUpdatedAt
            FROM {ResolveSourceObjectName(_options.BoardDirectorSourceObjectName)} src
            WHERE src.COMPANYID IS NOT NULL
              AND (@SourceCompanyId IS NULL OR src.COMPANYID = @SourceCompanyId)
            ORDER BY src.ID;
            """;

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@SourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var legacyCompanyId = reader.GetInt64(1);
            if (!companyMap.TryGetValue(legacyCompanyId, out var companyProfileId))
            {
                continue;
            }

            table.Rows.Add(
                companyProfileId,
                reader.GetInt64(0),
                GetNullableString(reader, 2),
                GetNullableInt64(reader, 3),
                GetNullableDecimal(reader, 4),
                GetNullableInt32(reader, 5),
                GetNullableDateTime(reader, 6),
                GetNullableInt32(reader, 7),
                GetNullableDateTime(reader, 8));
        }

        return table;
    }

    private async Task<DataTable> LoadAttachmentDocumentsAsync(
        SqlConnection sourceConnection,
        long? sourceCompanyId,
        IReadOnlyDictionary<long, Guid> companyMap,
        CancellationToken cancellationToken)
    {
        var table = CreateAttachmentDocumentTable();
        var query = $"""
            SELECT
                CAST(src.ID AS BIGINT) AS MigratedId,
                CAST(src.COMPANYID AS BIGINT) AS LegacyCompanyId,
                NULLIF(LTRIM(RTRIM(src.FILENAME)), N'') AS FileName,
                NULLIF(LTRIM(RTRIM(src.FILETYPE)), N'') AS FileType,
                CAST(src.FILEATTACHMENT AS VARBINARY(MAX)) AS FileContent,
                CAST(src.CREATEDBY AS INT) AS SourceCreatedByLegacyUserId,
                CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END AS SourceCreatedAt,
                CAST(src.MODIFIEDBY AS INT) AS SourceUpdatedByLegacyUserId,
                CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END AS SourceUpdatedAt
            FROM {ResolveSourceObjectName(_options.AttachmentDocumentSourceObjectName)} src
            WHERE src.COMPANYID IS NOT NULL
              AND (@SourceCompanyId IS NULL OR src.COMPANYID = @SourceCompanyId)
            ORDER BY src.ID;
            """;

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@SourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var legacyCompanyId = reader.GetInt64(1);
            if (!companyMap.TryGetValue(legacyCompanyId, out var companyProfileId))
            {
                continue;
            }

            table.Rows.Add(
                companyProfileId,
                reader.GetInt64(0),
                GetNullableString(reader, 2),
                GetNullableString(reader, 3),
                reader.IsDBNull(4) ? DBNull.Value : reader.GetValue(4),
                GetNullableInt32(reader, 5),
                GetNullableDateTime(reader, 6),
                GetNullableInt32(reader, 7),
                GetNullableDateTime(reader, 8));
        }

        return table;
    }

    private async Task MergeAuthorizedPersonsAsync(
        SqlConnection localConnection,
        SqlTransaction transaction,
        Guid tenantId,
        DataTable rows,
        long? sourceCompanyId,
        CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            CREATE TABLE #CompanyAuthorizedPersonBatch
            (
                CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
                MigratedId BIGINT NOT NULL,
                FullName NVARCHAR(100) NULL,
                Designation NVARCHAR(100) NULL,
                LegacyIdentityTypeId INT NULL,
                IdentityNumber NVARCHAR(50) NULL,
                Email NVARCHAR(250) NULL,
                TelephoneNo NVARCHAR(20) NULL,
                LegacyUserId INT NULL,
                IsDigiCertPaid BIT NULL,
                IsCertified BIT NULL,
                IsPinVerified BIT NULL,
                IsDeletedInSource BIT NOT NULL,
                LegacyTitleId INT NULL,
                LegacyCitizenshipId BIGINT NULL,
                CanEdit BIT NULL,
                SourceCreatedByLegacyUserId INT NULL,
                SourceCreatedAt DATETIME2(3) NULL,
                SourceUpdatedByLegacyUserId INT NULL,
                SourceUpdatedAt DATETIME2(3) NULL
            );
            """;

        const string mergeSql = """
            MERGE dbo.CompanyAuthorizedPersons AS target
            USING #CompanyAuthorizedPersonBatch AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    target.CompanyProfileId = source.CompanyProfileId,
                    target.FullName = source.FullName,
                    target.Designation = source.Designation,
                    target.LegacyIdentityTypeId = source.LegacyIdentityTypeId,
                    target.IdentityNumber = source.IdentityNumber,
                    target.Email = source.Email,
                    target.TelephoneNo = source.TelephoneNo,
                    target.LegacyUserId = source.LegacyUserId,
                    target.IsDigiCertPaid = source.IsDigiCertPaid,
                    target.IsCertified = source.IsCertified,
                    target.IsPinVerified = source.IsPinVerified,
                    target.IsDeletedInSource = source.IsDeletedInSource,
                    target.LegacyTitleId = source.LegacyTitleId,
                    target.LegacyCitizenshipId = source.LegacyCitizenshipId,
                    target.CanEdit = source.CanEdit,
                    target.SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
                    target.SourceCreatedAt = source.SourceCreatedAt,
                    target.SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
                    target.SourceUpdatedAt = source.SourceUpdatedAt,
                    target.LastSyncedAt = SYSUTCDATETIME(),
                    target.TenantId = COALESCE(target.TenantId, @TenantId),
                    target.IsDeleted = 0,
                    target.DeletedAt = NULL,
                    target.UpdatedAt = SYSUTCDATETIME(),
                    target.UpdatedBy = @Actor
            WHEN NOT MATCHED BY TARGET THEN
                INSERT
                (
                    Id,
                    TenantId,
                    CompanyProfileId,
                    MigratedId,
                    FullName,
                    Designation,
                    LegacyIdentityTypeId,
                    IdentityNumber,
                    Email,
                    TelephoneNo,
                    LegacyUserId,
                    IsDigiCertPaid,
                    IsCertified,
                    IsPinVerified,
                    IsDeletedInSource,
                    LegacyTitleId,
                    LegacyCitizenshipId,
                    CanEdit,
                    SourceCreatedByLegacyUserId,
                    SourceCreatedAt,
                    SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt,
                    LastSyncedAt,
                    CreatedAt,
                    CreatedBy,
                    UpdatedAt,
                    UpdatedBy,
                    IsDeleted,
                    DeletedAt
                )
                VALUES
                (
                    NEWSEQUENTIALID(),
                    @TenantId,
                    source.CompanyProfileId,
                    source.MigratedId,
                    source.FullName,
                    source.Designation,
                    source.LegacyIdentityTypeId,
                    source.IdentityNumber,
                    source.Email,
                    source.TelephoneNo,
                    source.LegacyUserId,
                    source.IsDigiCertPaid,
                    source.IsCertified,
                    source.IsPinVerified,
                    source.IsDeletedInSource,
                    source.LegacyTitleId,
                    source.LegacyCitizenshipId,
                    source.CanEdit,
                    source.SourceCreatedByLegacyUserId,
                    source.SourceCreatedAt,
                    source.SourceUpdatedByLegacyUserId,
                    source.SourceUpdatedAt,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME(),
                    @Actor,
                    NULL,
                    NULL,
                    0,
                    NULL
                )
            WHEN NOT MATCHED BY SOURCE AND (@IsFullSync = 1 OR target.CompanyProfileId IN (SELECT DISTINCT CompanyProfileId FROM #CompanyAuthorizedPersonBatch)) THEN
                UPDATE SET
                    target.IsDeleted = 1,
                    target.DeletedAt = SYSUTCDATETIME(),
                    target.UpdatedAt = SYSUTCDATETIME(),
                    target.UpdatedBy = @Actor;
            """;

        await CreateTempTableAsync(localConnection, transaction, createTempTableSql, cancellationToken);
        await BulkCopyAsync(localConnection, transaction, "#CompanyAuthorizedPersonBatch", rows, cancellationToken);
        await ExecuteMergeAsync(localConnection, transaction, mergeSql, tenantId, sourceCompanyId, cancellationToken);
    }

    private async Task MergeBoardDirectorsAsync(
        SqlConnection localConnection,
        SqlTransaction transaction,
        Guid tenantId,
        DataTable rows,
        long? sourceCompanyId,
        CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            CREATE TABLE #CompanyBoardDirectorBatch
            (
                CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
                MigratedId BIGINT NOT NULL,
                [Name] NVARCHAR(50) NULL,
                LegacyNationalityId BIGINT NULL,
                SharePercentage DECIMAL(18,2) NULL,
                SourceCreatedByLegacyUserId INT NULL,
                SourceCreatedAt DATETIME2(3) NULL,
                SourceUpdatedByLegacyUserId INT NULL,
                SourceUpdatedAt DATETIME2(3) NULL
            );
            """;

        const string mergeSql = """
            MERGE dbo.CompanyBoardDirectors AS target
            USING #CompanyBoardDirectorBatch AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    target.CompanyProfileId = source.CompanyProfileId,
                    target.Name = source.Name,
                    target.LegacyNationalityId = source.LegacyNationalityId,
                    target.SharePercentage = source.SharePercentage,
                    target.SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
                    target.SourceCreatedAt = source.SourceCreatedAt,
                    target.SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
                    target.SourceUpdatedAt = source.SourceUpdatedAt,
                    target.LastSyncedAt = SYSUTCDATETIME(),
                    target.TenantId = COALESCE(target.TenantId, @TenantId),
                    target.IsDeleted = 0,
                    target.DeletedAt = NULL,
                    target.UpdatedAt = SYSUTCDATETIME(),
                    target.UpdatedBy = @Actor
            WHEN NOT MATCHED BY TARGET THEN
                INSERT
                (
                    Id,
                    TenantId,
                    CompanyProfileId,
                    MigratedId,
                    Name,
                    LegacyNationalityId,
                    SharePercentage,
                    SourceCreatedByLegacyUserId,
                    SourceCreatedAt,
                    SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt,
                    LastSyncedAt,
                    CreatedAt,
                    CreatedBy,
                    UpdatedAt,
                    UpdatedBy,
                    IsDeleted,
                    DeletedAt
                )
                VALUES
                (
                    NEWSEQUENTIALID(),
                    @TenantId,
                    source.CompanyProfileId,
                    source.MigratedId,
                    source.Name,
                    source.LegacyNationalityId,
                    source.SharePercentage,
                    source.SourceCreatedByLegacyUserId,
                    source.SourceCreatedAt,
                    source.SourceUpdatedByLegacyUserId,
                    source.SourceUpdatedAt,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME(),
                    @Actor,
                    NULL,
                    NULL,
                    0,
                    NULL
                )
            WHEN NOT MATCHED BY SOURCE AND (@IsFullSync = 1 OR target.CompanyProfileId IN (SELECT DISTINCT CompanyProfileId FROM #CompanyBoardDirectorBatch)) THEN
                UPDATE SET
                    target.IsDeleted = 1,
                    target.DeletedAt = SYSUTCDATETIME(),
                    target.UpdatedAt = SYSUTCDATETIME(),
                    target.UpdatedBy = @Actor;
            """;

        await CreateTempTableAsync(localConnection, transaction, createTempTableSql, cancellationToken);
        await BulkCopyAsync(localConnection, transaction, "#CompanyBoardDirectorBatch", rows, cancellationToken);
        await ExecuteMergeAsync(localConnection, transaction, mergeSql, tenantId, sourceCompanyId, cancellationToken);
    }

    private async Task MergeAttachmentDocumentsAsync(
        SqlConnection localConnection,
        SqlTransaction transaction,
        Guid tenantId,
        DataTable rows,
        long? sourceCompanyId,
        CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            CREATE TABLE #CompanyAttachmentDocumentBatch
            (
                CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
                MigratedId BIGINT NOT NULL,
                FileName NVARCHAR(300) NULL,
                FileType NVARCHAR(100) NULL,
                FileContent VARBINARY(MAX) NULL,
                SourceCreatedByLegacyUserId INT NULL,
                SourceCreatedAt DATETIME2(3) NULL,
                SourceUpdatedByLegacyUserId INT NULL,
                SourceUpdatedAt DATETIME2(3) NULL
            );
            """;

        const string mergeSql = """
            MERGE dbo.CompanyAttachmentDocuments AS target
            USING #CompanyAttachmentDocumentBatch AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    target.CompanyProfileId = source.CompanyProfileId,
                    target.FileName = source.FileName,
                    target.FileType = source.FileType,
                    target.FileContent = source.FileContent,
                    target.SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
                    target.SourceCreatedAt = source.SourceCreatedAt,
                    target.SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
                    target.SourceUpdatedAt = source.SourceUpdatedAt,
                    target.LastSyncedAt = SYSUTCDATETIME(),
                    target.TenantId = COALESCE(target.TenantId, @TenantId),
                    target.IsDeleted = 0,
                    target.DeletedAt = NULL,
                    target.UpdatedAt = SYSUTCDATETIME(),
                    target.UpdatedBy = @Actor
            WHEN NOT MATCHED BY TARGET THEN
                INSERT
                (
                    Id,
                    TenantId,
                    CompanyProfileId,
                    MigratedId,
                    FileName,
                    FileType,
                    FileContent,
                    SourceCreatedByLegacyUserId,
                    SourceCreatedAt,
                    SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt,
                    LastSyncedAt,
                    CreatedAt,
                    CreatedBy,
                    UpdatedAt,
                    UpdatedBy,
                    IsDeleted,
                    DeletedAt
                )
                VALUES
                (
                    NEWSEQUENTIALID(),
                    @TenantId,
                    source.CompanyProfileId,
                    source.MigratedId,
                    source.FileName,
                    source.FileType,
                    source.FileContent,
                    source.SourceCreatedByLegacyUserId,
                    source.SourceCreatedAt,
                    source.SourceUpdatedByLegacyUserId,
                    source.SourceUpdatedAt,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME(),
                    @Actor,
                    NULL,
                    NULL,
                    0,
                    NULL
                )
            WHEN NOT MATCHED BY SOURCE AND (@IsFullSync = 1 OR target.CompanyProfileId IN (SELECT DISTINCT CompanyProfileId FROM #CompanyAttachmentDocumentBatch)) THEN
                UPDATE SET
                    target.IsDeleted = 1,
                    target.DeletedAt = SYSUTCDATETIME(),
                    target.UpdatedAt = SYSUTCDATETIME(),
                    target.UpdatedBy = @Actor;
            """;

        await CreateTempTableAsync(localConnection, transaction, createTempTableSql, cancellationToken);
        await BulkCopyAsync(localConnection, transaction, "#CompanyAttachmentDocumentBatch", rows, cancellationToken);
        await ExecuteMergeAsync(localConnection, transaction, mergeSql, tenantId, sourceCompanyId, cancellationToken);
    }

    private async Task CreateTempTableAsync(SqlConnection localConnection, SqlTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = localConnection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task BulkCopyAsync(SqlConnection localConnection, SqlTransaction transaction, string tableName, DataTable rows, CancellationToken cancellationToken)
    {
        using var bulkCopy = new SqlBulkCopy(localConnection, SqlBulkCopyOptions.CheckConstraints, transaction)
        {
            DestinationTableName = tableName,
            BatchSize = _options.BatchSize,
            BulkCopyTimeout = _options.CommandTimeoutSeconds
        };

        foreach (DataColumn column in rows.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(rows, cancellationToken);
    }

    private async Task ExecuteMergeAsync(
        SqlConnection localConnection,
        SqlTransaction transaction,
        string sql,
        Guid tenantId,
        long? sourceCompanyId,
        CancellationToken cancellationToken)
    {
        await using var command = localConnection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@Actor", SyncKey);
        command.Parameters.AddWithValue("@IsFullSync", sourceCompanyId.HasValue ? 0 : 1);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureTargetTablesAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.CompanyAuthorizedPersons', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyAuthorizedPersons
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyAuthorizedPersons PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    FullName NVARCHAR(100) NULL,
                    Designation NVARCHAR(100) NULL,
                    LegacyIdentityTypeId INT NULL,
                    IdentityNumber NVARCHAR(50) NULL,
                    Email NVARCHAR(250) NULL,
                    TelephoneNo NVARCHAR(20) NULL,
                    LegacyUserId INT NULL,
                    IsDigiCertPaid BIT NULL,
                    IsCertified BIT NULL,
                    IsPinVerified BIT NULL,
                    IsDeletedInSource BIT NOT NULL CONSTRAINT DF_CompanyAuthorizedPersons_IsDeletedInSource DEFAULT (0),
                    LegacyTitleId INT NULL,
                    LegacyCitizenshipId BIGINT NULL,
                    CanEdit BIT NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyAuthorizedPersons_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyAuthorizedPersons_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyAuthorizedPersons_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyAuthorizedPersons_MigratedId ON dbo.CompanyAuthorizedPersons (MigratedId);
                CREATE INDEX IX_CompanyAuthorizedPersons_CompanyProfileId ON dbo.CompanyAuthorizedPersons (CompanyProfileId);
            END;

            IF OBJECT_ID(N'dbo.CompanyBoardDirectors', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyBoardDirectors
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyBoardDirectors PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    [Name] NVARCHAR(50) NULL,
                    LegacyNationalityId BIGINT NULL,
                    SharePercentage DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyBoardDirectors_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyBoardDirectors_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyBoardDirectors_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyBoardDirectors_MigratedId ON dbo.CompanyBoardDirectors (MigratedId);
                CREATE INDEX IX_CompanyBoardDirectors_CompanyProfileId ON dbo.CompanyBoardDirectors (CompanyProfileId);
            END;

            IF OBJECT_ID(N'dbo.CompanyAttachmentDocuments', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyAttachmentDocuments
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyAttachmentDocuments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    FileName NVARCHAR(300) NULL,
                    FileType NVARCHAR(100) NULL,
                    FileContent VARBINARY(MAX) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyAttachmentDocuments_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyAttachmentDocuments_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyAttachmentDocuments_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyAttachmentDocuments_MigratedId ON dbo.CompanyAttachmentDocuments (MigratedId);
                CREATE INDEX IX_CompanyAttachmentDocuments_CompanyProfileId ON dbo.CompanyAttachmentDocuments (CompanyProfileId);
            END;

            IF OBJECT_ID(N'dbo.CompanyRelatedDataSyncStates', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyRelatedDataSyncStates
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyRelatedDataSyncStates PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    SourceSystem NVARCHAR(100) NOT NULL,
                    SyncName NVARCHAR(100) NOT NULL,
                    LastSourceCompanyId BIGINT NULL,
                    LastStartedAt DATETIME2(3) NULL,
                    LastCompletedAt DATETIME2(3) NULL,
                    LastRunSucceeded BIT NULL,
                    LastProcessedRows INT NOT NULL CONSTRAINT DF_CompanyRelatedDataSyncStates_LastProcessedRows DEFAULT (0),
                    LastRunMessage NVARCHAR(4000) NULL
                );
                CREATE UNIQUE INDEX IX_CompanyRelatedDataSyncStates_SourceSystem_SyncName
                    ON dbo.CompanyRelatedDataSyncStates (SourceSystem, SyncName);
            END;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureSyncStateRowAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.CompanyRelatedDataSyncStates
                WHERE SourceSystem = @SourceSystem
                  AND SyncName = @SyncName
            )
            BEGIN
                INSERT INTO dbo.CompanyRelatedDataSyncStates (SourceSystem, SyncName, LastRunSucceeded, LastProcessedRows, LastRunMessage)
                VALUES (@SourceSystem, @SyncName, NULL, 0, N'Not started');
            END;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceSystem", SyncStateSourceSystem);
        command.Parameters.AddWithValue("@SyncName", SyncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<CompanyRelatedDataSyncStateRow> ReadStateAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT LastSourceCompanyId, LastStartedAt, LastCompletedAt, LastRunSucceeded, LastProcessedRows, LastRunMessage
            FROM dbo.CompanyRelatedDataSyncStates
            WHERE SourceSystem = @SourceSystem
              AND SyncName = @SyncName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceSystem", SyncStateSourceSystem);
        command.Parameters.AddWithValue("@SyncName", SyncKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new CompanyRelatedDataSyncStateRow(null, null, null, null, 0, "Not started");
        }

        return new CompanyRelatedDataSyncStateRow(
            GetNullableInt64(reader, 0),
            GetNullableDateTime(reader, 1),
            GetNullableDateTime(reader, 2),
            reader.IsDBNull(3) ? null : reader.GetBoolean(3),
            reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
            GetNullableString(reader, 5));
    }

    private async Task<long> GetLocalRowCountAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                (SELECT COUNT_BIG(*) FROM dbo.CompanyAuthorizedPersons WHERE IsDeleted = 0) +
                (SELECT COUNT_BIG(*) FROM dbo.CompanyBoardDirectors WHERE IsDeleted = 0) +
                (SELECT COUNT_BIG(*) FROM dbo.CompanyAttachmentDocuments WHERE IsDeleted = 0);
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task MarkStartedAsync(SqlConnection localConnection, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.CompanyRelatedDataSyncStates
            SET LastSourceCompanyId = @LastSourceCompanyId,
                LastStartedAt = @LastStartedAt,
                LastRunSucceeded = NULL,
                LastRunMessage = @LastRunMessage
            WHERE SourceSystem = @SourceSystem
              AND SyncName = @SyncName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@LastSourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });
        command.Parameters.AddWithValue("@LastStartedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@LastRunMessage", sourceCompanyId.HasValue ? $"Running related data sync for source company {sourceCompanyId.Value}." : "Running full company related data sync.");
        command.Parameters.AddWithValue("@SourceSystem", SyncStateSourceSystem);
        command.Parameters.AddWithValue("@SyncName", SyncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkCompletedAsync(SqlConnection localConnection, long? sourceCompanyId, int processedRows, string message, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.CompanyRelatedDataSyncStates
            SET LastSourceCompanyId = @LastSourceCompanyId,
                LastCompletedAt = @LastCompletedAt,
                LastRunSucceeded = 1,
                LastProcessedRows = @LastProcessedRows,
                LastRunMessage = @LastRunMessage
            WHERE SourceSystem = @SourceSystem
              AND SyncName = @SyncName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@LastSourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });
        command.Parameters.AddWithValue("@LastCompletedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@LastProcessedRows", processedRows);
        command.Parameters.AddWithValue("@LastRunMessage", message);
        command.Parameters.AddWithValue("@SourceSystem", SyncStateSourceSystem);
        command.Parameters.AddWithValue("@SyncName", SyncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task TryMarkFailedAsync(SqlConnection localConnection, long? sourceCompanyId, string message, CancellationToken cancellationToken)
    {
        try
        {
            const string sql = """
                UPDATE dbo.CompanyRelatedDataSyncStates
                SET LastSourceCompanyId = @LastSourceCompanyId,
                    LastCompletedAt = @LastCompletedAt,
                    LastRunSucceeded = 0,
                    LastProcessedRows = 0,
                    LastRunMessage = @LastRunMessage
                WHERE SourceSystem = @SourceSystem
                  AND SyncName = @SyncName;
                """;

            await using var command = localConnection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.Add(new SqlParameter("@LastSourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });
            command.Parameters.AddWithValue("@LastCompletedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@LastRunMessage", message.Length > 4000 ? message[..4000] : message);
            command.Parameters.AddWithValue("@SourceSystem", SyncStateSourceSystem);
            command.Parameters.AddWithValue("@SyncName", SyncKey);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to mark company related data sync as failed.");
        }
    }

    private async Task EnsureSourceObjectExistsAsync(SqlConnection sourceConnection, string sourceObjectName, CancellationToken cancellationToken)
    {
        var sql = $"SELECT OBJECT_ID(N'{sourceObjectName.Replace("'", "''")}');";

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;

        var objectId = await command.ExecuteScalarAsync(cancellationToken);
        if (objectId is not DBNull && objectId is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Company related data sync source object {sourceObjectName} was not found in source database '{sourceConnection.Database}'.");
    }

    private async Task AcquireAppLockAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await using var command = localConnection.CreateCommand();
        command.CommandText = """
            DECLARE @result INT;
            EXEC @result = sp_getapplock
                @Resource = @Resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Session',
                @LockTimeout = 10000;
            SELECT @result;
            """;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@Resource", $"CompanyRelatedDataSync:{SyncStateSourceSystem}:{SyncKey}");

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
        {
            throw new InvalidOperationException("Unable to acquire company related data sync lock.");
        }
    }

    private async Task ReleaseAppLockAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = localConnection.CreateCommand();
            command.CommandText = "EXEC sp_releaseapplock @Resource = @Resource, @LockOwner = 'Session';";
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.AddWithValue("@Resource", $"CompanyRelatedDataSync:{SyncStateSourceSystem}:{SyncKey}");
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to release company related data sync lock.");
        }
    }

    private IEnumerable<string> GetAllSourceObjectNames()
    {
        yield return ResolveSourceObjectName(_options.AuthorizedPersonSourceObjectName);
        yield return ResolveSourceObjectName(_options.BoardDirectorSourceObjectName);
        yield return ResolveSourceObjectName(_options.AttachmentDocumentSourceObjectName);
    }

    private string ResolveSourceObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new InvalidOperationException("Company related data sync source object name is not configured.");
        }

        if (!ObjectNamePattern.IsMatch(objectName))
        {
            throw new InvalidOperationException("Company related data sync source object name contains unsupported characters.");
        }

        return objectName;
    }

    private async Task<Guid> ResolveTenantIdAsync(CancellationToken cancellationToken)
    {
        if (tenantProvider.TenantId.HasValue)
        {
            return tenantProvider.TenantId.Value;
        }

        return await dbContext.Tenants
            .Where(tenant => tenant.Identifier == "default")
            .Select(tenant => tenant.Id)
            .FirstAsync(cancellationToken);
    }

    private string GetLocalConnectionString()
    {
        return configuration.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    private string GetSourceConnectionString()
    {
        var namedConnectionString = string.IsNullOrWhiteSpace(_options.SourceConnectionStringName)
            ? null
            : configuration.GetConnectionString(_options.SourceConnectionStringName);

        if (!string.IsNullOrWhiteSpace(namedConnectionString))
        {
            return namedConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(_options.SourceConnectionString))
        {
            return _options.SourceConnectionString;
        }

        throw new InvalidOperationException("Company related data sync source connection string is not configured.");
    }

    private bool HasConfiguredSourceConnection()
    {
        var namedConnectionString = string.IsNullOrWhiteSpace(_options.SourceConnectionStringName)
            ? null
            : configuration.GetConnectionString(_options.SourceConnectionStringName);

        return !string.IsNullOrWhiteSpace(namedConnectionString) ||
               !string.IsNullOrWhiteSpace(_options.SourceConnectionString);
    }

    private static SyncJobSummaryResponse ToSummary(CompanyRelatedDataSyncStatusResponse status)
    {
        return new SyncJobSummaryResponse(
            SyncKey,
            SyncName,
            SyncDescription,
            string.Join(" + ", status.AuthorizedPersonSourceObjectName, status.BoardDirectorSourceObjectName, status.AttachmentDocumentSourceObjectName),
            "[dbo].[CompanyAuthorizedPersons] + [dbo].[CompanyBoardDirectors] + [dbo].[CompanyAttachmentDocuments]",
            false,
            0,
            status.BatchSize,
            false,
            status.SourceConnectionStringName,
            status.SourceConnectionConfigured,
            status.LocalRowCount,
            status.LastStartedAt,
            status.LastCompletedAt,
            status.LastRunSucceeded,
            status.LastProcessedRows,
            status.LastRunMessage,
            "/configuration/company-related-data");
    }

    private static string BuildCompletedMessage(int authorizedPersonCount, int boardDirectorCount, int attachmentDocumentCount, long? sourceCompanyId)
    {
        var scope = sourceCompanyId.HasValue ? $"source company {sourceCompanyId.Value}" : "all companies";
        return $"Synced {authorizedPersonCount} authorized persons, {boardDirectorCount} board directors, and {attachmentDocumentCount} attachment documents for {scope}.";
    }

    private static DataTable CreateAuthorizedPersonTable()
    {
        var table = new DataTable();
        table.Columns.Add("CompanyProfileId", typeof(Guid));
        table.Columns.Add("MigratedId", typeof(long));
        table.Columns.Add("FullName", typeof(string));
        table.Columns.Add("Designation", typeof(string));
        table.Columns.Add("LegacyIdentityTypeId", typeof(int));
        table.Columns.Add("IdentityNumber", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("TelephoneNo", typeof(string));
        table.Columns.Add("LegacyUserId", typeof(int));
        table.Columns.Add("IsDigiCertPaid", typeof(bool));
        table.Columns.Add("IsCertified", typeof(bool));
        table.Columns.Add("IsPinVerified", typeof(bool));
        table.Columns.Add("IsDeletedInSource", typeof(bool));
        table.Columns.Add("LegacyTitleId", typeof(int));
        table.Columns.Add("LegacyCitizenshipId", typeof(long));
        table.Columns.Add("CanEdit", typeof(bool));
        table.Columns.Add("SourceCreatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceCreatedAt", typeof(DateTime));
        table.Columns.Add("SourceUpdatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceUpdatedAt", typeof(DateTime));
        return table;
    }

    private static DataTable CreateBoardDirectorTable()
    {
        var table = new DataTable();
        table.Columns.Add("CompanyProfileId", typeof(Guid));
        table.Columns.Add("MigratedId", typeof(long));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("LegacyNationalityId", typeof(long));
        table.Columns.Add("SharePercentage", typeof(decimal));
        table.Columns.Add("SourceCreatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceCreatedAt", typeof(DateTime));
        table.Columns.Add("SourceUpdatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceUpdatedAt", typeof(DateTime));
        return table;
    }

    private static DataTable CreateAttachmentDocumentTable()
    {
        var table = new DataTable();
        table.Columns.Add("CompanyProfileId", typeof(Guid));
        table.Columns.Add("MigratedId", typeof(long));
        table.Columns.Add("FileName", typeof(string));
        table.Columns.Add("FileType", typeof(string));
        table.Columns.Add("FileContent", typeof(byte[]));
        table.Columns.Add("SourceCreatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceCreatedAt", typeof(DateTime));
        table.Columns.Add("SourceUpdatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceUpdatedAt", typeof(DateTime));
        return table;
    }

    private static string? GetNullableString(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).Trim();
    }

    private static int? GetNullableInt32(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static long? GetNullableInt64(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static bool? GetNullableBoolean(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    }

    private static decimal? GetNullableDecimal(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static DateTimeOffset? ToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }

    private sealed record CompanyRelatedDataSyncStateRow(
        long? LastSourceCompanyId,
        DateTime? LastStartedAt,
        DateTime? LastCompletedAt,
        bool? LastRunSucceeded,
        int LastProcessedRows,
        string? LastRunMessage);
}
