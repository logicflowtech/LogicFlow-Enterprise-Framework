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

public sealed class CompanyFinancialDataSyncService(
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    ITenantProvider tenantProvider,
    IOptions<CompanyFinancialDataSyncOptions> options,
    ILogger<CompanyFinancialDataSyncService> logger) : ICompanyFinancialDataSyncService
{
    private const string SyncKey = "company-financial-data";
    private const string SyncStateSourceSystem = "outsystems";
    private static readonly Regex ObjectNamePattern = new("^[A-Za-z0-9_\\.\\[\\]]+$", RegexOptions.Compiled);
    private readonly CompanyFinancialDataSyncOptions _options = options.Value;

    public async Task<SyncJobSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(cancellationToken);
        return new SyncJobSummaryResponse(
            SyncKey,
            "Company Financial Data Sync",
            "Sync company-level financial details and ownership breakdowns into local cache tables.",
            ResolveSourceObjectName(_options.ProjectFinancingSourceObjectName),
            "[dbo].[CompanyProfileFinancialDetails] + related financial tables",
            false,
            0,
            _options.BatchSize,
            false,
            _options.SourceConnectionStringName,
            HasConfiguredSourceConnection(),
            status.LocalRowCount,
            status.LastStartedAt,
            status.LastCompletedAt,
            status.LastRunSucceeded,
            status.LastProcessedRows,
            status.LastRunMessage,
            string.Empty);
    }

    public async Task<CompanyFinancialDataSyncStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureTargetTablesAsync(localConnection, cancellationToken);
        await EnsureSyncStateRowAsync(localConnection, cancellationToken);
        var state = await ReadStateAsync(localConnection, cancellationToken);
        var localRowCount = await GetLocalRowCountAsync(localConnection, cancellationToken);

        return new CompanyFinancialDataSyncStatusResponse(
            _options.SourceConnectionStringName,
            HasConfiguredSourceConnection(),
            _options.BatchSize,
            localRowCount,
            ToUtc(state.LastStartedAt),
            ToUtc(state.LastCompletedAt),
            state.LastRunSucceeded,
            state.LastProcessedRows,
            state.LastRunMessage,
            state.LastSourceCompanyId);
    }

    public async Task<CompanyFinancialDataSyncStatusResponse> RunSyncAsync(long? sourceCompanyId = null, CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureTargetTablesAsync(localConnection, cancellationToken);
        await EnsureSyncStateRowAsync(localConnection, cancellationToken);

        try
        {
            await MarkStartedAsync(localConnection, sourceCompanyId, cancellationToken);

            var companyMap = await dbContext.CompanyProfiles
                .AsNoTracking()
                .Where(x => x.MigratedId.HasValue)
                .ToDictionaryAsync(x => x.MigratedId!.Value, x => x.Id, cancellationToken);

            var tenantId = await ResolveTenantIdAsync(cancellationToken);
            var financialDetails = await LoadFinancialDetailsAsync(companyMap, sourceCompanyId, cancellationToken);
            await using var transaction = (SqlTransaction)await localConnection.BeginTransactionAsync(cancellationToken);
            await MergeFinancialDetailsAsync(localConnection, transaction, tenantId, financialDetails, sourceCompanyId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var financialDetailMap = await dbContext.CompanyProfileFinancialDetails
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ToDictionaryAsync(x => x.MigratedId, x => x.Id, cancellationToken);

            var authorizedCapitals = await LoadAuthorizedCapitalAsync(companyMap, cancellationToken);
            var equityStructures = await LoadSimpleFinancialChildAsync(
                ResolveSourceObjectName(_options.EquityStructureSourceObjectName),
                financialDetailMap,
                "BUMI_RM, BUMI_PERCENT, NONBUMI_RM, NONBUMI_PERCENT, FOREIGN_RM, FOREIGN_PERCENT, TOTAL_RM, TOTAL_PERCENT",
                cancellationToken);
            var financialPerformance = await LoadSimpleFinancialChildAsync(
                ResolveSourceObjectName(_options.FinancialPerformanceSourceObjectName),
                financialDetailMap,
                "REVENUE, PROFIT, TAXABLEEXPENDITURE, EXPORTSALES, LOCALSALES, TOTALASSET, SHAREHOLDERFUND, EMPLOYEECOUNT",
                cancellationToken);
            var paidUpCapitals = await LoadSimpleFinancialChildAsync(
                ResolveSourceObjectName(_options.PaidUpCapitalSourceObjectName),
                financialDetailMap,
                "TOTAL_PAIDUPCAPITAL, TOTAL_RESERVES, TOTAL_SHAREHOLDERFUND, TOTALRM_MSIANINDIVIDUALS, TOTALPERCENT_MSIANINDIVIDUAL, TOTALRM_FOREIGNCOMPANY, TOTALPERCENT_FOREIGNCOMPANY, TOTALRM_COMPANYMALAYSIA, TOTALPERCENT_COMPANYMALAYSI",
                cancellationToken);
            var malaysianIndividuals = await LoadSimpleFinancialChildAsync(
                ResolveSourceObjectName(_options.MalaysianIndividualsSourceObjectName),
                financialDetailMap,
                "BUMIPUTERA_RM, BUMIPUTERA_PERCENT, NONBUMIPUTERA_RM, NONBUMIPUTERA_PERCENT",
                cancellationToken,
                "PUC_PAIDUPCAPITALID");
            var foreignCompanies = await LoadForeignCompaniesAsync(financialDetailMap, cancellationToken);
            var companiesMalaysia = await LoadCompaniesMalaysiaAsync(financialDetailMap, cancellationToken);
            var loans = await LoadSimpleFinancialChildAsync(
                ResolveSourceObjectName(_options.LoanSourceObjectName),
                financialDetailMap,
                "TOTALRM_LOAN, TOTALLOAN_PERCENT, DOMESTICDESCRIPTION, FOREIGNDESCRIPTION",
                cancellationToken);
            var loanForeign = await LoadLoanForeignAsync(financialDetailMap, cancellationToken);
            var totalFinancing = await LoadSimpleFinancialChildAsync(
                ResolveSourceObjectName(_options.TotalFinancingSourceObjectName),
                financialDetailMap,
                "TOTALPAIDUPCAPITAL, TOTALRESERVE, TOTALLOAN, TOTALRM_OTHERSOURCES, TOTALPERCENT_OTHERSOURCES, TOTALFINANCING_RM",
                cancellationToken);
            var otherSources = await LoadOtherSourcesAsync(financialDetailMap, cancellationToken);

            await using var detailTransaction = (SqlTransaction)await localConnection.BeginTransactionAsync(cancellationToken);
            await MergeAuthorizedCapitalAsync(localConnection, detailTransaction, tenantId, authorizedCapitals, sourceCompanyId, cancellationToken);
            await MergeSimpleFinancialChildAsync(localConnection, detailTransaction, tenantId, "CompanyProfileEquityStructures", equityStructures, sourceCompanyId, cancellationToken);
            await MergeSimpleFinancialChildAsync(localConnection, detailTransaction, tenantId, "CompanyProfileFinancialPerformanceRecords", financialPerformance, sourceCompanyId, cancellationToken);
            await MergeSimpleFinancialChildAsync(localConnection, detailTransaction, tenantId, "CompanyProfilePaidUpCapitals", paidUpCapitals, sourceCompanyId, cancellationToken);
            await MergeSimpleFinancialChildAsync(localConnection, detailTransaction, tenantId, "CompanyProfilePaidUpCapitalMalaysianIndividuals", malaysianIndividuals, sourceCompanyId, cancellationToken);
            await MergeOwnedRowsAsync(localConnection, detailTransaction, tenantId, "CompanyProfilePaidUpCapitalForeignCompanies", foreignCompanies, sourceCompanyId, cancellationToken);
            await MergeOwnedRowsAsync(localConnection, detailTransaction, tenantId, "CompanyProfilePaidUpCapitalCompaniesMalaysia", companiesMalaysia, sourceCompanyId, cancellationToken);
            await MergeSimpleFinancialChildAsync(localConnection, detailTransaction, tenantId, "CompanyProfileLoans", loans, sourceCompanyId, cancellationToken);
            await MergeOwnedRowsAsync(localConnection, detailTransaction, tenantId, "CompanyProfileLoanForeigns", loanForeign, sourceCompanyId, cancellationToken);
            await MergeSimpleFinancialChildAsync(localConnection, detailTransaction, tenantId, "CompanyProfileTotalFinancings", totalFinancing, sourceCompanyId, cancellationToken);
            await MergeOwnedRowsAsync(localConnection, detailTransaction, tenantId, "CompanyProfileOtherSources", otherSources, sourceCompanyId, cancellationToken);
            await detailTransaction.CommitAsync(cancellationToken);

            var processedRows =
                financialDetails.Rows.Count + authorizedCapitals.Rows.Count + equityStructures.Rows.Count + financialPerformance.Rows.Count +
                paidUpCapitals.Rows.Count + malaysianIndividuals.Rows.Count + foreignCompanies.Rows.Count + companiesMalaysia.Rows.Count +
                loans.Rows.Count + loanForeign.Rows.Count + totalFinancing.Rows.Count + otherSources.Rows.Count;
            await MarkCompletedAsync(localConnection, sourceCompanyId, processedRows, $"Processed {processedRows} financial rows.", cancellationToken);
            return await GetStatusAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Company financial data sync failed for source company id {SourceCompanyId}.", sourceCompanyId);
            await TryMarkFailedAsync(localConnection, sourceCompanyId, exception.Message, cancellationToken);
            throw;
        }
    }

    private async Task<DataTable> LoadFinancialDetailsAsync(IReadOnlyDictionary<long, Guid> companyMap, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        var table = new DataTable();
        table.Columns.Add("CompanyProfileId", typeof(Guid));
        table.Columns.Add("MigratedId", typeof(long));
        table.Columns.Add("LegacyProjectId", typeof(long));
        table.Columns.Add("FinancialYear", typeof(int));
        table.Columns.Add("EffectiveDate", typeof(DateTime));
        table.Columns.Add("ProjectStatusId", typeof(int));
        table.Columns.Add("SourceCreatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceCreatedAt", typeof(DateTime));
        table.Columns.Add("SourceUpdatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceUpdatedAt", typeof(DateTime));

        var query = $"""
            SELECT
                pf.ID AS MigratedId,
                pf.PROJECTID AS LegacyProjectId,
                pf.[YEAR] AS FinancialYear,
                pf.EFFECTIVEDATE,
                pf.PROJECTSTATUSID,
                pf.CREATEDBY,
                pf.CREATEDDATETIME,
                pf.MODIFIEDBY,
                pf.MODIFIEDDATETIME,
                COALESCE(fs.COMPANYID, project.COMPANYID) AS LegacyCompanyId
            FROM {ResolveSourceObjectName(_options.ProjectFinancingSourceObjectName)} pf
            LEFT JOIN {ResolveSourceObjectName(_options.ProjectSourceObjectName)} project ON project.ID = pf.PROJECTID
            LEFT JOIN {ResolveSourceObjectName(_options.FinancingStructureSourceObjectName)} fs ON fs.ID = pf.ID
            WHERE COALESCE(fs.COMPANYID, project.COMPANYID) IS NOT NULL
              AND (@SourceCompanyId IS NULL OR COALESCE(fs.COMPANYID, project.COMPANYID) = @SourceCompanyId)
            ORDER BY pf.ID;
            """;

        await using var sourceConnection = new SqlConnection(GetSourceConnectionString());
        await sourceConnection.OpenAsync(cancellationToken);
        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@SourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var legacyCompanyId = reader.GetInt64(9);
            if (!companyMap.TryGetValue(legacyCompanyId, out var companyProfileId))
            {
                continue;
            }

            table.Rows.Add(
                companyProfileId,
                reader.GetInt64(0),
                reader.IsDBNull(1) ? DBNull.Value : reader.GetInt64(1),
                reader.IsDBNull(2) ? DBNull.Value : reader.GetInt32(2),
                reader.IsDBNull(3) ? DBNull.Value : reader.GetDateTime(3),
                reader.IsDBNull(4) ? DBNull.Value : reader.GetInt32(4),
                reader.IsDBNull(5) ? DBNull.Value : reader.GetInt32(5),
                NormalizeDateTime(reader, 6),
                reader.IsDBNull(7) ? DBNull.Value : reader.GetInt32(7),
                NormalizeDateTime(reader, 8));
        }

        return table;
    }

    private async Task<DataTable> LoadAuthorizedCapitalAsync(IReadOnlyDictionary<long, Guid> companyMap, CancellationToken cancellationToken)
    {
        var table = new DataTable();
        table.Columns.Add("CompanyProfileId", typeof(Guid));
        table.Columns.Add("MigratedCompanyId", typeof(long));
        table.Columns.Add("AuthorizedCapital", typeof(decimal));
        table.Columns.Add("SourceCreatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceCreatedAt", typeof(DateTime));
        table.Columns.Add("SourceUpdatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceUpdatedAt", typeof(DateTime));

        var query = $"""
            SELECT COMPANYID, AUTHORIZEDCAPITAL, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
            FROM {ResolveSourceObjectName(_options.AuthorizedCapitalSourceObjectName)}
            WHERE COMPANYID IS NOT NULL
            ORDER BY COMPANYID;
            """;

        await using var sourceConnection = new SqlConnection(GetSourceConnectionString());
        await sourceConnection.OpenAsync(cancellationToken);
        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var companyId = reader.GetInt64(0);
            if (!companyMap.TryGetValue(companyId, out var companyProfileId))
            {
                continue;
            }

            table.Rows.Add(
                companyProfileId,
                companyId,
                reader.IsDBNull(1) ? DBNull.Value : reader.GetDecimal(1),
                reader.IsDBNull(2) ? DBNull.Value : reader.GetInt32(2),
                NormalizeDateTime(reader, 3),
                reader.IsDBNull(4) ? DBNull.Value : reader.GetInt32(4),
                NormalizeDateTime(reader, 5));
        }

        return table;
    }

    private async Task<DataTable> LoadSimpleFinancialChildAsync(string objectName, IReadOnlyDictionary<long, Guid> financialDetailMap, string projection, CancellationToken cancellationToken, string idColumn = "FINANCINGDETAILSID")
    {
        var table = new DataTable();
        table.Columns.Add("FinancialDetailsId", typeof(Guid));
        table.Columns.Add("Payload", typeof(string));
        table.Columns.Add("SourceCreatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceCreatedAt", typeof(DateTime));
        table.Columns.Add("SourceUpdatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceUpdatedAt", typeof(DateTime));

        var query = $"""
            SELECT {idColumn} AS LegacyId, {projection}, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
            FROM {objectName}
            WHERE {idColumn} IS NOT NULL
            ORDER BY {idColumn};
            """;

        await using var sourceConnection = new SqlConnection(GetSourceConnectionString());
        await sourceConnection.OpenAsync(cancellationToken);
        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var legacyId = reader.GetInt64(0);
            if (!financialDetailMap.TryGetValue(legacyId, out var financialDetailsId))
            {
                continue;
            }

            var payload = new object[reader.FieldCount - 5];
            for (var i = 1; i < reader.FieldCount - 4; i++)
            {
                payload[i - 1] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
            }

            table.Rows.Add(
                financialDetailsId,
                System.Text.Json.JsonSerializer.Serialize(payload),
                reader.IsDBNull(reader.FieldCount - 4) ? DBNull.Value : reader.GetInt32(reader.FieldCount - 4),
                NormalizeDateTime(reader, reader.FieldCount - 3),
                reader.IsDBNull(reader.FieldCount - 2) ? DBNull.Value : reader.GetInt32(reader.FieldCount - 2),
                NormalizeDateTime(reader, reader.FieldCount - 1));
        }

        return table;
    }

    private async Task<DataTable> LoadForeignCompaniesAsync(IReadOnlyDictionary<long, Guid> financialDetailMap, CancellationToken cancellationToken)
    {
        return await LoadOwnedRowsWithNameAsync(
            ResolveSourceObjectName(_options.ForeignCompanySourceObjectName),
            financialDetailMap,
            "PUC_PAIDUPCAPITALID",
            "ID",
            "COMPANYNAME",
            "COUNTRYID",
            cancellationToken);
    }

    private async Task<DataTable> LoadCompaniesMalaysiaAsync(IReadOnlyDictionary<long, Guid> financialDetailMap, CancellationToken cancellationToken)
    {
        return await LoadOwnedRowsWithNameAsync(
            ResolveSourceObjectName(_options.CompanyMalaysiaSourceObjectName),
            financialDetailMap,
            "PUC_PAIDUPCAPITALID",
            "ID",
            "COMPANYNAME",
            null,
            cancellationToken);
    }

    private async Task<DataTable> LoadLoanForeignAsync(IReadOnlyDictionary<long, Guid> financialDetailMap, CancellationToken cancellationToken)
    {
        return await LoadOwnedRowsWithNameAsync(
            ResolveSourceObjectName(_options.LoanForeignSourceObjectName),
            financialDetailMap,
            "LOANID",
            "ID",
            null,
            "COUNTRYOFORIGIN",
            cancellationToken);
    }

    private async Task<DataTable> LoadOtherSourcesAsync(IReadOnlyDictionary<long, Guid> financialDetailMap, CancellationToken cancellationToken)
    {
        return await LoadOwnedRowsWithNameAsync(
            ResolveSourceObjectName(_options.OtherSourcesSourceObjectName),
            financialDetailMap,
            "TOTALFINANCINGID",
            "ID",
            "OTHERSOURCES",
            null,
            cancellationToken);
    }

    private async Task<DataTable> LoadOwnedRowsWithNameAsync(string objectName, IReadOnlyDictionary<long, Guid> financialDetailMap, string parentIdColumn, string migratedIdColumn, string? textColumn, string? countryColumn, CancellationToken cancellationToken)
    {
        var table = new DataTable();
        table.Columns.Add("FinancialDetailsId", typeof(Guid));
        table.Columns.Add("MigratedId", typeof(long));
        table.Columns.Add("DisplayText", typeof(string));
        table.Columns.Add("LegacyCountryId", typeof(long));
        table.Columns.Add("AmountRm", typeof(decimal));
        table.Columns.Add("AmountPercent", typeof(decimal));
        table.Columns.Add("SourceCreatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceCreatedAt", typeof(DateTime));
        table.Columns.Add("SourceUpdatedByLegacyUserId", typeof(int));
        table.Columns.Add("SourceUpdatedAt", typeof(DateTime));

        var countrySelect = countryColumn is null ? "CAST(NULL AS BIGINT) AS LegacyCountryId" : $"{countryColumn} AS LegacyCountryId";
        var textSelect = textColumn is null ? "CAST(NULL AS NVARCHAR(500)) AS DisplayText" : $"{textColumn} AS DisplayText";
        var query = $"""
            SELECT {parentIdColumn} AS ParentId, {migratedIdColumn} AS MigratedId, {textSelect}, {countrySelect}, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
            FROM {objectName}
            WHERE {parentIdColumn} IS NOT NULL
            ORDER BY {migratedIdColumn};
            """;

        await using var sourceConnection = new SqlConnection(GetSourceConnectionString());
        await sourceConnection.OpenAsync(cancellationToken);
        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var parentId = reader.GetInt64(0);
            if (!financialDetailMap.TryGetValue(parentId, out var financialDetailsId))
            {
                continue;
            }

            table.Rows.Add(
                financialDetailsId,
                reader.GetInt64(1),
                reader.IsDBNull(2) ? DBNull.Value : reader.GetString(2),
                reader.IsDBNull(3) ? DBNull.Value : reader.GetInt64(3),
                reader.IsDBNull(4) ? DBNull.Value : reader.GetDecimal(4),
                reader.IsDBNull(5) ? DBNull.Value : reader.GetDecimal(5),
                reader.IsDBNull(6) ? DBNull.Value : reader.GetInt32(6),
                NormalizeDateTime(reader, 7),
                reader.IsDBNull(8) ? DBNull.Value : reader.GetInt32(8),
                NormalizeDateTime(reader, 9));
        }

        return table;
    }

    private async Task MergeFinancialDetailsAsync(SqlConnection localConnection, SqlTransaction transaction, Guid tenantId, DataTable rows, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        const string createSql = """
            IF OBJECT_ID('tempdb..#CompanyFinancialDetailsBatch') IS NOT NULL DROP TABLE #CompanyFinancialDetailsBatch;
            CREATE TABLE #CompanyFinancialDetailsBatch
            (
                CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
                MigratedId BIGINT NOT NULL,
                LegacyProjectId BIGINT NULL,
                FinancialYear INT NULL,
                EffectiveDate DATETIME2(3) NULL,
                ProjectStatusId INT NULL,
                SourceCreatedByLegacyUserId INT NULL,
                SourceCreatedAt DATETIME2(3) NULL,
                SourceUpdatedByLegacyUserId INT NULL,
                SourceUpdatedAt DATETIME2(3) NULL
            );
            """;
        const string mergeSql = """
            MERGE dbo.CompanyProfileFinancialDetails AS target
            USING #CompanyFinancialDetailsBatch AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET
                    TenantId = @TenantId,
                    CompanyProfileId = source.CompanyProfileId,
                    LegacyProjectId = source.LegacyProjectId,
                    FinancialYear = source.FinancialYear,
                    EffectiveDate = source.EffectiveDate,
                    ProjectStatusId = source.ProjectStatusId,
                    SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
                    SourceCreatedAt = source.SourceCreatedAt,
                    SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
                    SourceUpdatedAt = source.SourceUpdatedAt,
                    LastSyncedAt = SYSUTCDATETIME(),
                    UpdatedAt = SYSUTCDATETIME(),
                    UpdatedBy = @Actor,
                    IsDeleted = 0,
                    DeletedAt = NULL
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (TenantId, CompanyProfileId, MigratedId, LegacyProjectId, FinancialYear, EffectiveDate, ProjectStatusId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, CreatedBy, IsDeleted)
                VALUES (@TenantId, source.CompanyProfileId, source.MigratedId, source.LegacyProjectId, source.FinancialYear, source.EffectiveDate, source.ProjectStatusId, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), @Actor, 0)
            WHEN NOT MATCHED BY SOURCE AND (@IsFullSync = 1 OR target.CompanyProfileId IN (SELECT DISTINCT CompanyProfileId FROM #CompanyFinancialDetailsBatch)) THEN
                UPDATE SET target.IsDeleted = 1, target.DeletedAt = SYSUTCDATETIME(), target.UpdatedAt = SYSUTCDATETIME(), target.UpdatedBy = @Actor;
            """;
        await CreateTempTableAsync(localConnection, transaction, createSql, cancellationToken);
        await BulkCopyAsync(localConnection, transaction, "#CompanyFinancialDetailsBatch", rows, cancellationToken);
        await ExecuteMergeAsync(localConnection, transaction, mergeSql, tenantId, sourceCompanyId, cancellationToken);
    }

    private async Task MergeAuthorizedCapitalAsync(SqlConnection localConnection, SqlTransaction transaction, Guid tenantId, DataTable rows, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        const string createSql = """
            IF OBJECT_ID('tempdb..#CompanyProfileAuthorizedCapitalsBatch') IS NOT NULL DROP TABLE #CompanyProfileAuthorizedCapitalsBatch;
            CREATE TABLE #CompanyProfileAuthorizedCapitalsBatch
            (
                CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
                MigratedCompanyId BIGINT NOT NULL,
                AuthorizedCapital DECIMAL(18,2) NULL,
                SourceCreatedByLegacyUserId INT NULL,
                SourceCreatedAt DATETIME2(3) NULL,
                SourceUpdatedByLegacyUserId INT NULL,
                SourceUpdatedAt DATETIME2(3) NULL
            );
            """;
        const string mergeSql = """
            MERGE dbo.CompanyProfileAuthorizedCapitals AS target
            USING #CompanyProfileAuthorizedCapitalsBatch AS source
                ON target.MigratedCompanyId = source.MigratedCompanyId
            WHEN MATCHED THEN
                UPDATE SET TenantId = @TenantId, CompanyProfileId = source.CompanyProfileId, AuthorizedCapital = source.AuthorizedCapital, SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId, SourceCreatedAt = source.SourceCreatedAt, SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId, SourceUpdatedAt = source.SourceUpdatedAt, LastSyncedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @Actor, IsDeleted = 0, DeletedAt = NULL
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (TenantId, CompanyProfileId, MigratedCompanyId, AuthorizedCapital, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, CreatedBy, IsDeleted)
                VALUES (@TenantId, source.CompanyProfileId, source.MigratedCompanyId, source.AuthorizedCapital, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), @Actor, 0);
            """;
        await CreateTempTableAsync(localConnection, transaction, createSql, cancellationToken);
        await BulkCopyAsync(localConnection, transaction, "#CompanyProfileAuthorizedCapitalsBatch", rows, cancellationToken);
        await ExecuteMergeAsync(localConnection, transaction, mergeSql, tenantId, sourceCompanyId, cancellationToken);
    }

    private async Task MergeSimpleFinancialChildAsync(SqlConnection localConnection, SqlTransaction transaction, Guid tenantId, string tableName, DataTable rows, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        const string createSql = """
            IF OBJECT_ID('tempdb..#CompanyFinancialSimpleBatch') IS NOT NULL DROP TABLE #CompanyFinancialSimpleBatch;
            CREATE TABLE #CompanyFinancialSimpleBatch
            (
                FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                Payload NVARCHAR(MAX) NULL,
                SourceCreatedByLegacyUserId INT NULL,
                SourceCreatedAt DATETIME2(3) NULL,
                SourceUpdatedByLegacyUserId INT NULL,
                SourceUpdatedAt DATETIME2(3) NULL
            );
            """;
        var mergeSql = $"""
            MERGE dbo.{tableName} AS target
            USING #CompanyFinancialSimpleBatch AS source
                ON target.FinancialDetailsId = source.FinancialDetailsId
            WHEN MATCHED THEN
                UPDATE SET TenantId = @TenantId, SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId, SourceCreatedAt = source.SourceCreatedAt, SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId, SourceUpdatedAt = source.SourceUpdatedAt, LastSyncedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @Actor, IsDeleted = 0, DeletedAt = NULL
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (TenantId, FinancialDetailsId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, CreatedBy, IsDeleted)
                VALUES (@TenantId, source.FinancialDetailsId, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), @Actor, 0);
            """;
        await CreateTempTableAsync(localConnection, transaction, createSql, cancellationToken);
        await BulkCopyAsync(localConnection, transaction, "#CompanyFinancialSimpleBatch", rows, cancellationToken);
        await ExecuteMergeAsync(localConnection, transaction, mergeSql, tenantId, sourceCompanyId, cancellationToken);
        await ApplySimplePayloadAsync(localConnection, transaction, tableName, rows, cancellationToken);
    }

    private async Task MergeOwnedRowsAsync(SqlConnection localConnection, SqlTransaction transaction, Guid tenantId, string tableName, DataTable rows, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        const string createSql = """
            IF OBJECT_ID('tempdb..#CompanyFinancialOwnedBatch') IS NOT NULL DROP TABLE #CompanyFinancialOwnedBatch;
            CREATE TABLE #CompanyFinancialOwnedBatch
            (
                FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                MigratedId BIGINT NOT NULL,
                DisplayText NVARCHAR(500) NULL,
                LegacyCountryId BIGINT NULL,
                AmountRm DECIMAL(18,2) NULL,
                AmountPercent DECIMAL(18,2) NULL,
                SourceCreatedByLegacyUserId INT NULL,
                SourceCreatedAt DATETIME2(3) NULL,
                SourceUpdatedByLegacyUserId INT NULL,
                SourceUpdatedAt DATETIME2(3) NULL
            );
            """;
        var mergeSql = $"""
            MERGE dbo.{tableName} AS target
            USING #CompanyFinancialOwnedBatch AS source
                ON target.MigratedId = source.MigratedId
            WHEN MATCHED THEN
                UPDATE SET TenantId = @TenantId, FinancialDetailsId = source.FinancialDetailsId, LegacyCountryId = source.LegacyCountryId, SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId, SourceCreatedAt = source.SourceCreatedAt, SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId, SourceUpdatedAt = source.SourceUpdatedAt, LastSyncedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @Actor, IsDeleted = 0, DeletedAt = NULL
            WHEN NOT MATCHED BY TARGET THEN
                INSERT (TenantId, FinancialDetailsId, MigratedId, LegacyCountryId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, CreatedBy, IsDeleted)
                VALUES (@TenantId, source.FinancialDetailsId, source.MigratedId, source.LegacyCountryId, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), @Actor, 0);
            """;
        await CreateTempTableAsync(localConnection, transaction, createSql, cancellationToken);
        await BulkCopyAsync(localConnection, transaction, "#CompanyFinancialOwnedBatch", rows, cancellationToken);
        await ExecuteMergeAsync(localConnection, transaction, mergeSql, tenantId, sourceCompanyId, cancellationToken);
        await ApplyOwnedPayloadAsync(localConnection, transaction, tableName, rows, cancellationToken);
    }

    private async Task ApplySimplePayloadAsync(SqlConnection localConnection, SqlTransaction transaction, string tableName, DataTable rows, CancellationToken cancellationToken)
    {
        if (rows.Rows.Count == 0) return;
        foreach (DataRow row in rows.Rows)
        {
            var payload = row["Payload"]?.ToString() ?? "[]";
            var sql = tableName switch
            {
                "CompanyProfileEquityStructures" => """
                    UPDATE dbo.CompanyProfileEquityStructures
                    SET BumiRm = TRY_CAST(JSON_VALUE(@Payload, '$[0]') AS DECIMAL(18,2)),
                        BumiPercent = TRY_CAST(JSON_VALUE(@Payload, '$[1]') AS DECIMAL(18,2)),
                        NonBumiRm = TRY_CAST(JSON_VALUE(@Payload, '$[2]') AS DECIMAL(18,2)),
                        NonBumiPercent = TRY_CAST(JSON_VALUE(@Payload, '$[3]') AS DECIMAL(18,2)),
                        ForeignRm = TRY_CAST(JSON_VALUE(@Payload, '$[4]') AS DECIMAL(18,2)),
                        ForeignPercent = TRY_CAST(JSON_VALUE(@Payload, '$[5]') AS DECIMAL(18,2)),
                        TotalRm = TRY_CAST(JSON_VALUE(@Payload, '$[6]') AS DECIMAL(18,2)),
                        TotalPercent = TRY_CAST(JSON_VALUE(@Payload, '$[7]') AS DECIMAL(18,2))
                    WHERE FinancialDetailsId = @FinancialDetailsId;
                    """,
                "CompanyProfileFinancialPerformanceRecords" => """
                    UPDATE dbo.CompanyProfileFinancialPerformanceRecords
                    SET Revenue = TRY_CAST(JSON_VALUE(@Payload, '$[0]') AS DECIMAL(18,2)),
                        Profit = TRY_CAST(JSON_VALUE(@Payload, '$[1]') AS DECIMAL(18,2)),
                        TaxableExpenditure = TRY_CAST(JSON_VALUE(@Payload, '$[2]') AS DECIMAL(18,2)),
                        ExportSales = TRY_CAST(JSON_VALUE(@Payload, '$[3]') AS DECIMAL(18,2)),
                        LocalSales = TRY_CAST(JSON_VALUE(@Payload, '$[4]') AS DECIMAL(18,2)),
                        TotalAsset = TRY_CAST(JSON_VALUE(@Payload, '$[5]') AS DECIMAL(18,2)),
                        ShareholderFund = TRY_CAST(JSON_VALUE(@Payload, '$[6]') AS DECIMAL(18,2)),
                        EmployeeCount = TRY_CAST(JSON_VALUE(@Payload, '$[7]') AS INT)
                    WHERE FinancialDetailsId = @FinancialDetailsId;
                    """,
                "CompanyProfilePaidUpCapitals" => """
                    UPDATE dbo.CompanyProfilePaidUpCapitals
                    SET TotalPaidUpCapital = TRY_CAST(JSON_VALUE(@Payload, '$[0]') AS DECIMAL(18,2)),
                        TotalReserves = TRY_CAST(JSON_VALUE(@Payload, '$[1]') AS DECIMAL(18,2)),
                        TotalShareholderFund = TRY_CAST(JSON_VALUE(@Payload, '$[2]') AS DECIMAL(18,2)),
                        TotalRmMalaysianIndividuals = TRY_CAST(JSON_VALUE(@Payload, '$[3]') AS DECIMAL(18,2)),
                        TotalPercentMalaysianIndividuals = TRY_CAST(JSON_VALUE(@Payload, '$[4]') AS DECIMAL(18,2)),
                        TotalRmForeignCompany = TRY_CAST(JSON_VALUE(@Payload, '$[5]') AS DECIMAL(18,2)),
                        TotalPercentForeignCompany = TRY_CAST(JSON_VALUE(@Payload, '$[6]') AS DECIMAL(18,2)),
                        TotalRmCompanyMalaysia = TRY_CAST(JSON_VALUE(@Payload, '$[7]') AS DECIMAL(18,2)),
                        TotalPercentCompanyMalaysia = TRY_CAST(JSON_VALUE(@Payload, '$[8]') AS DECIMAL(18,2))
                    WHERE FinancialDetailsId = @FinancialDetailsId;
                    """,
                "CompanyProfilePaidUpCapitalMalaysianIndividuals" => """
                    UPDATE dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals
                    SET BumiputeraRm = TRY_CAST(JSON_VALUE(@Payload, '$[0]') AS DECIMAL(18,2)),
                        BumiputeraPercent = TRY_CAST(JSON_VALUE(@Payload, '$[1]') AS DECIMAL(18,2)),
                        NonBumiputeraRm = TRY_CAST(JSON_VALUE(@Payload, '$[2]') AS DECIMAL(18,2)),
                        NonBumiputeraPercent = TRY_CAST(JSON_VALUE(@Payload, '$[3]') AS DECIMAL(18,2))
                    WHERE FinancialDetailsId = @FinancialDetailsId;
                    """,
                "CompanyProfileLoans" => """
                    UPDATE dbo.CompanyProfileLoans
                    SET TotalRmLoan = TRY_CAST(JSON_VALUE(@Payload, '$[0]') AS DECIMAL(18,2)),
                        TotalLoanPercent = TRY_CAST(JSON_VALUE(@Payload, '$[1]') AS DECIMAL(18,2)),
                        DomesticDescription = JSON_VALUE(@Payload, '$[2]'),
                        ForeignDescription = JSON_VALUE(@Payload, '$[3]')
                    WHERE FinancialDetailsId = @FinancialDetailsId;
                    """,
                "CompanyProfileTotalFinancings" => """
                    UPDATE dbo.CompanyProfileTotalFinancings
                    SET TotalPaidUpCapital = TRY_CAST(JSON_VALUE(@Payload, '$[0]') AS DECIMAL(18,2)),
                        TotalReserve = TRY_CAST(JSON_VALUE(@Payload, '$[1]') AS DECIMAL(18,2)),
                        TotalLoan = TRY_CAST(JSON_VALUE(@Payload, '$[2]') AS DECIMAL(18,2)),
                        TotalRmOtherSources = TRY_CAST(JSON_VALUE(@Payload, '$[3]') AS DECIMAL(18,2)),
                        TotalPercentOtherSources = TRY_CAST(JSON_VALUE(@Payload, '$[4]') AS DECIMAL(18,2)),
                        TotalFinancingRm = TRY_CAST(JSON_VALUE(@Payload, '$[5]') AS DECIMAL(18,2))
                    WHERE FinancialDetailsId = @FinancialDetailsId;
                    """,
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(sql))
            {
                continue;
            }

            await using var command = localConnection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.CommandText = sql;
            command.Parameters.AddWithValue("@FinancialDetailsId", row["FinancialDetailsId"]);
            command.Parameters.AddWithValue("@Payload", payload);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task ApplyOwnedPayloadAsync(SqlConnection localConnection, SqlTransaction transaction, string tableName, DataTable rows, CancellationToken cancellationToken)
    {
        if (rows.Rows.Count == 0) return;
        foreach (DataRow row in rows.Rows)
        {
            var sql = tableName switch
            {
                "CompanyProfilePaidUpCapitalForeignCompanies" => """
                    UPDATE dbo.CompanyProfilePaidUpCapitalForeignCompanies
                    SET CompanyName = @DisplayText,
                        AmountRm = @AmountRm,
                        AmountPercent = @AmountPercent,
                        CountryId = (SELECT TOP 1 Id FROM dbo.LookupCountries WHERE MigratedId = @LegacyCountryId)
                    WHERE MigratedId = @MigratedId;
                    """,
                "CompanyProfilePaidUpCapitalCompaniesMalaysia" => """
                    UPDATE dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia
                    SET CompanyName = @DisplayText,
                        AmountRm = @AmountRm,
                        AmountPercent = @AmountPercent
                    WHERE MigratedId = @MigratedId;
                    """,
                "CompanyProfileLoanForeigns" => """
                    UPDATE dbo.CompanyProfileLoanForeigns
                    SET AmountRm = @AmountRm,
                        AmountPercent = @AmountPercent,
                        CountryId = (SELECT TOP 1 Id FROM dbo.LookupCountries WHERE MigratedId = @LegacyCountryId)
                    WHERE MigratedId = @MigratedId;
                    """,
                "CompanyProfileOtherSources" => """
                    UPDATE dbo.CompanyProfileOtherSources
                    SET OtherSources = @DisplayText,
                        AmountRm = @AmountRm,
                        AmountPercent = @AmountPercent
                    WHERE MigratedId = @MigratedId;
                    """,
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(sql))
            {
                continue;
            }

            await using var command = localConnection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.CommandText = sql;
            command.Parameters.AddWithValue("@MigratedId", row["MigratedId"]);
            command.Parameters.AddWithValue("@DisplayText", row["DisplayText"] == DBNull.Value ? (object)DBNull.Value : row["DisplayText"]);
            command.Parameters.AddWithValue("@LegacyCountryId", row["LegacyCountryId"] == DBNull.Value ? (object)DBNull.Value : row["LegacyCountryId"]);
            command.Parameters.AddWithValue("@AmountRm", row["AmountRm"] == DBNull.Value ? (object)DBNull.Value : row["AmountRm"]);
            command.Parameters.AddWithValue("@AmountPercent", row["AmountPercent"] == DBNull.Value ? (object)DBNull.Value : row["AmountPercent"]);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
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

    private async Task ExecuteMergeAsync(SqlConnection localConnection, SqlTransaction transaction, string sql, Guid tenantId, long? sourceCompanyId, CancellationToken cancellationToken)
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
            IF OBJECT_ID(N'dbo.CompanyProfileFinancialDetails', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileFinancialDetails
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileFinancialDetails PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    LegacyProjectId BIGINT NULL,
                    FinancialYear INT NULL,
                    EffectiveDate DATETIME2(3) NULL,
                    ProjectStatusId INT NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileFinancialDetails_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileFinancialDetails_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileFinancialDetails_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileFinancialDetails_MigratedId ON dbo.CompanyProfileFinancialDetails (MigratedId);
                CREATE INDEX IX_CompanyProfileFinancialDetails_CompanyProfileId ON dbo.CompanyProfileFinancialDetails (CompanyProfileId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileAuthorizedCapitals', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileAuthorizedCapitals
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileAuthorizedCapitals PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
                    MigratedCompanyId BIGINT NOT NULL,
                    AuthorizedCapital DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileAuthorizedCapitals_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileAuthorizedCapitals_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileAuthorizedCapitals_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileAuthorizedCapitals_MigratedCompanyId ON dbo.CompanyProfileAuthorizedCapitals (MigratedCompanyId);
                CREATE INDEX IX_CompanyProfileAuthorizedCapitals_CompanyProfileId ON dbo.CompanyProfileAuthorizedCapitals (CompanyProfileId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileEquityStructures', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileEquityStructures
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileEquityStructures PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    BumiRm DECIMAL(18,2) NULL,
                    BumiPercent DECIMAL(18,2) NULL,
                    NonBumiRm DECIMAL(18,2) NULL,
                    NonBumiPercent DECIMAL(18,2) NULL,
                    ForeignRm DECIMAL(18,2) NULL,
                    ForeignPercent DECIMAL(18,2) NULL,
                    TotalRm DECIMAL(18,2) NULL,
                    TotalPercent DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileEquityStructures_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileEquityStructures_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileEquityStructures_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileEquityStructures_FinancialDetailsId ON dbo.CompanyProfileEquityStructures (FinancialDetailsId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileFinancialPerformanceRecords', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileFinancialPerformanceRecords
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileFinancialPerformanceRecords PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    Revenue DECIMAL(18,2) NULL,
                    Profit DECIMAL(18,2) NULL,
                    TaxableExpenditure DECIMAL(18,2) NULL,
                    ExportSales DECIMAL(18,2) NULL,
                    LocalSales DECIMAL(18,2) NULL,
                    TotalAsset DECIMAL(18,2) NULL,
                    ShareholderFund DECIMAL(18,2) NULL,
                    EmployeeCount INT NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileFinancialPerformanceRecords_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileFinancialPerformanceRecords_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileFinancialPerformanceRecords_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileFinancialPerformanceRecords_FinancialDetailsId ON dbo.CompanyProfileFinancialPerformanceRecords (FinancialDetailsId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfilePaidUpCapitals', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfilePaidUpCapitals
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfilePaidUpCapitals PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    TotalPaidUpCapital DECIMAL(18,2) NULL,
                    TotalReserves DECIMAL(18,2) NULL,
                    TotalShareholderFund DECIMAL(18,2) NULL,
                    TotalRmMalaysianIndividuals DECIMAL(18,2) NULL,
                    TotalPercentMalaysianIndividuals DECIMAL(18,2) NULL,
                    TotalRmForeignCompany DECIMAL(18,2) NULL,
                    TotalPercentForeignCompany DECIMAL(18,2) NULL,
                    TotalRmCompanyMalaysia DECIMAL(18,2) NULL,
                    TotalPercentCompanyMalaysia DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitals_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitals_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitals_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfilePaidUpCapitals_FinancialDetailsId ON dbo.CompanyProfilePaidUpCapitals (FinancialDetailsId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfilePaidUpCapitalMalaysianIndividuals PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    BumiputeraRm DECIMAL(18,2) NULL,
                    BumiputeraPercent DECIMAL(18,2) NULL,
                    NonBumiputeraRm DECIMAL(18,2) NULL,
                    NonBumiputeraPercent DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalMalaysianIndividuals_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalMalaysianIndividuals_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalMalaysianIndividuals_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfilePaidUpCapitalMalaysianIndividuals_FinancialDetailsId ON dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals (FinancialDetailsId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfilePaidUpCapitalForeignCompanies', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfilePaidUpCapitalForeignCompanies
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfilePaidUpCapitalForeignCompanies PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    CompanyName NVARCHAR(300) NULL,
                    CountryId UNIQUEIDENTIFIER NULL,
                    LegacyCountryId BIGINT NULL,
                    AmountRm DECIMAL(18,2) NULL,
                    AmountPercent DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalForeignCompanies_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalForeignCompanies_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalForeignCompanies_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfilePaidUpCapitalForeignCompanies_MigratedId ON dbo.CompanyProfilePaidUpCapitalForeignCompanies (MigratedId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfilePaidUpCapitalCompaniesMalaysia PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    CompanyName NVARCHAR(300) NULL,
                    AmountRm DECIMAL(18,2) NULL,
                    AmountPercent DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalCompaniesMalaysia_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalCompaniesMalaysia_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalCompaniesMalaysia_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfilePaidUpCapitalCompaniesMalaysia_MigratedId ON dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia (MigratedId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileCompanyIncorporated', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileCompanyIncorporated
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileCompanyIncorporated PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    PaidUpCapitalCompanyMalaysiaEntryId UNIQUEIDENTIFIER NOT NULL,
                    LocalCompanyTypeId INT NULL,
                    BumiPercent DECIMAL(18,2) NULL,
                    NonBumiPercent DECIMAL(18,2) NULL,
                    ForeignCountryId UNIQUEIDENTIFIER NULL,
                    LegacyForeignCountryId BIGINT NULL,
                    ForeignPercent DECIMAL(18,2) NULL,
                    TotalPercent DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporated_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporated_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporated_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileCompanyIncorporated_MigratedId ON dbo.CompanyProfileCompanyIncorporated (MigratedId);
                CREATE INDEX IX_CompanyProfileCompanyIncorporated_FinancialDetailsId ON dbo.CompanyProfileCompanyIncorporated (FinancialDetailsId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileCompanyIncorporatedCountries', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileCompanyIncorporatedCountries
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileCompanyIncorporatedCountries PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    CompanyIncorporatedEntryId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    CountryId UNIQUEIDENTIFIER NULL,
                    LegacyCountryId BIGINT NULL,
                    CountryPercent DECIMAL(18,2) NULL,
                    AmountRm DECIMAL(18,2) NULL,
                    PercentOverTotal DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporatedCountries_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporatedCountries_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporatedCountries_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileCompanyIncorporatedCountries_MigratedId ON dbo.CompanyProfileCompanyIncorporatedCountries (MigratedId);
                CREATE INDEX IX_CompanyProfileCompanyIncorporatedCountries_CompanyIncorporatedEntryId ON dbo.CompanyProfileCompanyIncorporatedCountries (CompanyIncorporatedEntryId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileLoans', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileLoans
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileLoans PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    TotalRmLoan DECIMAL(18,2) NULL,
                    TotalLoanPercent DECIMAL(18,2) NULL,
                    DomesticDescription NVARCHAR(MAX) NULL,
                    ForeignDescription NVARCHAR(MAX) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileLoans_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileLoans_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileLoans_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileLoans_FinancialDetailsId ON dbo.CompanyProfileLoans (FinancialDetailsId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileLoanDomestics', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileLoanDomestics
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileLoanDomestics PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    AmountRm DECIMAL(18,2) NULL,
                    AmountPercent DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileLoanDomestics_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileLoanDomestics_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileLoanDomestics_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileLoanDomestics_FinancialDetailsId ON dbo.CompanyProfileLoanDomestics (FinancialDetailsId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileLoanForeigns', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileLoanForeigns
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileLoanForeigns PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    CountryId UNIQUEIDENTIFIER NULL,
                    LegacyCountryId BIGINT NULL,
                    AmountRm DECIMAL(18,2) NULL,
                    AmountPercent DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileLoanForeigns_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileLoanForeigns_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileLoanForeigns_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileLoanForeigns_MigratedId ON dbo.CompanyProfileLoanForeigns (MigratedId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileTotalFinancings', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileTotalFinancings
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileTotalFinancings PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    TotalPaidUpCapital DECIMAL(18,2) NULL,
                    TotalReserve DECIMAL(18,2) NULL,
                    TotalLoan DECIMAL(18,2) NULL,
                    TotalRmOtherSources DECIMAL(18,2) NULL,
                    TotalPercentOtherSources DECIMAL(18,2) NULL,
                    TotalFinancingRm DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileTotalFinancings_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileTotalFinancings_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileTotalFinancings_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileTotalFinancings_FinancialDetailsId ON dbo.CompanyProfileTotalFinancings (FinancialDetailsId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileOtherSources', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileOtherSources
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileOtherSources PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    OtherSources NVARCHAR(500) NULL,
                    AmountRm DECIMAL(18,2) NULL,
                    AmountPercent DECIMAL(18,2) NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileOtherSources_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileOtherSources_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileOtherSources_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileOtherSources_MigratedId ON dbo.CompanyProfileOtherSources (MigratedId);
            END;

            IF OBJECT_ID(N'dbo.CompanyProfileUltimateParentHoldingCompanies', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyProfileUltimateParentHoldingCompanies
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileUltimateParentHoldingCompanies PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    TenantId UNIQUEIDENTIFIER NULL,
                    FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
                    MigratedId BIGINT NOT NULL,
                    PaidUpCapitalForeignCompanyEntryId UNIQUEIDENTIFIER NOT NULL,
                    UltimateCompany NVARCHAR(300) NULL,
                    CountryId UNIQUEIDENTIFIER NULL,
                    LegacyCountryId BIGINT NULL,
                    SourceCreatedByLegacyUserId INT NULL,
                    SourceCreatedAt DATETIME2(3) NULL,
                    SourceUpdatedByLegacyUserId INT NULL,
                    SourceUpdatedAt DATETIME2(3) NULL,
                    LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileUltimateParentHoldingCompanies_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                    CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileUltimateParentHoldingCompanies_CreatedAt DEFAULT SYSUTCDATETIME(),
                    CreatedBy NVARCHAR(450) NULL,
                    UpdatedAt DATETIMEOFFSET NULL,
                    UpdatedBy NVARCHAR(450) NULL,
                    IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileUltimateParentHoldingCompanies_IsDeleted DEFAULT (0),
                    DeletedAt DATETIMEOFFSET NULL
                );
                CREATE UNIQUE INDEX IX_CompanyProfileUltimateParentHoldingCompanies_MigratedId ON dbo.CompanyProfileUltimateParentHoldingCompanies (MigratedId);
                CREATE INDEX IX_CompanyProfileUltimateParentHoldingCompanies_FinancialDetailsId ON dbo.CompanyProfileUltimateParentHoldingCompanies (FinancialDetailsId);
            END;

            IF OBJECT_ID(N'dbo.CompanyFinancialDataSyncStates', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyFinancialDataSyncStates
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyFinancialDataSyncStates PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    SourceSystem NVARCHAR(100) NOT NULL,
                    SyncName NVARCHAR(100) NOT NULL,
                    LastSourceCompanyId BIGINT NULL,
                    LastStartedAt DATETIME2(3) NULL,
                    LastCompletedAt DATETIME2(3) NULL,
                    LastRunSucceeded BIT NULL,
                    LastProcessedRows INT NOT NULL CONSTRAINT DF_CompanyFinancialDataSyncStates_LastProcessedRows DEFAULT (0),
                    LastRunMessage NVARCHAR(4000) NULL
                );
                CREATE UNIQUE INDEX IX_CompanyFinancialDataSyncStates_SourceSystem_SyncName ON dbo.CompanyFinancialDataSyncStates (SourceSystem, SyncName);
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
            IF NOT EXISTS (SELECT 1 FROM dbo.CompanyFinancialDataSyncStates WHERE SourceSystem = @SourceSystem AND SyncName = @SyncName)
            BEGIN
                INSERT INTO dbo.CompanyFinancialDataSyncStates (SourceSystem, SyncName, LastRunSucceeded, LastProcessedRows, LastRunMessage)
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

    private async Task<(long? LastSourceCompanyId, DateTime? LastStartedAt, DateTime? LastCompletedAt, bool? LastRunSucceeded, int LastProcessedRows, string? LastRunMessage)> ReadStateAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT LastSourceCompanyId, LastStartedAt, LastCompletedAt, LastRunSucceeded, LastProcessedRows, LastRunMessage
            FROM dbo.CompanyFinancialDataSyncStates
            WHERE SourceSystem = @SourceSystem AND SyncName = @SyncName;
            """;
        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceSystem", SyncStateSourceSystem);
        command.Parameters.AddWithValue("@SyncName", SyncKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return (
            reader.IsDBNull(0) ? null : reader.GetInt64(0),
            reader.IsDBNull(1) ? null : reader.GetDateTime(1),
            reader.IsDBNull(2) ? null : reader.GetDateTime(2),
            reader.IsDBNull(3) ? null : reader.GetBoolean(3),
            reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
            reader.IsDBNull(5) ? null : reader.GetString(5));
    }

    private async Task<long> GetLocalRowCountAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        await using var command = localConnection.CreateCommand();
        command.CommandText = "SELECT COUNT_BIG(*) FROM dbo.CompanyProfileFinancialDetails WHERE IsDeleted = 0;";
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task MarkStartedAsync(SqlConnection localConnection, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.CompanyFinancialDataSyncStates
            SET LastSourceCompanyId = @SourceCompanyId, LastStartedAt = SYSUTCDATETIME(), LastCompletedAt = NULL, LastRunSucceeded = NULL, LastRunMessage = N'Running'
            WHERE SourceSystem = @SourceSystem AND SyncName = @SyncName;
            """;
        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceCompanyId", (object?)sourceCompanyId ?? DBNull.Value);
        command.Parameters.AddWithValue("@SourceSystem", SyncStateSourceSystem);
        command.Parameters.AddWithValue("@SyncName", SyncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkCompletedAsync(SqlConnection localConnection, long? sourceCompanyId, int processedRows, string message, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.CompanyFinancialDataSyncStates
            SET LastSourceCompanyId = @SourceCompanyId, LastCompletedAt = SYSUTCDATETIME(), LastRunSucceeded = 1, LastProcessedRows = @LastProcessedRows, LastRunMessage = @LastRunMessage
            WHERE SourceSystem = @SourceSystem AND SyncName = @SyncName;
            """;
        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceCompanyId", (object?)sourceCompanyId ?? DBNull.Value);
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
                UPDATE dbo.CompanyFinancialDataSyncStates
                SET LastSourceCompanyId = @SourceCompanyId, LastCompletedAt = SYSUTCDATETIME(), LastRunSucceeded = 0, LastRunMessage = @LastRunMessage
                WHERE SourceSystem = @SourceSystem AND SyncName = @SyncName;
                """;
            await using var command = localConnection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.AddWithValue("@SourceCompanyId", (object?)sourceCompanyId ?? DBNull.Value);
            command.Parameters.AddWithValue("@LastRunMessage", message);
            command.Parameters.AddWithValue("@SourceSystem", SyncStateSourceSystem);
            command.Parameters.AddWithValue("@SyncName", SyncKey);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
        }
    }

    private async Task<Guid> ResolveTenantIdAsync(CancellationToken cancellationToken)
    {
        if (tenantProvider.TenantId.HasValue)
        {
            return tenantProvider.TenantId.Value;
        }

        return await dbContext.Tenants.AsNoTracking().OrderBy(x => x.CreatedAt).Select(x => x.Id).FirstAsync(cancellationToken);
    }

    private string ResolveSourceObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName) || !ObjectNamePattern.IsMatch(objectName))
        {
            throw new InvalidOperationException("Company financial data sync source object name contains unsupported characters.");
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
            : _options.SourceConnectionString ?? throw new InvalidOperationException("Company financial data sync source connection string is not configured.");
    }

    private bool HasConfiguredSourceConnection()
        => !string.IsNullOrWhiteSpace(configuration.GetConnectionString(_options.SourceConnectionStringName)) || !string.IsNullOrWhiteSpace(_options.SourceConnectionString);

    private static object NormalizeDateTime(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return DBNull.Value;
        }

        var value = reader.GetDateTime(ordinal);
        return value.Date <= new DateTime(1900, 1, 1) ? DBNull.Value : value;
    }

    private static DateTimeOffset? ToUtc(DateTime? value) => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null;
}
