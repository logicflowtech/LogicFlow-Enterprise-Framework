using System.Data;
using System.Text.RegularExpressions;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class CompanyProfileSyncService(
    IConfiguration configuration,
    IOptions<CompanyProfileSyncOptions> options,
    IOptions<AddressSyncOptions> addressSyncOptions,
    ILogger<CompanyProfileSyncService> logger) : ICompanyProfileSyncService
{
    private const string SourceName = "syn_Company";
    private const string SyncLockResource = "CompanyProfileSync:syn_Company";
    private static readonly Regex ObjectNamePattern = new("^[A-Za-z0-9_\\.\\[\\]]+$", RegexOptions.Compiled);
    private readonly CompanyProfileSyncOptions _options = options.Value;
    private readonly AddressSyncOptions _addressSyncOptions = addressSyncOptions.Value;

    public async Task<CompanyProfileSyncStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureSyncStateRowAsync(localConnection, cancellationToken);
        return await ReadStatusAsync(localConnection, cancellationToken);
    }

    public async Task<CompanyProfileSyncStatusResponse> RunSyncAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureSyncStateRowAsync(localConnection, cancellationToken);
        await AcquireAppLockAsync(localConnection, cancellationToken);

        try
        {
            var startedAt = DateTimeOffset.UtcNow;
            await MarkStartedAsync(localConnection, startedAt, cancellationToken);

            SqlConnection? externalSourceConnection = null;
            var sourceConnection = localConnection;
            if (!_options.UseLocalSynonym)
            {
                externalSourceConnection = new SqlConnection(GetSourceConnectionString());
                await externalSourceConnection.OpenAsync(cancellationToken);
                sourceConnection = externalSourceConnection;
            }

            try
            {
                await ValidateSourceConfigurationAsync(localConnection, sourceConnection, cancellationToken);
                var sourceRows = await LoadAllSourceRowsAsync(sourceConnection, cancellationToken);
                await ReplaceLocalCacheAsync(localConnection, sourceRows, cancellationToken);
                var syncedAddressCount = await SyncReferencedAddressesAsync(localConnection, sourceRows, cancellationToken);

                var totalProcessed = sourceRows.Rows.Count;
                DateTime? lastModified = null;
                long? lastId = null;

                if (sourceRows.Rows.Count > 0)
                {
                    var lastRow = sourceRows.Rows[sourceRows.Rows.Count - 1];
                    lastModified = ReadNullableDateTime(lastRow["SourceWatermarkDateTime"]);
                    lastId = ReadNullableInt64(lastRow["MigratedId"]);
                }

                await MarkCompletedAsync(
                    localConnection,
                    totalProcessed,
                    lastModified,
                    lastId,
                    cancellationToken,
                    $"Success. Full refresh loaded {totalProcessed} company rows and synced {syncedAddressCount} related addresses.");
            }
            finally
            {
                if (externalSourceConnection is not null)
                {
                    await externalSourceConnection.DisposeAsync();
                }
            }

            return await ReadStatusAsync(localConnection, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Company profile sync failed.");
            await TryMarkFailedAsync(localConnection, exception.Message, cancellationToken);
            throw;
        }
        finally
        {
            await ReleaseAppLockAsync(localConnection, cancellationToken);
        }
    }

    private async Task ValidateSourceConfigurationAsync(
        SqlConnection localConnection,
        SqlConnection sourceConnection,
        CancellationToken cancellationToken)
    {
        if (_options.UseLocalSynonym)
        {
            await EnsureLocalSynonymExistsAsync(localConnection, cancellationToken);
            return;
        }

        await EnsureSourceObjectExistsAsync(sourceConnection, cancellationToken);
    }

    private async Task EnsureLocalSynonymExistsAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM sys.synonyms
            WHERE name = N'syn_Company'
              AND schema_id = SCHEMA_ID(N'dbo');
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;

        var synonymExists = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        if (synonymExists)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Company profile sync is running in local synonym mode, but {_options.LocalSynonymName} was not found in the DefaultConnection database '{localConnection.Database}'. " +
            "Either create the synonym in that database or set CompanyProfileSync:UseLocalSynonym=false and configure ConnectionStrings:CompanyProfileSource.");
    }

    private async Task EnsureSourceObjectExistsAsync(SqlConnection sourceConnection, CancellationToken cancellationToken)
    {
        var sourceObjectName = ResolveSourceObjectName();
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
            $"Company profile sync source object {sourceObjectName} was not found in source database '{sourceConnection.Database}'. " +
            $"Check ConnectionStrings:{_options.SourceConnectionStringName} or CompanyProfileSync:SourceConnectionString, and confirm CompanyProfileSync:SourceObjectName.");
    }

    private async Task<DataTable> LoadAllSourceRowsAsync(SqlConnection sourceConnection, CancellationToken cancellationToken)
    {
        var sourceObjectName = ResolveSourceObjectName();
        var query = $"""
            WITH SourceData AS
            (
                SELECT
                    CAST(src.ID AS BIGINT) AS MigratedId,
                    NULLIF(LTRIM(RTRIM(src.COMPANYNAME)), N'') AS CompanyName,
                    NULLIF(LTRIM(RTRIM(src.REGISTRATIONNO)), N'') AS RegistrationNo,
                    CASE WHEN src.REGISTRATIONDATE IS NULL OR CONVERT(date, src.REGISTRATIONDATE) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.REGISTRATIONDATE) END AS RegistrationDate,
                    CASE WHEN src.DATEOFINCORPORATION IS NULL OR CONVERT(date, src.DATEOFINCORPORATION) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.DATEOFINCORPORATION) END AS DateOfIncorporation,
                    NULLIF(LTRIM(RTRIM(src.TELEPHONENO)), N'') AS TelephoneNo,
                    NULLIF(LTRIM(RTRIM(src.FAXNO)), N'') AS FaxNo,
                    NULLIF(LTRIM(RTRIM(src.WEBSITE)), N'') AS Website,
                    NULLIF(LTRIM(RTRIM(src.EMAIL)), N'') AS Email,
                    NULLIF(LTRIM(RTRIM(src.INCOMETAXNO)), N'') AS IncomeTaxNo,
                    NULLIF(LTRIM(RTRIM(src.EPFNO)), N'') AS EpfNo,
                    NULLIF(LTRIM(RTRIM(src.SOCSONO)), N'') AS SocsoNo,
                    src.USERID AS UserId,
                    src.COMPANYSIGNATUREID AS CompanySignatureId,
                    src.COMPANYTYPE AS CompanyType,
                    src.ISCOMPANYCERTIFIED AS IsCompanyCertified,
                    src.COMPANYAPPROVALSTATUS AS CompanyApprovalStatus,
                    src.ISPAID AS IsPaid,
                    src.ISCOMPANYLOCAL AS IsCompanyLocal,
                    src.CREATEDBY AS CreatedBySourceUserId,
                    CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END AS SourceCreatedDateTime,
                    src.MODIFIEDBY AS ModifiedBySourceUserId,
                    CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END AS SourceModifiedDateTime,
                    src.ADDRESSID AS LegacyAddressId,
                    NULLIF(src.BACKGROUNDDESCRIPTION1, N'') AS BackgroundDescription1,
                    NULLIF(LTRIM(RTRIM(src.NEWSSM_COMPANYREGNO)), N'') AS NewSsmCompanyRegNo,
                    src.COMPANYSTATUSID AS CompanyStatusId,
                    src.TOTALEMPLOYMENT AS TotalEmployment,
                    src.ANNUALCLOSINGDATE_DAY AS AnnualClosingDateDay,
                    src.ANNUALCLOSINGDATE_MONTH AS AnnualClosingDateMonth,
                    NULLIF(LTRIM(RTRIM(src.APRNO)), N'') AS AprNo,
                    NULLIF(LTRIM(RTRIM(src.NON)), N'') AS NonCode,
                    COALESCE(
                        CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END,
                        CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END,
                        CONVERT(DATETIME2(3), '1900-01-01')
                    ) AS SourceWatermarkDateTime
                FROM {sourceObjectName} AS src
            )
            SELECT *
            FROM SourceData
            ORDER BY SourceWatermarkDateTime, MigratedId;
            """;

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private async Task ReplaceLocalCacheAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#CompanyProfileSyncBatch') IS NOT NULL
            BEGIN
                DROP TABLE #CompanyProfileSyncBatch;
            END;

            CREATE TABLE #CompanyProfileSyncBatch
            (
                MigratedId BIGINT NOT NULL,
                CompanyName NVARCHAR(100) NULL,
                RegistrationNo NVARCHAR(50) NULL,
                RegistrationDate DATETIME2(3) NULL,
                DateOfIncorporation DATETIME2(3) NULL,
                TelephoneNo NVARCHAR(100) NULL,
                FaxNo NVARCHAR(100) NULL,
                Website NVARCHAR(100) NULL,
                Email NVARCHAR(250) NULL,
                IncomeTaxNo NVARCHAR(50) NULL,
                EpfNo NVARCHAR(20) NULL,
                SocsoNo NVARCHAR(20) NULL,
                UserId INT NULL,
                CompanySignatureId BIGINT NULL,
                CompanyType INT NULL,
                IsCompanyCertified BIT NULL,
                CompanyApprovalStatus INT NULL,
                IsPaid BIT NULL,
                IsCompanyLocal BIT NULL,
                CreatedBySourceUserId INT NULL,
                SourceCreatedDateTime DATETIME2(3) NULL,
                ModifiedBySourceUserId INT NULL,
                SourceModifiedDateTime DATETIME2(3) NULL,
                LegacyAddressId BIGINT NULL,
                BackgroundDescription1 NVARCHAR(MAX) NULL,
                NewSsmCompanyRegNo NVARCHAR(50) NULL,
                CompanyStatusId INT NULL,
                TotalEmployment INT NULL,
                AnnualClosingDateDay INT NULL,
                AnnualClosingDateMonth INT NULL,
                AprNo NVARCHAR(50) NULL,
                NonCode NVARCHAR(2) NULL,
                SourceWatermarkDateTime DATETIME2(3) NULL
            );
            """;

        await using (var createTempTableCommand = localConnection.CreateCommand())
        {
            createTempTableCommand.CommandText = createTempTableSql;
            createTempTableCommand.CommandTimeout = _options.CommandTimeoutSeconds;
            await createTempTableCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var bulkCopy = new SqlBulkCopy(localConnection))
        {
            bulkCopy.DestinationTableName = "#CompanyProfileSyncBatch";
            bulkCopy.BatchSize = _options.BatchSize;
            bulkCopy.BulkCopyTimeout = _options.CommandTimeoutSeconds;

            foreach (DataColumn column in sourceRows.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(sourceRows, cancellationToken);
        }

        const string mergeSql = """
            MERGE dbo.CompanyProfiles AS target
            USING
            (
                SELECT
                    source.MigratedId,
                    source.CompanyName,
                    source.RegistrationNo,
                    source.RegistrationDate,
                    source.DateOfIncorporation,
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
                    addresses.Id AS AddressId,
                    source.LegacyAddressId,
                    source.BackgroundDescription1,
                    source.NewSsmCompanyRegNo,
                    source.CompanyStatusId,
                    source.TotalEmployment,
                    source.AnnualClosingDateDay,
                    source.AnnualClosingDateMonth,
                    source.AprNo,
                    source.NonCode
                FROM #CompanyProfileSyncBatch AS source
                LEFT JOIN dbo.Addresses AS addresses ON addresses.MigratedId = source.LegacyAddressId
            ) AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    MigratedId = source.MigratedId,
                    CompanyName = source.CompanyName,
                    RegistrationNo = source.RegistrationNo,
                    RegistrationDate = source.RegistrationDate,
                    DateOfIncorporation = source.DateOfIncorporation,
                    TelephoneNo = source.TelephoneNo,
                    FaxNo = source.FaxNo,
                    Website = source.Website,
                    Email = source.Email,
                    IncomeTaxNo = source.IncomeTaxNo,
                    EpfNo = source.EpfNo,
                    SocsoNo = source.SocsoNo,
                    UserId = source.UserId,
                    CompanySignatureId = source.CompanySignatureId,
                    CompanyType = source.CompanyType,
                    IsCompanyCertified = source.IsCompanyCertified,
                    CompanyApprovalStatus = source.CompanyApprovalStatus,
                    IsPaid = source.IsPaid,
                    IsCompanyLocal = source.IsCompanyLocal,
                    CreatedBySourceUserId = source.CreatedBySourceUserId,
                    SourceCreatedDateTime = source.SourceCreatedDateTime,
                    ModifiedBySourceUserId = source.ModifiedBySourceUserId,
                    SourceModifiedDateTime = source.SourceModifiedDateTime,
                    AddressId = source.AddressId,
                    LegacyAddressId = source.LegacyAddressId,
                    BackgroundDescription1 = source.BackgroundDescription1,
                    NewSsmCompanyRegNo = source.NewSsmCompanyRegNo,
                    CompanyStatusId = source.CompanyStatusId,
                    TotalEmployment = source.TotalEmployment,
                    AnnualClosingDateDay = source.AnnualClosingDateDay,
                    AnnualClosingDateMonth = source.AnnualClosingDateMonth,
                    AprNo = source.AprNo,
                    NonCode = source.NonCode,
                    LastSyncedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED BY TARGET THEN
                INSERT
                (
                    MigratedId,
                    CompanyName,
                    RegistrationNo,
                    RegistrationDate,
                    DateOfIncorporation,
                    TelephoneNo,
                    FaxNo,
                    Website,
                    Email,
                    IncomeTaxNo,
                    EpfNo,
                    SocsoNo,
                    UserId,
                    CompanySignatureId,
                    CompanyType,
                    IsCompanyCertified,
                    CompanyApprovalStatus,
                    IsPaid,
                    IsCompanyLocal,
                    CreatedBySourceUserId,
                    SourceCreatedDateTime,
                    ModifiedBySourceUserId,
                    SourceModifiedDateTime,
                    AddressId,
                    LegacyAddressId,
                    BackgroundDescription1,
                    NewSsmCompanyRegNo,
                    CompanyStatusId,
                    TotalEmployment,
                    AnnualClosingDateDay,
                    AnnualClosingDateMonth,
                    AprNo,
                    NonCode,
                    LastSyncedAt
                )
                VALUES
                (
                    source.MigratedId,
                    source.CompanyName,
                    source.RegistrationNo,
                    source.RegistrationDate,
                    source.DateOfIncorporation,
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
                    source.AddressId,
                    source.LegacyAddressId,
                    source.BackgroundDescription1,
                    source.NewSsmCompanyRegNo,
                    source.CompanyStatusId,
                    source.TotalEmployment,
                    source.AnnualClosingDateDay,
                    source.AnnualClosingDateMonth,
                    source.AprNo,
                    source.NonCode,
                    SYSUTCDATETIME()
                )
            WHEN NOT MATCHED BY SOURCE AND target.MigratedId IS NOT NULL THEN
                DELETE;
            """;

        await using var mergeCommand = localConnection.CreateCommand();
        mergeCommand.CommandText = mergeSql;
        mergeCommand.CommandTimeout = _options.CommandTimeoutSeconds;
        await mergeCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<int> SyncReferencedAddressesAsync(
        SqlConnection localConnection,
        DataTable companyRows,
        CancellationToken cancellationToken)
    {
        var addressIds = companyRows.AsEnumerable()
            .Select(row => ReadNullableInt64(row["LegacyAddressId"]))
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .ToList();

        if (addressIds.Count == 0)
        {
            return 0;
        }

        var sourceRows = await LoadReferencedAddressRowsAsync(addressIds, cancellationToken);
        if (sourceRows.Rows.Count == 0)
        {
            return 0;
        }

        await UpsertReferencedAddressesAsync(localConnection, sourceRows, cancellationToken);
        return sourceRows.Rows.Count;
    }

    private async Task<DataTable> LoadReferencedAddressRowsAsync(
        IReadOnlyCollection<long> addressIds,
        CancellationToken cancellationToken)
    {
        await using var sourceConnection = new SqlConnection(GetAddressSourceConnectionString());
        await sourceConnection.OpenAsync(cancellationToken);
        await EnsureAddressSourceObjectExistsAsync(sourceConnection, cancellationToken);

        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#CompanyReferencedAddressIds') IS NOT NULL
            BEGIN
                DROP TABLE #CompanyReferencedAddressIds;
            END;

            CREATE TABLE #CompanyReferencedAddressIds
            (
                MigratedId BIGINT NOT NULL PRIMARY KEY
            );
            """;

        await using (var createTempTableCommand = sourceConnection.CreateCommand())
        {
            createTempTableCommand.CommandText = createTempTableSql;
            createTempTableCommand.CommandTimeout = _addressSyncOptions.CommandTimeoutSeconds;
            await createTempTableCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var idTable = new DataTable();
        idTable.Columns.Add("MigratedId", typeof(long));
        foreach (var addressId in addressIds)
        {
            idTable.Rows.Add(addressId);
        }

        using (var bulkCopy = new SqlBulkCopy(sourceConnection))
        {
            bulkCopy.DestinationTableName = "#CompanyReferencedAddressIds";
            bulkCopy.BatchSize = _addressSyncOptions.BatchSize;
            bulkCopy.BulkCopyTimeout = _addressSyncOptions.CommandTimeoutSeconds;
            bulkCopy.ColumnMappings.Add("MigratedId", "MigratedId");
            await bulkCopy.WriteToServerAsync(idTable, cancellationToken);
        }

        var query = $"""
            SELECT
                CAST(src.ID AS BIGINT) AS MigratedId,
                NULLIF(LTRIM(RTRIM(src.ADDRESS1)), N'') AS AddressLine1,
                NULLIF(LTRIM(RTRIM(src.ADDRESS2)), N'') AS AddressLine2,
                NULLIF(LTRIM(RTRIM(src.ADDRESS3)), N'') AS AddressLine3,
                CAST(src.COUNTRYID AS BIGINT) AS LegacyCountryId,
                CAST(src.STATEID AS BIGINT) AS LegacyStateId,
                CAST(src.CITYID AS BIGINT) AS LegacyCityId,
                NULLIF(LTRIM(RTRIM(src.STATENAME)), N'') AS StateName,
                NULLIF(LTRIM(RTRIM(src.CITYNAME)), N'') AS CityName,
                NULLIF(LTRIM(RTRIM(src.POSTCODE)), N'') AS Postcode,
                src.CREATEDBY AS SourceCreatedByLegacyUserId,
                CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END AS SourceCreatedAt,
                src.MODIFIEDBY AS SourceUpdatedByLegacyUserId,
                CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END AS SourceUpdatedAt
            FROM {ResolveAddressSourceObjectName()} AS src
            INNER JOIN #CompanyReferencedAddressIds AS filterIds ON filterIds.MigratedId = CAST(src.ID AS BIGINT)
            ORDER BY src.ID;
            """;

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _addressSyncOptions.CommandTimeoutSeconds;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private async Task UpsertReferencedAddressesAsync(
        SqlConnection localConnection,
        DataTable sourceRows,
        CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#CompanyReferencedAddressBatch') IS NOT NULL
            BEGIN
                DROP TABLE #CompanyReferencedAddressBatch;
            END;

            CREATE TABLE #CompanyReferencedAddressBatch
            (
                MigratedId BIGINT NOT NULL,
                AddressLine1 NVARCHAR(200) NULL,
                AddressLine2 NVARCHAR(200) NULL,
                AddressLine3 NVARCHAR(200) NULL,
                LegacyCountryId BIGINT NULL,
                LegacyStateId BIGINT NULL,
                LegacyCityId BIGINT NULL,
                StateName NVARCHAR(200) NULL,
                CityName NVARCHAR(200) NULL,
                Postcode NVARCHAR(100) NULL,
                SourceCreatedByLegacyUserId INT NULL,
                SourceCreatedAt DATETIME2(3) NULL,
                SourceUpdatedByLegacyUserId INT NULL,
                SourceUpdatedAt DATETIME2(3) NULL
            );
            """;

        await using (var createTempTableCommand = localConnection.CreateCommand())
        {
            createTempTableCommand.CommandText = createTempTableSql;
            createTempTableCommand.CommandTimeout = _addressSyncOptions.CommandTimeoutSeconds;
            await createTempTableCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var bulkCopy = new SqlBulkCopy(localConnection))
        {
            bulkCopy.DestinationTableName = "#CompanyReferencedAddressBatch";
            bulkCopy.BatchSize = _addressSyncOptions.BatchSize;
            bulkCopy.BulkCopyTimeout = _addressSyncOptions.CommandTimeoutSeconds;

            foreach (DataColumn column in sourceRows.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(sourceRows, cancellationToken);
        }

        const string mergeSql = """
            MERGE dbo.Addresses AS target
            USING
            (
                SELECT
                    source.MigratedId,
                    source.AddressLine1,
                    source.AddressLine2,
                    source.AddressLine3,
                    countries.Id AS CountryId,
                    CASE
                        WHEN states.Id IS NULL THEN NULL
                        WHEN NULLIF(LTRIM(RTRIM(source.StateName)), N'') IS NOT NULL
                            AND UPPER(REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(states.Name)), N' ', N''), N'.', N''), N'-', N''), N',', N'')) =
                                UPPER(REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(source.StateName)), N' ', N''), N'.', N''), N'-', N''), N',', N''))
                            THEN states.Id
                        ELSE NULL
                    END AS StateId,
                    CASE
                        WHEN cities.Id IS NULL THEN NULL
                        WHEN NULLIF(LTRIM(RTRIM(source.CityName)), N'') IS NOT NULL
                            AND UPPER(REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(cities.Name)), N' ', N''), N'.', N''), N'-', N''), N',', N'')) =
                                UPPER(REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(source.CityName)), N' ', N''), N'.', N''), N'-', N''), N',', N''))
                            THEN cities.Id
                        ELSE NULL
                    END AS CityId,
                    CAST(NULL AS NVARCHAR(200)) AS CountryName,
                    source.StateName,
                    source.CityName,
                    source.Postcode,
                    source.SourceCreatedByLegacyUserId,
                    source.SourceCreatedAt,
                    source.SourceUpdatedByLegacyUserId,
                    source.SourceUpdatedAt
                FROM #CompanyReferencedAddressBatch AS source
                LEFT JOIN dbo.LookupCountries AS countries ON countries.MigratedId = source.LegacyCountryId
                LEFT JOIN dbo.LookupStates AS states ON states.MigratedId = source.LegacyStateId
                LEFT JOIN dbo.LookupCities AS cities ON cities.MigratedId = source.LegacyCityId
            ) AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    AddressLine1 = source.AddressLine1,
                    AddressLine2 = source.AddressLine2,
                    AddressLine3 = source.AddressLine3,
                    CountryId = source.CountryId,
                    StateId = CASE
                        WHEN NULLIF(LTRIM(RTRIM(source.StateName)), N'') IS NULL AND target.StateName IS NOT NULL THEN target.StateId
                        ELSE source.StateId
                    END,
                    CityId = CASE
                        WHEN NULLIF(LTRIM(RTRIM(source.CityName)), N'') IS NULL AND target.CityName IS NOT NULL THEN target.CityId
                        ELSE source.CityId
                    END,
                    CountryName = source.CountryName,
                    StateName = COALESCE(source.StateName, target.StateName),
                    CityName = COALESCE(source.CityName, target.CityName),
                    Postcode = source.Postcode,
                    SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
                    SourceCreatedAt = source.SourceCreatedAt,
                    SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt = source.SourceUpdatedAt,
                    LastSyncedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED BY TARGET THEN
                INSERT
                (
                    MigratedId,
                    AddressLine1,
                    AddressLine2,
                    AddressLine3,
                    CountryId,
                    StateId,
                    CityId,
                    CountryName,
                    StateName,
                    CityName,
                    Postcode,
                    SourceCreatedByLegacyUserId,
                    SourceCreatedAt,
                    SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt,
                    LastSyncedAt
                )
                VALUES
                (
                    source.MigratedId,
                    source.AddressLine1,
                    source.AddressLine2,
                    source.AddressLine3,
                    source.CountryId,
                    source.StateId,
                    source.CityId,
                    source.CountryName,
                    source.StateName,
                    source.CityName,
                    source.Postcode,
                    source.SourceCreatedByLegacyUserId,
                    source.SourceCreatedAt,
                    source.SourceUpdatedByLegacyUserId,
                    source.SourceUpdatedAt,
                    SYSUTCDATETIME()
                );
            """;

        await using var mergeCommand = localConnection.CreateCommand();
        mergeCommand.CommandText = mergeSql;
        mergeCommand.CommandTimeout = _addressSyncOptions.CommandTimeoutSeconds;
        await mergeCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureSyncStateRowAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.CompanyProfileSyncState WHERE SourceName = N'syn_Company')
            BEGIN
                INSERT INTO dbo.CompanyProfileSyncState (SourceName, LastRunSucceeded, LastProcessedRows, LastRunMessage)
                VALUES (N'syn_Company', NULL, 0, N'Not started');
            END;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<CompanyProfileSyncStatusResponse> ReadStatusAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string statusSql = """
            SELECT
                LastSourceModifiedDateTime,
                LastSourceCompanyId,
                LastStartedAt,
                LastCompletedAt,
                LastRunSucceeded,
                LastProcessedRows,
                LastRunMessage
            FROM dbo.CompanyProfileSyncState
            WHERE SourceName = @SourceName;
            """;

        DateTime? lastSourceModifiedDateTime = null;
        long? lastSourceCompanyId = null;
        DateTime? lastStartedAt = null;
        DateTime? lastCompletedAt = null;
        bool? lastRunSucceeded = null;
        var lastProcessedRows = 0;
        string? lastRunMessage = null;

        await using (var command = localConnection.CreateCommand())
        {
            command.CommandText = statusSql;
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.AddWithValue("@SourceName", SourceName);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                lastSourceModifiedDateTime = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                lastSourceCompanyId = reader.IsDBNull(1) ? null : reader.GetInt64(1);
                lastStartedAt = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
                lastCompletedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
                lastRunSucceeded = reader.IsDBNull(4) ? null : reader.GetBoolean(4);
                lastProcessedRows = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                lastRunMessage = reader.IsDBNull(6) ? null : reader.GetString(6);
            }
        }

        long localRowCount;
        await using (var countCommand = localConnection.CreateCommand())
        {
            countCommand.CommandText = "SELECT COUNT_BIG(*) FROM dbo.CompanyProfiles;";
            countCommand.CommandTimeout = _options.CommandTimeoutSeconds;
            localRowCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));
        }

        return new CompanyProfileSyncStatusResponse(
            _options.ScheduleEnabled,
            _options.UseLocalSynonym,
            _options.UseLocalSynonym ? null : _options.SourceConnectionStringName,
            _options.UseLocalSynonym || HasConfiguredSourceConnection(),
            _options.UseLocalSynonym ? _options.LocalSynonymName : _options.SourceObjectName,
            _options.BatchSize,
            _options.ScheduleMinutes,
            localRowCount,
            ToUtc(lastStartedAt),
            ToUtc(lastCompletedAt),
            lastRunSucceeded,
            lastProcessedRows,
            lastRunMessage,
            lastSourceModifiedDateTime,
            lastSourceCompanyId);
    }

    private async Task MarkStartedAsync(SqlConnection localConnection, DateTimeOffset startedAt, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.CompanyProfileSyncState
            SET LastStartedAt = @LastStartedAt,
                LastRunSucceeded = NULL,
                LastRunMessage = N'Running full refresh'
            WHERE SourceName = @SourceName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@LastStartedAt", startedAt.UtcDateTime);
        command.Parameters.AddWithValue("@SourceName", SourceName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkCompletedAsync(
        SqlConnection localConnection,
        int totalProcessed,
        DateTime? lastModified,
        long? lastId,
        CancellationToken cancellationToken,
        string? runMessage = null)
    {
        const string sql = """
            UPDATE dbo.CompanyProfileSyncState
            SET LastSourceModifiedDateTime = COALESCE(@LastSourceModifiedDateTime, LastSourceModifiedDateTime),
                LastSourceCompanyId = COALESCE(@LastSourceCompanyId, LastSourceCompanyId),
                LastCompletedAt = @LastCompletedAt,
                LastRunSucceeded = 1,
                LastProcessedRows = @LastProcessedRows,
                LastRunMessage = @LastRunMessage
            WHERE SourceName = @SourceName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@LastSourceModifiedDateTime", lastModified.HasValue ? lastModified.Value : DBNull.Value);
        command.Parameters.AddWithValue("@LastSourceCompanyId", lastId.HasValue ? lastId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@LastCompletedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@LastProcessedRows", totalProcessed);
        command.Parameters.AddWithValue("@LastRunMessage", runMessage ?? $"Success. Full refresh loaded {totalProcessed} rows.");
        command.Parameters.AddWithValue("@SourceName", SourceName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task TryMarkFailedAsync(SqlConnection localConnection, string message, CancellationToken cancellationToken)
    {
        try
        {
            const string sql = """
                UPDATE dbo.CompanyProfileSyncState
                SET LastCompletedAt = @LastCompletedAt,
                    LastRunSucceeded = 0,
                    LastProcessedRows = 0,
                    LastRunMessage = @LastRunMessage
                WHERE SourceName = @SourceName;
                """;

            await using var command = localConnection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.AddWithValue("@LastCompletedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@LastRunMessage", message.Length > 4000 ? message[..4000] : message);
            command.Parameters.AddWithValue("@SourceName", SourceName);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to mark company profile sync as failed.");
        }
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
        command.Parameters.AddWithValue("@Resource", SyncLockResource);

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
        {
            throw new InvalidOperationException("Unable to acquire company profile sync lock.");
        }
    }

    private async Task ReleaseAppLockAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = localConnection.CreateCommand();
            command.CommandText = "EXEC sp_releaseapplock @Resource = @Resource, @LockOwner = 'Session';";
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.AddWithValue("@Resource", SyncLockResource);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to release company profile sync lock.");
        }
    }

    private string ResolveSourceObjectName()
    {
        var objectName = _options.UseLocalSynonym ? _options.LocalSynonymName : _options.SourceObjectName;
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new InvalidOperationException("Company profile sync source object name is not configured.");
        }

        if (!ObjectNamePattern.IsMatch(objectName))
        {
            throw new InvalidOperationException("Company profile sync source object name contains unsupported characters.");
        }

        return objectName;
    }

    private string GetLocalConnectionString()
    {
        return configuration.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    private string GetSourceConnectionString()
    {
        if (_options.UseLocalSynonym)
        {
            return GetLocalConnectionString();
        }

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

        throw new InvalidOperationException("Company profile source connection string is not configured.");
    }

    private async Task EnsureAddressSourceObjectExistsAsync(SqlConnection sourceConnection, CancellationToken cancellationToken)
    {
        var sourceObjectName = ResolveAddressSourceObjectName();
        var sql = $"SELECT OBJECT_ID(N'{sourceObjectName.Replace("'", "''")}');";

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _addressSyncOptions.CommandTimeoutSeconds;

        var objectId = await command.ExecuteScalarAsync(cancellationToken);
        if (objectId is not DBNull && objectId is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Company-related address sync source object {sourceObjectName} was not found in source database '{sourceConnection.Database}'. " +
            $"Check ConnectionStrings:{_addressSyncOptions.SourceConnectionStringName} or AddressSync:SourceConnectionString.");
    }

    private string ResolveAddressSourceObjectName()
    {
        if (string.IsNullOrWhiteSpace(_addressSyncOptions.SourceObjectName))
        {
            throw new InvalidOperationException("Company-related address sync source object name is not configured.");
        }

        if (!ObjectNamePattern.IsMatch(_addressSyncOptions.SourceObjectName))
        {
            throw new InvalidOperationException("Company-related address sync source object name contains unsupported characters.");
        }

        return _addressSyncOptions.SourceObjectName;
    }

    private string GetAddressSourceConnectionString()
    {
        var namedConnectionString = string.IsNullOrWhiteSpace(_addressSyncOptions.SourceConnectionStringName)
            ? null
            : configuration.GetConnectionString(_addressSyncOptions.SourceConnectionStringName);

        if (!string.IsNullOrWhiteSpace(namedConnectionString))
        {
            return namedConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(_addressSyncOptions.SourceConnectionString))
        {
            return _addressSyncOptions.SourceConnectionString;
        }

        throw new InvalidOperationException("Company-related address source connection string is not configured.");
    }

    private bool HasConfiguredSourceConnection()
    {
        if (_options.UseLocalSynonym)
        {
            return true;
        }

        var namedConnectionString = string.IsNullOrWhiteSpace(_options.SourceConnectionStringName)
            ? null
            : configuration.GetConnectionString(_options.SourceConnectionStringName);

        return !string.IsNullOrWhiteSpace(namedConnectionString) ||
               !string.IsNullOrWhiteSpace(_options.SourceConnectionString);
    }

    private static DateTimeOffset? ToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }

    private static DateTime? ReadNullableDateTime(object value)
    {
        return value == DBNull.Value ? null : Convert.ToDateTime(value);
    }

    private static long? ReadNullableInt64(object value)
    {
        return value == DBNull.Value ? null : Convert.ToInt64(value);
    }
}
