using System.Data;
using System.Text.RegularExpressions;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class ApplicationLookupSyncService(
    IConfiguration configuration,
    IOptions<ApplicationLookupSyncOptions> options,
    ILogger<ApplicationLookupSyncService> logger) : IApplicationLookupSyncService
{
    private const string SourceSystem = "outsystems";
    private const string ApplicationCategoriesKey = "application-categories";
    private const string ApplicationForsKey = "application-fors";
    private const string ApplicationTypesKey = "application-types";
    private const string ApplicationStatusesKey = "application-statuses";
    private const string ApplicationCategoryForsKey = "application-category-fors";
    private const string ApplicationForTypesKey = "application-for-types";
    private static readonly Regex ObjectNamePattern = new("^[A-Za-z0-9_\\.\\[\\]]+$", RegexOptions.Compiled);
    private readonly ApplicationLookupSyncOptions _options = options.Value;

    public async Task<IReadOnlyList<SyncJobSummaryResponse>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureTargetTablesAsync(localConnection, cancellationToken);

        var syncKeys = GetSyncKeys();
        foreach (var syncKey in syncKeys)
        {
            await EnsureStateRowAsync(localConnection, syncKey, cancellationToken);
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
        await EnsureTargetTablesAsync(localConnection, cancellationToken);
        await EnsureStateRowAsync(localConnection, syncKey, cancellationToken);
        await AcquireAppLockAsync(localConnection, syncKey, cancellationToken);

        try
        {
            switch (syncKey)
            {
                case ApplicationCategoriesKey:
                    await RunApplicationCategorySyncAsync(localConnection, cancellationToken);
                    break;
                case ApplicationForsKey:
                    await RunApplicationForSyncAsync(localConnection, cancellationToken);
                    break;
                case ApplicationTypesKey:
                    await RunApplicationTypeSyncAsync(localConnection, cancellationToken);
                    break;
                case ApplicationStatusesKey:
                    await RunApplicationStatusSyncAsync(localConnection, cancellationToken);
                    break;
                case ApplicationCategoryForsKey:
                    await RunApplicationCategoryForSyncAsync(localConnection, cancellationToken);
                    break;
                case ApplicationForTypesKey:
                    await RunApplicationForTypeSyncAsync(localConnection, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Application lookup sync job '{syncKey}' is not configured.");
            }

            return await BuildSummaryAsync(localConnection, syncKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Application lookup sync failed for {SyncKey}.", syncKey);
            await MarkFailedAsync(localConnection, syncKey, exception.Message, cancellationToken);
            throw;
        }
        finally
        {
            await ReleaseAppLockAsync(localConnection, syncKey, cancellationToken);
        }
    }

    private static string[] GetSyncKeys() =>
    [
        ApplicationCategoriesKey,
        ApplicationForsKey,
        ApplicationTypesKey,
        ApplicationStatusesKey,
        ApplicationCategoryForsKey,
        ApplicationForTypesKey
    ];

    private async Task RunApplicationCategorySyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await MarkStartedAsync(localConnection, ApplicationCategoriesKey, "Running application category sync", cancellationToken);
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(ApplicationCategoriesKey), cancellationToken);
        var sourceRows = await LoadApplicationCategoryRowsAsync(cancellationToken);
        await UpsertApplicationCategoriesAsync(localConnection, sourceRows, cancellationToken);
        await MarkCompletedAsync(localConnection, ApplicationCategoriesKey, sourceRows.Rows.Count, $"Synced {sourceRows.Rows.Count} application category rows.", cancellationToken);
    }

    private async Task RunApplicationForSyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await MarkStartedAsync(localConnection, ApplicationForsKey, "Running application for sync", cancellationToken);
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(ApplicationForsKey), cancellationToken);
        var sourceRows = await LoadApplicationForRowsAsync(cancellationToken);
        await UpsertApplicationForsAsync(localConnection, sourceRows, cancellationToken);
        await MarkCompletedAsync(localConnection, ApplicationForsKey, sourceRows.Rows.Count, $"Synced {sourceRows.Rows.Count} application for rows.", cancellationToken);
    }

    private async Task RunApplicationTypeSyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await MarkStartedAsync(localConnection, ApplicationTypesKey, "Running application type sync", cancellationToken);
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(ApplicationTypesKey), cancellationToken);
        var sourceRows = await LoadApplicationTypeRowsAsync(cancellationToken);
        await UpsertApplicationTypesAsync(localConnection, sourceRows, cancellationToken);
        await MarkCompletedAsync(localConnection, ApplicationTypesKey, sourceRows.Rows.Count, $"Synced {sourceRows.Rows.Count} application type rows.", cancellationToken);
    }

    private async Task RunApplicationStatusSyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await MarkStartedAsync(localConnection, ApplicationStatusesKey, "Running application status sync", cancellationToken);
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(ApplicationStatusesKey), cancellationToken);
        var sourceRows = await LoadApplicationStatusRowsAsync(cancellationToken);
        await UpsertApplicationStatusesAsync(localConnection, sourceRows, cancellationToken);
        await MarkCompletedAsync(localConnection, ApplicationStatusesKey, sourceRows.Rows.Count, $"Synced {sourceRows.Rows.Count} application status rows.", cancellationToken);
    }

    private async Task RunApplicationCategoryForSyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await MarkStartedAsync(localConnection, ApplicationCategoryForsKey, "Running application category/for mapping sync", cancellationToken);
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(ApplicationCategoryForsKey), cancellationToken);
        var sourceRows = await LoadApplicationCategoryForRowsAsync(cancellationToken);
        await UpsertApplicationCategoryForsAsync(localConnection, sourceRows, cancellationToken);
        await MarkCompletedAsync(localConnection, ApplicationCategoryForsKey, sourceRows.Rows.Count, $"Synced {sourceRows.Rows.Count} application category/for mapping rows.", cancellationToken);
    }

    private async Task RunApplicationForTypeSyncAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await MarkStartedAsync(localConnection, ApplicationForTypesKey, "Running application for/type mapping sync", cancellationToken);
        await EnsureSourceObjectExistsAsync(ResolveSourceObjectName(ApplicationForTypesKey), cancellationToken);
        var sourceRows = await LoadApplicationForTypeRowsAsync(cancellationToken);
        await UpsertApplicationForTypesAsync(localConnection, sourceRows, cancellationToken);
        await MarkCompletedAsync(localConnection, ApplicationForTypesKey, sourceRows.Rows.Count, $"Synced {sourceRows.Rows.Count} application for/type mapping rows.", cancellationToken);
    }

    private async Task<SyncJobSummaryResponse> BuildSummaryAsync(SqlConnection localConnection, string syncKey, CancellationToken cancellationToken)
    {
        var definition = GetDefinition(syncKey);
        var state = await ReadStateAsync(localConnection, syncKey, cancellationToken);
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
        command.CommandText = $"SELECT COUNT_BIG(*) FROM {targetTableName} WHERE 1 = 1{(targetTableName.Contains("SyncStates", StringComparison.OrdinalIgnoreCase) ? string.Empty : " AND IsDeleted = 0")};";
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<DataTable> LoadApplicationCategoryRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS INT) AS LegacyId,
                NULLIF(LTRIM(RTRIM(src.LABEL)), N'') AS Name,
                NULLIF(LTRIM(RTRIM(src.CODE)), N'') AS Code,
                NULLIF(LTRIM(RTRIM(src.CODEKEY)), N'') AS CodeKey,
                CAST(src.CATEGORYNUMBER AS INT) AS CategoryNumber,
                CAST(src.[ORDER] AS INT) AS SortOrder,
                CAST(COALESCE(src.IS_ACTIVE, 0) AS BIT) AS IsActive
            FROM {ResolveSourceObjectName(ApplicationCategoriesKey)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(query, cancellationToken);
    }

    private async Task<DataTable> LoadApplicationForRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS INT) AS LegacyId,
                CAST(src.APPLICATIONCATEGORYID AS INT) AS LegacyApplicationCategoryId,
                NULLIF(LTRIM(RTRIM(src.LABEL)), N'') AS Name,
                NULLIF(LTRIM(RTRIM(src.LABELBM)), N'') AS NameBahasa,
                NULLIF(LTRIM(RTRIM(src.DESCRIPTION)), N'') AS Description,
                CAST(src.[ORDER] AS INT) AS SortOrder,
                CAST(COALESCE(src.IS_ACTIVE, 0) AS BIT) AS IsActive
            FROM {ResolveSourceObjectName(ApplicationForsKey)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(query, cancellationToken);
    }

    private async Task<DataTable> LoadApplicationTypeRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS INT) AS LegacyId,
                NULLIF(LTRIM(RTRIM(src.LABEL)), N'') AS Name,
                NULLIF(LTRIM(RTRIM(src.LABELBAHASA)), N'') AS NameBahasa,
                CAST(src.[ORDER] AS INT) AS SortOrder,
                CAST(COALESCE(src.IS_ACTIVE, 0) AS BIT) AS IsActive
            FROM {ResolveSourceObjectName(ApplicationTypesKey)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(query, cancellationToken);
    }

    private async Task<DataTable> LoadApplicationStatusRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS INT) AS LegacyId,
                NULLIF(LTRIM(RTRIM(src.LABEL)), N'') AS Name,
                NULLIF(LTRIM(RTRIM(src.CODEKEY)), N'') AS CodeKey,
                CAST(src.[ORDER] AS INT) AS SortOrder,
                CAST(COALESCE(src.ISACTIVE, 0) AS BIT) AS IsActive,
                CAST(src.APPLICATIONSTATUSMAINTYPEID AS INT) AS LegacyMainTypeId,
                CAST(src.APPLICATIONSTATUSAPPLICANTID AS INT) AS LegacyApplicantStatusId,
                CAST(src.APPLICATIONSTATUSCUSTOMID AS INT) AS LegacyCustomStatusId
            FROM {ResolveSourceObjectName(ApplicationStatusesKey)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(query, cancellationToken);
    }

    private async Task<DataTable> LoadApplicationCategoryForRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS BIGINT) AS LegacyId,
                CAST(src.APPLICATIONCATEGORYID AS INT) AS LegacyApplicationCategoryId,
                CAST(src.APPLICATIONFORID AS INT) AS LegacyApplicationForId,
                CAST(src.CREATEDBYID AS INT) AS SourceCreatedByLegacyUserId,
                src.CREATEDDATETIME AS SourceCreatedAt,
                CAST(src.MODIFIEDBYID AS INT) AS SourceUpdatedByLegacyUserId,
                src.MODIFIEDDATETIME AS SourceUpdatedAt
            FROM {ResolveSourceObjectName(ApplicationCategoryForsKey)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(query, cancellationToken);
    }

    private async Task<DataTable> LoadApplicationForTypeRowsAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS INT) AS LegacyId,
                CAST(src.APPLICATIONFORID AS INT) AS LegacyApplicationForId,
                CAST(src.APPLICATIONTYPEID AS INT) AS LegacyApplicationTypeId,
                CAST(src.APPLICATIONFOREXEMPTIONTYPEI AS INT) AS LegacyApplicationForExemptionTypeId,
                CAST(src.CREATEDBYID AS INT) AS SourceCreatedByLegacyUserId,
                src.CREATEDDATETIME AS SourceCreatedAt,
                CAST(src.MODIFIEDBYID AS INT) AS SourceUpdatedByLegacyUserId,
                src.MODIFIEDDATETIME AS SourceUpdatedAt
            FROM {ResolveSourceObjectName(ApplicationForTypesKey)} AS src
            ORDER BY src.ID;
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

    private async Task UpsertApplicationCategoriesAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#ApplicationCategorySyncBatch') IS NOT NULL DROP TABLE #ApplicationCategorySyncBatch;
            CREATE TABLE #ApplicationCategorySyncBatch
            (
                LegacyId INT NOT NULL,
                Name NVARCHAR(200) NULL,
                Code NVARCHAR(100) NULL,
                CodeKey NVARCHAR(100) NULL,
                CategoryNumber INT NULL,
                SortOrder INT NULL,
                IsActive BIT NOT NULL
            );
            """;

        const string mergeSql = """
            MERGE dbo.ApplicationCategories AS target
            USING #ApplicationCategorySyncBatch AS source
                ON target.LegacyId = source.LegacyId
            WHEN MATCHED THEN
                UPDATE SET
                    Name = source.Name,
                    Code = source.Code,
                    CodeKey = source.CodeKey,
                    CategoryNumber = source.CategoryNumber,
                    SortOrder = source.SortOrder,
                    IsActive = source.IsActive,
                    LastSyncedAt = SYSUTCDATETIME(),
                    IsDeleted = 0,
                    DeletedAt = NULL
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (LegacyId, Name, Code, CodeKey, CategoryNumber, SortOrder, IsActive, LastSyncedAt, CreatedAt, IsDeleted)
                VALUES (source.LegacyId, source.Name, source.Code, source.CodeKey, source.CategoryNumber, source.SortOrder, source.IsActive, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
            WHEN NOT MATCHED BY SOURCE THEN
                UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();
            """;

        await UpsertSimpleAsync(localConnection, createTempTableSql, "#ApplicationCategorySyncBatch", sourceRows, mergeSql, cancellationToken);
    }

    private async Task UpsertApplicationForsAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#ApplicationForSyncBatch') IS NOT NULL DROP TABLE #ApplicationForSyncBatch;
            CREATE TABLE #ApplicationForSyncBatch
            (
                LegacyId INT NOT NULL,
                LegacyApplicationCategoryId INT NULL,
                Name NVARCHAR(500) NULL,
                NameBahasa NVARCHAR(500) NULL,
                Description NVARCHAR(1000) NULL,
                SortOrder INT NULL,
                IsActive BIT NOT NULL
            );
            """;

        const string mergeSql = """
            MERGE dbo.ApplicationFors AS target
            USING
            (
                SELECT
                    source.LegacyId,
                    source.LegacyApplicationCategoryId,
                    categories.Id AS ApplicationCategoryId,
                    source.Name,
                    source.NameBahasa,
                    source.Description,
                    source.SortOrder,
                    source.IsActive
                FROM #ApplicationForSyncBatch AS source
                LEFT JOIN dbo.ApplicationCategories AS categories
                    ON categories.LegacyId = source.LegacyApplicationCategoryId
                   AND categories.IsDeleted = 0
            ) AS source
                ON target.LegacyId = source.LegacyId
            WHEN MATCHED THEN
                UPDATE SET
                    LegacyApplicationCategoryId = source.LegacyApplicationCategoryId,
                    ApplicationCategoryId = source.ApplicationCategoryId,
                    Name = source.Name,
                    NameBahasa = source.NameBahasa,
                    Description = source.Description,
                    SortOrder = source.SortOrder,
                    IsActive = source.IsActive,
                    LastSyncedAt = SYSUTCDATETIME(),
                    IsDeleted = 0,
                    DeletedAt = NULL
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (LegacyId, LegacyApplicationCategoryId, ApplicationCategoryId, Name, NameBahasa, Description, SortOrder, IsActive, LastSyncedAt, CreatedAt, IsDeleted)
                VALUES (source.LegacyId, source.LegacyApplicationCategoryId, source.ApplicationCategoryId, source.Name, source.NameBahasa, source.Description, source.SortOrder, source.IsActive, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
            WHEN NOT MATCHED BY SOURCE THEN
                UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();
            """;

        await UpsertSimpleAsync(localConnection, createTempTableSql, "#ApplicationForSyncBatch", sourceRows, mergeSql, cancellationToken);
    }

    private async Task UpsertApplicationTypesAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#ApplicationTypeSyncBatch') IS NOT NULL DROP TABLE #ApplicationTypeSyncBatch;
            CREATE TABLE #ApplicationTypeSyncBatch
            (
                LegacyId INT NOT NULL,
                Name NVARCHAR(200) NULL,
                NameBahasa NVARCHAR(500) NULL,
                SortOrder INT NULL,
                IsActive BIT NOT NULL
            );
            """;

        const string mergeSql = """
            MERGE dbo.ApplicationTypes AS target
            USING #ApplicationTypeSyncBatch AS source
                ON target.LegacyId = source.LegacyId
            WHEN MATCHED THEN
                UPDATE SET
                    Name = source.Name,
                    NameBahasa = source.NameBahasa,
                    SortOrder = source.SortOrder,
                    IsActive = source.IsActive,
                    LastSyncedAt = SYSUTCDATETIME(),
                    IsDeleted = 0,
                    DeletedAt = NULL
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (LegacyId, Name, NameBahasa, SortOrder, IsActive, LastSyncedAt, CreatedAt, IsDeleted)
                VALUES (source.LegacyId, source.Name, source.NameBahasa, source.SortOrder, source.IsActive, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
            WHEN NOT MATCHED BY SOURCE THEN
                UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();
            """;

        await UpsertSimpleAsync(localConnection, createTempTableSql, "#ApplicationTypeSyncBatch", sourceRows, mergeSql, cancellationToken);
    }

    private async Task UpsertApplicationStatusesAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#ApplicationStatusSyncBatch') IS NOT NULL DROP TABLE #ApplicationStatusSyncBatch;
            CREATE TABLE #ApplicationStatusSyncBatch
            (
                LegacyId INT NOT NULL,
                Name NVARCHAR(200) NULL,
                CodeKey NVARCHAR(100) NULL,
                SortOrder INT NULL,
                IsActive BIT NOT NULL,
                LegacyMainTypeId INT NULL,
                LegacyApplicantStatusId INT NULL,
                LegacyCustomStatusId INT NULL
            );
            """;

        const string mergeSql = """
            MERGE dbo.ApplicationStatuses AS target
            USING #ApplicationStatusSyncBatch AS source
                ON target.LegacyId = source.LegacyId
            WHEN MATCHED THEN
                UPDATE SET
                    Name = source.Name,
                    CodeKey = source.CodeKey,
                    SortOrder = source.SortOrder,
                    IsActive = source.IsActive,
                    LegacyMainTypeId = source.LegacyMainTypeId,
                    LegacyApplicantStatusId = source.LegacyApplicantStatusId,
                    LegacyCustomStatusId = source.LegacyCustomStatusId,
                    LastSyncedAt = SYSUTCDATETIME(),
                    IsDeleted = 0,
                    DeletedAt = NULL
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (LegacyId, Name, CodeKey, SortOrder, IsActive, LegacyMainTypeId, LegacyApplicantStatusId, LegacyCustomStatusId, LastSyncedAt, CreatedAt, IsDeleted)
                VALUES (source.LegacyId, source.Name, source.CodeKey, source.SortOrder, source.IsActive, source.LegacyMainTypeId, source.LegacyApplicantStatusId, source.LegacyCustomStatusId, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
            WHEN NOT MATCHED BY SOURCE THEN
                UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();
            """;

        await UpsertSimpleAsync(localConnection, createTempTableSql, "#ApplicationStatusSyncBatch", sourceRows, mergeSql, cancellationToken);
    }

    private async Task UpsertApplicationCategoryForsAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#ApplicationCategoryForSyncBatch') IS NOT NULL DROP TABLE #ApplicationCategoryForSyncBatch;
            CREATE TABLE #ApplicationCategoryForSyncBatch
            (
                LegacyId BIGINT NOT NULL,
                LegacyApplicationCategoryId INT NULL,
                LegacyApplicationForId INT NULL,
                SourceCreatedByLegacyUserId INT NULL,
                SourceCreatedAt DATETIME2(3) NULL,
                SourceUpdatedByLegacyUserId INT NULL,
                SourceUpdatedAt DATETIME2(3) NULL
            );
            """;

        const string mergeSql = """
            MERGE dbo.ApplicationCategoryFors AS target
            USING
            (
                SELECT
                    source.LegacyId,
                    source.LegacyApplicationCategoryId,
                    categories.Id AS ApplicationCategoryId,
                    source.LegacyApplicationForId,
                    appFors.Id AS ApplicationForId,
                    source.SourceCreatedByLegacyUserId,
                    source.SourceCreatedAt,
                    source.SourceUpdatedByLegacyUserId,
                    source.SourceUpdatedAt
                FROM #ApplicationCategoryForSyncBatch AS source
                LEFT JOIN dbo.ApplicationCategories AS categories
                    ON categories.LegacyId = source.LegacyApplicationCategoryId
                   AND categories.IsDeleted = 0
                LEFT JOIN dbo.ApplicationFors AS appFors
                    ON appFors.LegacyId = source.LegacyApplicationForId
                   AND appFors.IsDeleted = 0
            ) AS source
                ON target.LegacyId = source.LegacyId
            WHEN MATCHED THEN
                UPDATE SET
                    LegacyApplicationCategoryId = source.LegacyApplicationCategoryId,
                    ApplicationCategoryId = source.ApplicationCategoryId,
                    LegacyApplicationForId = source.LegacyApplicationForId,
                    ApplicationForId = source.ApplicationForId,
                    SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
                    SourceCreatedAt = source.SourceCreatedAt,
                    SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt = source.SourceUpdatedAt,
                    LastSyncedAt = SYSUTCDATETIME(),
                    IsDeleted = 0,
                    DeletedAt = NULL
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (LegacyId, LegacyApplicationCategoryId, ApplicationCategoryId, LegacyApplicationForId, ApplicationForId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
                VALUES (source.LegacyId, source.LegacyApplicationCategoryId, source.ApplicationCategoryId, source.LegacyApplicationForId, source.ApplicationForId, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
            WHEN NOT MATCHED BY SOURCE THEN
                UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();
            """;

        await UpsertSimpleAsync(localConnection, createTempTableSql, "#ApplicationCategoryForSyncBatch", sourceRows, mergeSql, cancellationToken);
    }

    private async Task UpsertApplicationForTypesAsync(SqlConnection localConnection, DataTable sourceRows, CancellationToken cancellationToken)
    {
        const string createTempTableSql = """
            IF OBJECT_ID('tempdb..#ApplicationForTypeSyncBatch') IS NOT NULL DROP TABLE #ApplicationForTypeSyncBatch;
            CREATE TABLE #ApplicationForTypeSyncBatch
            (
                LegacyId INT NOT NULL,
                LegacyApplicationForId INT NULL,
                LegacyApplicationTypeId INT NULL,
                LegacyApplicationForExemptionTypeId INT NULL,
                SourceCreatedByLegacyUserId INT NULL,
                SourceCreatedAt DATETIME2(3) NULL,
                SourceUpdatedByLegacyUserId INT NULL,
                SourceUpdatedAt DATETIME2(3) NULL
            );
            """;

        const string mergeSql = """
            MERGE dbo.ApplicationForTypes AS target
            USING
            (
                SELECT
                    source.LegacyId,
                    source.LegacyApplicationForId,
                    appFors.Id AS ApplicationForId,
                    source.LegacyApplicationTypeId,
                    appTypes.Id AS ApplicationTypeId,
                    source.LegacyApplicationForExemptionTypeId,
                    source.SourceCreatedByLegacyUserId,
                    source.SourceCreatedAt,
                    source.SourceUpdatedByLegacyUserId,
                    source.SourceUpdatedAt
                FROM #ApplicationForTypeSyncBatch AS source
                LEFT JOIN dbo.ApplicationFors AS appFors
                    ON appFors.LegacyId = source.LegacyApplicationForId
                   AND appFors.IsDeleted = 0
                LEFT JOIN dbo.ApplicationTypes AS appTypes
                    ON appTypes.LegacyId = source.LegacyApplicationTypeId
                   AND appTypes.IsDeleted = 0
            ) AS source
                ON target.LegacyId = source.LegacyId
            WHEN MATCHED THEN
                UPDATE SET
                    LegacyApplicationForId = source.LegacyApplicationForId,
                    ApplicationForId = source.ApplicationForId,
                    LegacyApplicationTypeId = source.LegacyApplicationTypeId,
                    ApplicationTypeId = source.ApplicationTypeId,
                    LegacyApplicationForExemptionTypeId = source.LegacyApplicationForExemptionTypeId,
                    SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
                    SourceCreatedAt = source.SourceCreatedAt,
                    SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt = source.SourceUpdatedAt,
                    LastSyncedAt = SYSUTCDATETIME(),
                    IsDeleted = 0,
                    DeletedAt = NULL
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (LegacyId, LegacyApplicationForId, ApplicationForId, LegacyApplicationTypeId, ApplicationTypeId, LegacyApplicationForExemptionTypeId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
                VALUES (source.LegacyId, source.LegacyApplicationForId, source.ApplicationForId, source.LegacyApplicationTypeId, source.ApplicationTypeId, source.LegacyApplicationForExemptionTypeId, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
            WHEN NOT MATCHED BY SOURCE THEN
                UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();
            """;

        await UpsertSimpleAsync(localConnection, createTempTableSql, "#ApplicationForTypeSyncBatch", sourceRows, mergeSql, cancellationToken);
    }

    private async Task UpsertSimpleAsync(
        SqlConnection localConnection,
        string createTempTableSql,
        string tempTableName,
        DataTable sourceRows,
        string mergeSql,
        CancellationToken cancellationToken)
    {
        await CreateTempTableAsync(localConnection, createTempTableSql, cancellationToken);
        await BulkCopyAsync(localConnection, tempTableName, sourceRows, cancellationToken);
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
            $"Application lookup source object {sourceObjectName} was not found in source database '{sourceConnection.Database}'. " +
            $"Check ConnectionStrings:{_options.SourceConnectionStringName} or {ApplicationLookupSyncOptions.SectionName}:SourceConnectionString.");
    }

    private async Task EnsureTargetTablesAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.ApplicationCategories', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ApplicationCategories
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationCategories PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    LegacyId INT NOT NULL,
                    Name NVARCHAR(200) NULL,
                    Code NVARCHAR(100) NULL,
                    CodeKey NVARCHAR(100) NULL,
                    CategoryNumber INT NULL,
                    SortOrder INT NULL,
                    IsActive BIT NOT NULL CONSTRAINT DF_ApplicationCategories_IsActive DEFAULT (1),
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationCategories_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationCategories_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationCategories_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_ApplicationCategories_LegacyId ON dbo.ApplicationCategories (LegacyId);
                CREATE UNIQUE INDEX IX_ApplicationCategories_CodeKey ON dbo.ApplicationCategories (CodeKey) WHERE CodeKey IS NOT NULL;
                CREATE INDEX IX_ApplicationCategories_SortOrder ON dbo.ApplicationCategories (SortOrder, Name);
            END;

            IF OBJECT_ID(N'dbo.ApplicationTypes', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ApplicationTypes
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationTypes PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    LegacyId INT NOT NULL,
                    Name NVARCHAR(200) NULL,
                    NameBahasa NVARCHAR(500) NULL,
                    SortOrder INT NULL,
                    IsActive BIT NOT NULL CONSTRAINT DF_ApplicationTypes_IsActive DEFAULT (1),
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationTypes_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationTypes_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationTypes_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_ApplicationTypes_LegacyId ON dbo.ApplicationTypes (LegacyId);
                CREATE INDEX IX_ApplicationTypes_SortOrder ON dbo.ApplicationTypes (SortOrder, Name);
            END;

            IF OBJECT_ID(N'dbo.ApplicationStatuses', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ApplicationStatuses
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationStatuses PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    LegacyId INT NOT NULL,
                    Name NVARCHAR(200) NULL,
                    CodeKey NVARCHAR(100) NULL,
                    SortOrder INT NULL,
                    IsActive BIT NOT NULL CONSTRAINT DF_ApplicationStatuses_IsActive DEFAULT (1),
                    LegacyMainTypeId INT NULL,
                    LegacyApplicantStatusId INT NULL,
                    LegacyCustomStatusId INT NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationStatuses_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationStatuses_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationStatuses_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_ApplicationStatuses_LegacyId ON dbo.ApplicationStatuses (LegacyId);
                CREATE UNIQUE INDEX IX_ApplicationStatuses_CodeKey ON dbo.ApplicationStatuses (CodeKey) WHERE CodeKey IS NOT NULL;
                CREATE INDEX IX_ApplicationStatuses_SortOrder ON dbo.ApplicationStatuses (SortOrder, Name);
            END;

            IF OBJECT_ID(N'dbo.ApplicationFors', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ApplicationFors
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationFors PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    LegacyId INT NOT NULL,
                    LegacyApplicationCategoryId INT NULL,
                    ApplicationCategoryId UNIQUEIDENTIFIER NULL,
                    Name NVARCHAR(500) NULL,
                    NameBahasa NVARCHAR(500) NULL,
                    Description NVARCHAR(1000) NULL,
                    SortOrder INT NULL,
                    IsActive BIT NOT NULL CONSTRAINT DF_ApplicationFors_IsActive DEFAULT (1),
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationFors_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationFors_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationFors_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_ApplicationFors_LegacyId ON dbo.ApplicationFors (LegacyId);
                CREATE INDEX IX_ApplicationFors_LegacyApplicationCategoryId ON dbo.ApplicationFors (LegacyApplicationCategoryId);
                CREATE INDEX IX_ApplicationFors_ApplicationCategoryId ON dbo.ApplicationFors (ApplicationCategoryId, SortOrder);
            END;

            IF OBJECT_ID(N'dbo.ApplicationForTypes', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ApplicationForTypes
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationForTypes PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    LegacyId INT NOT NULL,
                    ApplicationForId UNIQUEIDENTIFIER NULL,
                    ApplicationTypeId UNIQUEIDENTIFIER NULL,
                    LegacyApplicationForId INT NULL,
                    LegacyApplicationTypeId INT NULL,
                    LegacyApplicationForExemptionTypeId INT NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationForTypes_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationForTypes_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationForTypes_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_ApplicationForTypes_LegacyId ON dbo.ApplicationForTypes (LegacyId);
                CREATE UNIQUE INDEX IX_ApplicationForTypes_ApplicationForId_ApplicationTypeId ON dbo.ApplicationForTypes (ApplicationForId, ApplicationTypeId) WHERE ApplicationForId IS NOT NULL AND ApplicationTypeId IS NOT NULL;
            END;

            IF OBJECT_ID(N'dbo.ApplicationCategoryFors', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ApplicationCategoryFors
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationCategoryFors PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    LegacyId BIGINT NOT NULL,
                    ApplicationCategoryId UNIQUEIDENTIFIER NULL,
                    ApplicationForId UNIQUEIDENTIFIER NULL,
                    LegacyApplicationCategoryId INT NULL,
                    LegacyApplicationForId INT NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationCategoryFors_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationCategoryFors_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationCategoryFors_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_ApplicationCategoryFors_LegacyId ON dbo.ApplicationCategoryFors (LegacyId);
                CREATE UNIQUE INDEX IX_ApplicationCategoryFors_ApplicationCategoryId_ApplicationForId ON dbo.ApplicationCategoryFors (ApplicationCategoryId, ApplicationForId) WHERE ApplicationCategoryId IS NOT NULL AND ApplicationForId IS NOT NULL;
            END;

            IF OBJECT_ID(N'dbo.ApplicationLookupSyncStates', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ApplicationLookupSyncStates
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationLookupSyncStates PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    SourceSystem NVARCHAR(100) NOT NULL,
                    SyncName NVARCHAR(100) NOT NULL,
                    LastStartedAt DATETIME2(3) NULL,
                    LastCompletedAt DATETIME2(3) NULL,
                    LastRunSucceeded BIT NULL,
                    LastProcessedRows INT NOT NULL CONSTRAINT DF_ApplicationLookupSyncStates_LastProcessedRows DEFAULT (0),
                    LastRunMessage NVARCHAR(4000) NULL
                );
                CREATE UNIQUE INDEX IX_ApplicationLookupSyncStates_SourceSystem_SyncName ON dbo.ApplicationLookupSyncStates (SourceSystem, SyncName);
            END;
            """;

        await ExecuteNonQueryAsync(localConnection, sql, cancellationToken);
    }

    private async Task EnsureStateRowAsync(SqlConnection localConnection, string syncKey, CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationLookupSyncStates WHERE SourceSystem = @SourceSystem AND SyncName = @SyncName)
            BEGIN
                INSERT INTO dbo.ApplicationLookupSyncStates (SourceSystem, SyncName, LastRunSucceeded, LastProcessedRows, LastRunMessage)
                VALUES (@SourceSystem, @SyncName, NULL, 0, N'Not started');
            END;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceSystem", SourceSystem);
        command.Parameters.AddWithValue("@SyncName", syncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<(DateTime? LastStartedAt, DateTime? LastCompletedAt, bool? LastRunSucceeded, int LastProcessedRows, string? LastRunMessage)> ReadStateAsync(
        SqlConnection localConnection,
        string syncKey,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT LastStartedAt, LastCompletedAt, LastRunSucceeded, LastProcessedRows, LastRunMessage
            FROM dbo.ApplicationLookupSyncStates
            WHERE SourceSystem = @SourceSystem AND SyncName = @SyncName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceSystem", SourceSystem);
        command.Parameters.AddWithValue("@SyncName", syncKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (null, null, null, 0, null);
        }

        return (
            reader.IsDBNull(0) ? null : reader.GetDateTime(0),
            reader.IsDBNull(1) ? null : reader.GetDateTime(1),
            reader.IsDBNull(2) ? null : reader.GetBoolean(2),
            reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            reader.IsDBNull(4) ? null : reader.GetString(4));
    }

    private async Task MarkStartedAsync(SqlConnection localConnection, string syncKey, string message, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ApplicationLookupSyncStates
            SET LastStartedAt = SYSUTCDATETIME(),
                LastCompletedAt = NULL,
                LastRunSucceeded = NULL,
                LastRunMessage = @LastRunMessage
            WHERE SourceSystem = @SourceSystem AND SyncName = @SyncName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@LastRunMessage", message);
        command.Parameters.AddWithValue("@SourceSystem", SourceSystem);
        command.Parameters.AddWithValue("@SyncName", syncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkCompletedAsync(SqlConnection localConnection, string syncKey, int processedRows, string message, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ApplicationLookupSyncStates
            SET LastCompletedAt = SYSUTCDATETIME(),
                LastRunSucceeded = 1,
                LastProcessedRows = @LastProcessedRows,
                LastRunMessage = @LastRunMessage
            WHERE SourceSystem = @SourceSystem AND SyncName = @SyncName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@LastProcessedRows", processedRows);
        command.Parameters.AddWithValue("@LastRunMessage", message);
        command.Parameters.AddWithValue("@SourceSystem", SourceSystem);
        command.Parameters.AddWithValue("@SyncName", syncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkFailedAsync(SqlConnection localConnection, string syncKey, string message, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ApplicationLookupSyncStates
            SET LastCompletedAt = SYSUTCDATETIME(),
                LastRunSucceeded = 0,
                LastProcessedRows = 0,
                LastRunMessage = @LastRunMessage
            WHERE SourceSystem = @SourceSystem AND SyncName = @SyncName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@LastRunMessage", message.Length > 4000 ? message[..4000] : message);
        command.Parameters.AddWithValue("@SourceSystem", SourceSystem);
        command.Parameters.AddWithValue("@SyncName", syncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
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
        command.Parameters.AddWithValue("@Resource", $"ApplicationLookupSync:{syncKey}");

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
            command.Parameters.AddWithValue("@Resource", $"ApplicationLookupSync:{syncKey}");
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to release application lookup sync lock for {SyncKey}.", syncKey);
        }
    }

    private SyncJobDefinition GetDefinition(string syncKey)
    {
        return syncKey switch
        {
            ApplicationCategoriesKey => new("Application Category Sync", "Syncs application category lookup data.", ResolveSourceObjectName(syncKey), "[dbo].[ApplicationCategories]"),
            ApplicationForsKey => new("Application For Sync", "Syncs application-for lookup data and category linkage.", ResolveSourceObjectName(syncKey), "[dbo].[ApplicationFors]"),
            ApplicationTypesKey => new("Application Type Sync", "Syncs application type lookup data.", ResolveSourceObjectName(syncKey), "[dbo].[ApplicationTypes]"),
            ApplicationStatusesKey => new("Application Status Sync", "Syncs application status lookup data.", ResolveSourceObjectName(syncKey), "[dbo].[ApplicationStatuses]"),
            ApplicationCategoryForsKey => new("Application Category/For Mapping Sync", "Syncs explicit category-to-application-for mappings.", ResolveSourceObjectName(syncKey), "[dbo].[ApplicationCategoryFors]"),
            ApplicationForTypesKey => new("Application For/Type Mapping Sync", "Syncs allowed application-for to application-type combinations.", ResolveSourceObjectName(syncKey), "[dbo].[ApplicationForTypes]"),
            _ => throw new InvalidOperationException($"Application lookup sync job '{syncKey}' is not configured.")
        };
    }

    private string ResolveSourceObjectName(string syncKey)
    {
        var objectName = syncKey switch
        {
            ApplicationCategoriesKey => _options.ApplicationCategorySourceObjectName,
            ApplicationForsKey => _options.ApplicationForSourceObjectName,
            ApplicationTypesKey => _options.ApplicationTypeSourceObjectName,
            ApplicationStatusesKey => _options.ApplicationStatusSourceObjectName,
            ApplicationCategoryForsKey => _options.ApplicationForCategorySourceObjectName,
            ApplicationForTypesKey => _options.ApplicationForApplicationTypeSourceObjectName,
            _ => throw new InvalidOperationException($"Application lookup sync job '{syncKey}' is not configured.")
        };

        if (string.IsNullOrWhiteSpace(objectName) || !ObjectNamePattern.IsMatch(objectName))
        {
            throw new InvalidOperationException($"Application lookup source object name for '{syncKey}' is invalid.");
        }

        return objectName;
    }

    private string GetLocalConnectionString() =>
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    private string GetSourceConnectionString()
    {
        var namedConnectionString = string.IsNullOrWhiteSpace(_options.SourceConnectionStringName)
            ? null
            : configuration.GetConnectionString(_options.SourceConnectionStringName);

        return !string.IsNullOrWhiteSpace(namedConnectionString)
            ? namedConnectionString
            : _options.SourceConnectionString ?? throw new InvalidOperationException("Application lookup source connection string is not configured.");
    }

    private bool HasConfiguredSourceConnection()
    {
        var namedConnectionString = string.IsNullOrWhiteSpace(_options.SourceConnectionStringName)
            ? null
            : configuration.GetConnectionString(_options.SourceConnectionStringName);

        return !string.IsNullOrWhiteSpace(namedConnectionString) || !string.IsNullOrWhiteSpace(_options.SourceConnectionString);
    }

    private static DateTimeOffset? ToUtc(DateTime? value) => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null;

    private sealed record SyncJobDefinition(string Name, string Description, string SourceObjectName, string TargetTableName);
}
