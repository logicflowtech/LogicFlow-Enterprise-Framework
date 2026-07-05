using System.Data;
using System.Text.RegularExpressions;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class AddressSyncService(
    IConfiguration configuration,
    IOptions<AddressSyncOptions> options,
    ILogger<AddressSyncService> logger) : IAddressSyncService
{
    private const string SyncKey = "addresses";
    private const string SyncLockResource = "AddressSync:OSUSR_Z5Z_ADDRESS";
    private static readonly Regex ObjectNamePattern = new("^[A-Za-z0-9_\\.\\[\\]]+$", RegexOptions.Compiled);
    private readonly AddressSyncOptions _options = options.Value;

    public async Task<SyncJobSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await SyncStateStore.EnsureRowAsync(localConnection, SyncKey, _options.CommandTimeoutSeconds, cancellationToken);

        var state = await SyncStateStore.ReadAsync(localConnection, SyncKey, _options.CommandTimeoutSeconds, cancellationToken);
        long localAddressCount;
        await using (var countCommand = localConnection.CreateCommand())
        {
            countCommand.CommandText = "SELECT COUNT_BIG(*) FROM dbo.Addresses;";
            countCommand.CommandTimeout = _options.CommandTimeoutSeconds;
            localAddressCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));
        }

        return new SyncJobSummaryResponse(
            SyncKey,
            "Address Sync",
            "Imports legacy addresses and backfills UserProfiles.AddressId and CompanyProfiles.AddressId.",
            ResolveSourceObjectName(),
            "[dbo].[Addresses]",
            false,
            0,
            _options.BatchSize,
            false,
            _options.SourceConnectionStringName,
            HasConfiguredSourceConnection(),
            localAddressCount,
            ToUtc(state.LastStartedAt),
            ToUtc(state.LastCompletedAt),
            state.LastRunSucceeded,
            state.LastProcessedRows,
            state.LastRunMessage,
            string.Empty);
    }

    public async Task<AddressSyncResponse> RunSyncAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await SyncStateStore.EnsureRowAsync(localConnection, SyncKey, _options.CommandTimeoutSeconds, cancellationToken);
        await AcquireAppLockAsync(localConnection, cancellationToken);

        try
        {
            await SyncStateStore.MarkStartedAsync(localConnection, SyncKey, _options.CommandTimeoutSeconds, cancellationToken, "Running address sync");
            await EnsureSourceObjectExistsAsync(cancellationToken);
            var sourceRows = await LoadSourceRowsAsync(cancellationToken);

            await UpsertAddressesAsync(localConnection, sourceRows, cancellationToken);
            var linkedUserProfiles = await BackfillUserProfileAddressesAsync(localConnection, cancellationToken);
            var linkedCompanyProfiles = await BackfillCompanyProfileAddressesAsync(localConnection, cancellationToken);

            long localAddressCount;
            await using (var countCommand = localConnection.CreateCommand())
            {
                countCommand.CommandText = "SELECT COUNT_BIG(*) FROM dbo.Addresses;";
                countCommand.CommandTimeout = _options.CommandTimeoutSeconds;
                localAddressCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));
            }

            var completionMessage =
                $"Imported {sourceRows.Rows.Count} addresses and linked {linkedUserProfiles} user profiles and {linkedCompanyProfiles} company profiles.";
            await SyncStateStore.MarkCompletedAsync(
                localConnection,
                SyncKey,
                _options.CommandTimeoutSeconds,
                cancellationToken,
                sourceRows.Rows.Count,
                completionMessage);

            return new AddressSyncResponse(
                ResolveSourceObjectName(),
                _options.SourceConnectionStringName,
                HasConfiguredSourceConnection(),
                sourceRows.Rows.Count,
                linkedUserProfiles,
                linkedCompanyProfiles,
                localAddressCount,
                DateTimeOffset.UtcNow,
                completionMessage);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Address sync failed.");
            await SyncStateStore.MarkFailedAsync(localConnection, SyncKey, _options.CommandTimeoutSeconds, cancellationToken, exception.Message);
            throw;
        }
        finally
        {
            await ReleaseAppLockAsync(localConnection, cancellationToken);
        }
    }

    private async Task EnsureSourceObjectExistsAsync(CancellationToken cancellationToken)
    {
        var sourceObjectName = ResolveSourceObjectName();

        await using var sourceConnection = new SqlConnection(GetSourceConnectionString());
        await sourceConnection.OpenAsync(cancellationToken);

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
            $"Address sync source object {sourceObjectName} was not found in source database '{sourceConnection.Database}'. " +
            $"Check ConnectionStrings:{_options.SourceConnectionStringName} or AddressSync:SourceConnectionString, and confirm AddressSync:SourceObjectName.");
    }

    private async Task<DataTable> LoadSourceRowsAsync(CancellationToken cancellationToken)
    {
        var sourceObjectName = ResolveSourceObjectName();
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
            FROM {sourceObjectName} AS src
            ORDER BY src.ID;
            """;

        await using var sourceConnection = new SqlConnection(GetSourceConnectionString());
        await sourceConnection.OpenAsync(cancellationToken);

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private async Task UpsertAddressesAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#AddressSyncBatch') IS NOT NULL
            BEGIN
                DROP TABLE #AddressSyncBatch;
            END;

            CREATE TABLE #AddressSyncBatch
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
            createTempTableCommand.CommandTimeout = _options.CommandTimeoutSeconds;
            await createTempTableCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var bulkCopy = new SqlBulkCopy(localConnection))
        {
            bulkCopy.DestinationTableName = "#AddressSyncBatch";
            bulkCopy.BatchSize = _options.BatchSize;
            bulkCopy.BulkCopyTimeout = _options.CommandTimeoutSeconds;

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
                FROM #AddressSyncBatch AS source
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
        mergeCommand.CommandTimeout = _options.CommandTimeoutSeconds;
        await mergeCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<int> BackfillUserProfileAddressesAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE profile
            SET AddressId = address.Id
            FROM dbo.UserProfiles AS profile
            INNER JOIN dbo.Addresses AS address ON address.MigratedId = profile.LegacyAddressId
            WHERE profile.LegacyAddressId IS NOT NULL
              AND (profile.AddressId IS NULL OR profile.AddressId <> address.Id);

            SELECT @@ROWCOUNT;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<int> BackfillCompanyProfileAddressesAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE profile
            SET AddressId = address.Id
            FROM dbo.CompanyProfiles AS profile
            INNER JOIN dbo.Addresses AS address ON address.MigratedId = profile.LegacyAddressId
            WHERE profile.LegacyAddressId IS NOT NULL
              AND (profile.AddressId IS NULL OR profile.AddressId <> address.Id);

            SELECT @@ROWCOUNT;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private string ResolveSourceObjectName()
    {
        if (string.IsNullOrWhiteSpace(_options.SourceObjectName))
        {
            throw new InvalidOperationException("Address sync source object name is not configured.");
        }

        if (!ObjectNamePattern.IsMatch(_options.SourceObjectName))
        {
            throw new InvalidOperationException("Address sync source object name contains unsupported characters.");
        }

        return _options.SourceObjectName;
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

        throw new InvalidOperationException("Address sync source connection string is not configured.");
    }

    private bool HasConfiguredSourceConnection()
    {
        var namedConnectionString = string.IsNullOrWhiteSpace(_options.SourceConnectionStringName)
            ? null
            : configuration.GetConnectionString(_options.SourceConnectionStringName);

        return !string.IsNullOrWhiteSpace(namedConnectionString) ||
               !string.IsNullOrWhiteSpace(_options.SourceConnectionString);
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
            throw new InvalidOperationException("Unable to acquire address sync lock.");
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
            logger.LogWarning(exception, "Failed to release address sync lock.");
        }
    }

    private static DateTimeOffset? ToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }
}
