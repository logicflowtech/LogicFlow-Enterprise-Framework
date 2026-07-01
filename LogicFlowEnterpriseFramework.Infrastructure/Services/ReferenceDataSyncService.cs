using System.Data;
using System.Text.RegularExpressions;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class ReferenceDataSyncService(
    IConfiguration configuration,
    IOptions<ReferenceDataSyncOptions> options,
    ILogger<ReferenceDataSyncService> logger) : IReferenceDataSyncService
{
    private const string LookupTitlesKey = "lookup-titles";
    private const string LookupIdentificationTypesKey = "lookup-identification-types";
    private const string LookupCountriesKey = "lookup-countries";
    private const string LookupStatesKey = "lookup-states";
    private const string LookupCitiesKey = "lookup-cities";
    private static readonly Regex ObjectNamePattern = new("^[A-Za-z0-9_\\.\\[\\]]+$", RegexOptions.Compiled);
    private readonly ReferenceDataSyncOptions _options = options.Value;

    public async Task<IReadOnlyList<SyncJobSummaryResponse>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);

        var syncKeys = new[]
        {
            LookupTitlesKey,
            LookupIdentificationTypesKey,
            LookupCountriesKey,
            LookupStatesKey,
            LookupCitiesKey
        };

        foreach (var syncKey in syncKeys)
        {
            await SyncStateStore.EnsureRowAsync(localConnection, syncKey, _options.CommandTimeoutSeconds, cancellationToken);
        }

        var results = new List<SyncJobSummaryResponse>(syncKeys.Length);
        foreach (var syncKey in syncKeys)
        {
            results.Add(await BuildSummaryAsync(localConnection, syncKey, cancellationToken));
        }

        return results;
    }

    public async Task<SyncJobSummaryResponse> RunAsync(string syncKey, CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await SyncStateStore.EnsureRowAsync(localConnection, syncKey, _options.CommandTimeoutSeconds, cancellationToken);
        await AcquireAppLockAsync(localConnection, syncKey, cancellationToken);

        try
        {
            switch (syncKey)
            {
                case LookupTitlesKey:
                    await RunTitleSyncAsync(localConnection, cancellationToken);
                    break;
                case LookupIdentificationTypesKey:
                    await RunIdentificationTypeSyncAsync(localConnection, cancellationToken);
                    break;
                case LookupCountriesKey:
                    await RunCountrySyncAsync(localConnection, cancellationToken);
                    break;
                case LookupStatesKey:
                    await RunStateSyncAsync(localConnection, cancellationToken);
                    break;
                case LookupCitiesKey:
                    await RunCitySyncAsync(localConnection, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Sync job '{syncKey}' is not configured.");
            }

            return await BuildSummaryAsync(localConnection, syncKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reference data sync failed for {SyncKey}.", syncKey);
            await SyncStateStore.MarkFailedAsync(localConnection, syncKey, _options.CommandTimeoutSeconds, cancellationToken, exception.Message);
            throw;
        }
        finally
        {
            await ReleaseAppLockAsync(localConnection, syncKey, cancellationToken);
        }
    }

    private async Task RunTitleSyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await SyncStateStore.MarkStartedAsync(localConnection, LookupTitlesKey, _options.CommandTimeoutSeconds, cancellationToken, "Running title sync");
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(LookupTitlesKey), cancellationToken);
        var sourceRows = await LoadTitleRowsAsync(cancellationToken);
        await UpsertTitlesAsync(localConnection, sourceRows, cancellationToken);
        await SyncStateStore.MarkCompletedAsync(
            localConnection,
            LookupTitlesKey,
            _options.CommandTimeoutSeconds,
            cancellationToken,
            sourceRows.Rows.Count,
            $"Synced {sourceRows.Rows.Count} title rows.");
    }

    private async Task RunIdentificationTypeSyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await SyncStateStore.MarkStartedAsync(localConnection, LookupIdentificationTypesKey, _options.CommandTimeoutSeconds, cancellationToken, "Running identification type sync");
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(LookupIdentificationTypesKey), cancellationToken);
        var sourceRows = await LoadIdentificationTypeRowsAsync(cancellationToken);
        await UpsertIdentificationTypesAsync(localConnection, sourceRows, cancellationToken);
        await SyncStateStore.MarkCompletedAsync(
            localConnection,
            LookupIdentificationTypesKey,
            _options.CommandTimeoutSeconds,
            cancellationToken,
            sourceRows.Rows.Count,
            $"Synced {sourceRows.Rows.Count} identification type rows.");
    }

    private async Task RunCountrySyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await SyncStateStore.MarkStartedAsync(localConnection, LookupCountriesKey, _options.CommandTimeoutSeconds, cancellationToken, "Running country lookup sync");
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(LookupCountriesKey), cancellationToken);
        var sourceRows = await LoadCountryRowsAsync(cancellationToken);
        await UpsertCountriesAsync(localConnection, sourceRows, cancellationToken);

        var fallbackCount = sourceRows.AsEnumerable().Count(row => row.Field<bool>("UsesFallbackName"));
        var message = fallbackCount == 0
            ? $"Synced {sourceRows.Rows.Count} country rows."
            : $"Synced {sourceRows.Rows.Count} country rows. {fallbackCount} rows used fallback names because no country mapping was configured.";

        await SyncStateStore.MarkCompletedAsync(
            localConnection,
            LookupCountriesKey,
            _options.CommandTimeoutSeconds,
            cancellationToken,
            sourceRows.Rows.Count,
            message);
    }

    private async Task RunStateSyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await SyncStateStore.MarkStartedAsync(localConnection, LookupStatesKey, _options.CommandTimeoutSeconds, cancellationToken, "Running state lookup sync");
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(LookupStatesKey), cancellationToken);
        var sourceRows = await LoadStateRowsAsync(cancellationToken);
        await UpsertStatesAsync(localConnection, sourceRows, cancellationToken);
        await SyncStateStore.MarkCompletedAsync(
            localConnection,
            LookupStatesKey,
            _options.CommandTimeoutSeconds,
            cancellationToken,
            sourceRows.Rows.Count,
            $"Synced {sourceRows.Rows.Count} state rows.");
    }

    private async Task RunCitySyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await SyncStateStore.MarkStartedAsync(localConnection, LookupCitiesKey, _options.CommandTimeoutSeconds, cancellationToken, "Running city lookup sync");
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(LookupCitiesKey), cancellationToken);
        var sourceRows = await LoadCityRowsAsync(cancellationToken);
        await UpsertCitiesAsync(localConnection, sourceRows, cancellationToken);
        await SyncStateStore.MarkCompletedAsync(
            localConnection,
            LookupCitiesKey,
            _options.CommandTimeoutSeconds,
            cancellationToken,
            sourceRows.Rows.Count,
            $"Synced {sourceRows.Rows.Count} city rows.");
    }

    private async Task<SyncJobSummaryResponse> BuildSummaryAsync(SqlConnection localConnection, string syncKey, CancellationToken cancellationToken)
    {
        var definition = GetDefinition(syncKey);
        var state = await SyncStateStore.ReadAsync(localConnection, syncKey, _options.CommandTimeoutSeconds, cancellationToken);
        var localRowCount = await GetLocalRowCountAsync(localConnection, definition.TargetTableName, cancellationToken);

        return new SyncJobSummaryResponse(
            syncKey,
            definition.Name,
            definition.Description,
            definition.SourceObjectName,
            definition.TargetTableName,
            false,
            0,
            _options.BatchSize,
            false,
            _options.SourceConnectionStringName,
            HasConfiguredSourceConnection(),
            localRowCount,
            ToUtc(state.LastStartedAt),
            ToUtc(state.LastCompletedAt),
            state.LastRunSucceeded,
            state.LastProcessedRows,
            state.LastRunMessage,
            string.Empty);
    }

    private async Task<long> GetLocalRowCountAsync(SqlConnection localConnection, string targetTableName, CancellationToken cancellationToken)
    {
        await using var command = localConnection.CreateCommand();
        command.CommandText = $"SELECT COUNT_BIG(*) FROM {targetTableName};";
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<DataTable> LoadTitleRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS INT) AS MigratedId,
                COALESCE(NULLIF(LTRIM(RTRIM(src.LABEL)), N''), CONCAT(N'Legacy Title ', CAST(src.ID AS NVARCHAR(20)))) AS Name,
                CAST(COALESCE(src.[ORDER], 0) AS INT) AS DisplayOrder
            FROM {ResolveSourceObjectName(LookupTitlesKey)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(query, cancellationToken);
    }

    private async Task<DataTable> LoadIdentificationTypeRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS INT) AS MigratedId,
                COALESCE(NULLIF(LTRIM(RTRIM(src.LABEL)), N''), CONCAT(N'Legacy Identification Type ', CAST(src.ID AS NVARCHAR(20)))) AS Name,
                CAST(COALESCE(src.[ORDER], 0) AS INT) AS DisplayOrder
            FROM {ResolveSourceObjectName(LookupIdentificationTypesKey)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(query, cancellationToken);
    }

    private async Task<DataTable> LoadCountryRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT DISTINCT
                CAST(src.COUNTRYID AS BIGINT) AS MigratedId
            FROM {ResolveSourceObjectName(LookupCountriesKey)} AS src
            WHERE src.COUNTRYID IS NOT NULL
            ORDER BY CAST(src.COUNTRYID AS BIGINT);
            """;

        var rawRows = await LoadSourceRowsAsync(query, cancellationToken);
        var table = new DataTable();
        table.Columns.Add("MigratedId", typeof(long));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Code", typeof(string));
        table.Columns.Add("DisplayOrder", typeof(int));
        table.Columns.Add("IsActive", typeof(bool));
        table.Columns.Add("UsesFallbackName", typeof(bool));

        var mappings = _options.CountryMappings.ToDictionary(item => item.MigratedId);
        foreach (DataRow rawRow in rawRows.Rows)
        {
            var migratedId = Convert.ToInt64(rawRow["MigratedId"]);
            if (mappings.TryGetValue(migratedId, out var mapping))
            {
                table.Rows.Add(migratedId, string.IsNullOrWhiteSpace(mapping.Name) ? $"Legacy Country {migratedId}" : mapping.Name, (object?)mapping.Code ?? DBNull.Value, mapping.DisplayOrder, mapping.IsActive, false);
                continue;
            }

            table.Rows.Add(migratedId, $"Legacy Country {migratedId}", DBNull.Value, 0, true, true);
        }

        return table;
    }

    private async Task<DataTable> LoadStateRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.STATEID AS BIGINT) AS MigratedId,
                CAST(MAX(CAST(src.COUNTRYID AS BIGINT)) AS BIGINT) AS LegacyCountryId,
                COALESCE(MAX(NULLIF(LTRIM(RTRIM(src.STATENAME)), N'')), CONCAT(N'Legacy State ', CAST(src.STATEID AS NVARCHAR(20)))) AS Name
            FROM {ResolveSourceObjectName(LookupStatesKey)} AS src
            WHERE src.STATEID IS NOT NULL
            GROUP BY CAST(src.STATEID AS BIGINT)
            ORDER BY CAST(src.STATEID AS BIGINT);
            """;

        return await LoadSourceRowsAsync(query, cancellationToken);
    }

    private async Task<DataTable> LoadCityRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.CITYID AS BIGINT) AS MigratedId,
                CAST(MAX(CAST(src.COUNTRYID AS BIGINT)) AS BIGINT) AS LegacyCountryId,
                CAST(MAX(CAST(src.STATEID AS BIGINT)) AS BIGINT) AS LegacyStateId,
                COALESCE(MAX(NULLIF(LTRIM(RTRIM(src.CITYNAME)), N'')), CONCAT(N'Legacy City ', CAST(src.CITYID AS NVARCHAR(20)))) AS Name
            FROM {ResolveSourceObjectName(LookupCitiesKey)} AS src
            WHERE src.CITYID IS NOT NULL
            GROUP BY CAST(src.CITYID AS BIGINT)
            ORDER BY CAST(src.CITYID AS BIGINT);
            """;

        return await LoadSourceRowsAsync(query, cancellationToken);
    }

    private async Task<DataTable> LoadSourceRowsAsync(string query, CancellationToken cancellationToken)
    {
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

    private async Task UpsertTitlesAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#LookupTitleSyncBatch') IS NOT NULL
            BEGIN
                DROP TABLE #LookupTitleSyncBatch;
            END;

            CREATE TABLE #LookupTitleSyncBatch
            (
                MigratedId INT NOT NULL,
                Name NVARCHAR(50) NULL,
                DisplayOrder INT NOT NULL
            );
            """;

        await CreateTempTableAsync(localConnection, createTempTableSql, cancellationToken);
        await BulkCopyAsync(localConnection, "#LookupTitleSyncBatch", sourceRows, cancellationToken);

        const string mergeSql = """
            MERGE dbo.LookupTitles AS target
            USING #LookupTitleSyncBatch AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    Name = source.Name,
                    NameBm = NULL,
                    DisplayOrder = source.DisplayOrder,
                    IsActive = 1,
                    LastSyncedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (MigratedId, Name, NameBm, DisplayOrder, IsActive, LastSyncedAt)
                VALUES (source.MigratedId, source.Name, NULL, source.DisplayOrder, 1, SYSUTCDATETIME())
            WHEN NOT MATCHED BY SOURCE AND target.MigratedId IS NOT NULL THEN
                DELETE;
            """;

        await ExecuteNonQueryAsync(localConnection, mergeSql, cancellationToken);
    }

    private async Task UpsertIdentificationTypesAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#LookupIdentificationTypeSyncBatch') IS NOT NULL
            BEGIN
                DROP TABLE #LookupIdentificationTypeSyncBatch;
            END;

            CREATE TABLE #LookupIdentificationTypeSyncBatch
            (
                MigratedId INT NOT NULL,
                Name NVARCHAR(50) NULL,
                DisplayOrder INT NOT NULL
            );
            """;

        await CreateTempTableAsync(localConnection, createTempTableSql, cancellationToken);
        await BulkCopyAsync(localConnection, "#LookupIdentificationTypeSyncBatch", sourceRows, cancellationToken);

        const string mergeSql = """
            MERGE dbo.LookupIdentificationTypes AS target
            USING #LookupIdentificationTypeSyncBatch AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    Name = source.Name,
                    DisplayOrder = source.DisplayOrder,
                    IsActive = 1,
                    LastSyncedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (MigratedId, Name, DisplayOrder, IsActive, LastSyncedAt)
                VALUES (source.MigratedId, source.Name, source.DisplayOrder, 1, SYSUTCDATETIME())
            WHEN NOT MATCHED BY SOURCE AND target.MigratedId IS NOT NULL THEN
                DELETE;
            """;

        await ExecuteNonQueryAsync(localConnection, mergeSql, cancellationToken);
    }

    private async Task UpsertCountriesAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#LookupCountrySyncBatch') IS NOT NULL
            BEGIN
                DROP TABLE #LookupCountrySyncBatch;
            END;

            CREATE TABLE #LookupCountrySyncBatch
            (
                MigratedId BIGINT NOT NULL,
                Name NVARCHAR(200) NOT NULL,
                Code NVARCHAR(50) NULL,
                DisplayOrder INT NOT NULL,
                IsActive BIT NOT NULL,
                UsesFallbackName BIT NOT NULL
            );
            """;

        await CreateTempTableAsync(localConnection, createTempTableSql, cancellationToken);
        await BulkCopyAsync(localConnection, "#LookupCountrySyncBatch", sourceRows, cancellationToken);

        const string mergeSql = """
            MERGE dbo.LookupCountries AS target
            USING #LookupCountrySyncBatch AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    Name = source.Name,
                    Code = source.Code,
                    DisplayOrder = source.DisplayOrder,
                    IsActive = source.IsActive,
                    LastSyncedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (MigratedId, Name, Code, DisplayOrder, IsActive, LastSyncedAt)
                VALUES (source.MigratedId, source.Name, source.Code, source.DisplayOrder, source.IsActive, SYSUTCDATETIME())
            WHEN NOT MATCHED BY SOURCE AND target.MigratedId IS NOT NULL THEN
                DELETE;
            """;

        await ExecuteNonQueryAsync(localConnection, mergeSql, cancellationToken);
    }

    private async Task UpsertStatesAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#LookupStateSyncBatch') IS NOT NULL
            BEGIN
                DROP TABLE #LookupStateSyncBatch;
            END;

            CREATE TABLE #LookupStateSyncBatch
            (
                MigratedId BIGINT NOT NULL,
                LegacyCountryId BIGINT NULL,
                Name NVARCHAR(200) NULL
            );
            """;

        await CreateTempTableAsync(localConnection, createTempTableSql, cancellationToken);
        await BulkCopyAsync(localConnection, "#LookupStateSyncBatch", sourceRows, cancellationToken);

        const string mergeSql = """
            MERGE dbo.LookupStates AS target
            USING
            (
                SELECT
                    source.MigratedId,
                    countries.Id AS CountryId,
                    source.Name
                FROM #LookupStateSyncBatch AS source
                LEFT JOIN dbo.LookupCountries AS countries ON countries.MigratedId = source.LegacyCountryId
            ) AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    CountryId = source.CountryId,
                    Name = source.Name,
                    Code = NULL,
                    DisplayOrder = 0,
                    IsActive = 1,
                    LastSyncedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (MigratedId, CountryId, Name, Code, DisplayOrder, IsActive, LastSyncedAt)
                VALUES (source.MigratedId, source.CountryId, source.Name, NULL, 0, 1, SYSUTCDATETIME())
            WHEN NOT MATCHED BY SOURCE AND target.MigratedId IS NOT NULL THEN
                DELETE;
            """;

        await ExecuteNonQueryAsync(localConnection, mergeSql, cancellationToken);
    }

    private async Task UpsertCitiesAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#LookupCitySyncBatch') IS NOT NULL
            BEGIN
                DROP TABLE #LookupCitySyncBatch;
            END;

            CREATE TABLE #LookupCitySyncBatch
            (
                MigratedId BIGINT NOT NULL,
                LegacyCountryId BIGINT NULL,
                LegacyStateId BIGINT NULL,
                Name NVARCHAR(200) NULL
            );
            """;

        await CreateTempTableAsync(localConnection, createTempTableSql, cancellationToken);
        await BulkCopyAsync(localConnection, "#LookupCitySyncBatch", sourceRows, cancellationToken);

        const string mergeSql = """
            MERGE dbo.LookupCities AS target
            USING
            (
                SELECT
                    source.MigratedId,
                    countries.Id AS CountryId,
                    states.Id AS StateId,
                    source.Name
                FROM #LookupCitySyncBatch AS source
                LEFT JOIN dbo.LookupCountries AS countries ON countries.MigratedId = source.LegacyCountryId
                LEFT JOIN dbo.LookupStates AS states ON states.MigratedId = source.LegacyStateId
            ) AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    CountryId = source.CountryId,
                    StateId = source.StateId,
                    Name = source.Name,
                    Code = NULL,
                    DisplayOrder = 0,
                    IsActive = 1,
                    LastSyncedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (MigratedId, CountryId, StateId, Name, Code, DisplayOrder, IsActive, LastSyncedAt)
                VALUES (source.MigratedId, source.CountryId, source.StateId, source.Name, NULL, 0, 1, SYSUTCDATETIME())
            WHEN NOT MATCHED BY SOURCE AND target.MigratedId IS NOT NULL THEN
                DELETE;
            """;

        await ExecuteNonQueryAsync(localConnection, mergeSql, cancellationToken);
    }

    private async Task CreateTempTableAsync(SqlConnection localConnection, string sql, CancellationToken cancellationToken)
    {
        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task BulkCopyAsync(SqlConnection localConnection, string tempTableName, DataTable sourceRows, CancellationToken cancellationToken)
    {
        using var bulkCopy = new SqlBulkCopy(localConnection);
        bulkCopy.DestinationTableName = tempTableName;
        bulkCopy.BatchSize = _options.BatchSize;
        bulkCopy.BulkCopyTimeout = _options.CommandTimeoutSeconds;

        foreach (DataColumn column in sourceRows.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(sourceRows, cancellationToken);
    }

    private async Task ExecuteNonQueryAsync(SqlConnection localConnection, string sql, CancellationToken cancellationToken)
    {
        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureSourceObjectExistsAsync(string sourceObjectName, CancellationToken cancellationToken)
    {
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
            $"Reference data sync source object {sourceObjectName} was not found in source database '{sourceConnection.Database}'. " +
            $"Check ConnectionStrings:{_options.SourceConnectionStringName} or ReferenceDataSync:SourceConnectionString.");
    }

    private string ResolveSourceObjectName(string syncKey)
    {
        var objectName = syncKey switch
        {
            LookupTitlesKey => _options.TitleSourceObjectName,
            LookupIdentificationTypesKey => _options.IdentificationTypeSourceObjectName,
            LookupCountriesKey or LookupStatesKey or LookupCitiesKey => _options.GeoSourceObjectName,
            _ => throw new InvalidOperationException($"Sync job '{syncKey}' is not configured.")
        };

        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new InvalidOperationException($"Source object name for sync job '{syncKey}' is not configured.");
        }

        if (!ObjectNamePattern.IsMatch(objectName))
        {
            throw new InvalidOperationException($"Source object name for sync job '{syncKey}' contains unsupported characters.");
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

        throw new InvalidOperationException("Reference data source connection string is not configured.");
    }

    private bool HasConfiguredSourceConnection()
    {
        var namedConnectionString = string.IsNullOrWhiteSpace(_options.SourceConnectionStringName)
            ? null
            : configuration.GetConnectionString(_options.SourceConnectionStringName);

        return !string.IsNullOrWhiteSpace(namedConnectionString) ||
               !string.IsNullOrWhiteSpace(_options.SourceConnectionString);
    }

    private async Task AcquireAppLockAsync(SqlConnection localConnection, string syncKey, CancellationToken cancellationToken)
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
        command.Parameters.AddWithValue("@Resource", $"ReferenceDataSync:{syncKey}");

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
        {
            throw new InvalidOperationException($"Unable to acquire sync lock for '{syncKey}'.");
        }
    }

    private async Task ReleaseAppLockAsync(SqlConnection localConnection, string syncKey, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = localConnection.CreateCommand();
            command.CommandText = "EXEC sp_releaseapplock @Resource = @Resource, @LockOwner = 'Session';";
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.AddWithValue("@Resource", $"ReferenceDataSync:{syncKey}");
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to release reference data sync lock for {SyncKey}.", syncKey);
        }
    }

    private SyncJobDefinition GetDefinition(string syncKey)
    {
        return syncKey switch
        {
            LookupTitlesKey => new SyncJobDefinition(
                "Lookup Title Sync",
                "Syncs title master data into the local lookup cache.",
                ResolveSourceObjectName(syncKey),
                "[dbo].[LookupTitles]"),
            LookupIdentificationTypesKey => new SyncJobDefinition(
                "Lookup Identification Type Sync",
                "Syncs identification type master data into the local lookup cache.",
                ResolveSourceObjectName(syncKey),
                "[dbo].[LookupIdentificationTypes]"),
            LookupCountriesKey => new SyncJobDefinition(
                "Lookup Country Sync",
                "Builds country lookup keys used by address mapping.",
                ResolveSourceObjectName(syncKey),
                "[dbo].[LookupCountries]"),
            LookupStatesKey => new SyncJobDefinition(
                "Lookup State Sync",
                "Builds state lookup rows from legacy address records.",
                ResolveSourceObjectName(syncKey),
                "[dbo].[LookupStates]"),
            LookupCitiesKey => new SyncJobDefinition(
                "Lookup City Sync",
                "Builds city lookup rows from legacy address records.",
                ResolveSourceObjectName(syncKey),
                "[dbo].[LookupCities]"),
            _ => throw new InvalidOperationException($"Sync job '{syncKey}' is not configured.")
        };
    }

    private static DateTimeOffset? ToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }

    private sealed record SyncJobDefinition(
        string Name,
        string Description,
        string SourceObjectName,
        string TargetTableName);
}
