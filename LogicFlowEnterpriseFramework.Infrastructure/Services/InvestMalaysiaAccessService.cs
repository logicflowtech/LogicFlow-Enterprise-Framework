using System.Data;
using System.Text.RegularExpressions;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Domain.Entities;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class InvestMalaysiaAccessService(
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    ITenantProvider tenantProvider,
    IOptions<InvestMalaysiaAccessSyncOptions> options,
    ILogger<InvestMalaysiaAccessService> logger) : IInvestMalaysiaAccessService
{
    private const string SyncKey = "invest-malaysia-access";
    private const string SyncName = "InvestMalaysia User Access Sync";
    private const string SyncDescription = "Full refresh of InvestMalaysia users, groups, roles, contact persons, and access relationships into local cache tables.";
    private const string SyncStateSourceName = "invest-malaysia-access";
    private const string DetailPath = "/configuration/invest-malaysia-groups";
    private const string TargetObjectName = "[dbo].[InvestMalaysiaUsers] + [dbo].[InvestMalaysiaContactPersons] + access cache tables";
    private static readonly Regex ObjectNamePattern = new("^[A-Za-z0-9_\\.\\[\\]]+$", RegexOptions.Compiled);
    private readonly InvestMalaysiaAccessSyncOptions _options = options.Value;

    public async Task<SyncJobSummaryResponse> GetSyncSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureSyncStateRowAsync(localConnection, cancellationToken);
        return await BuildSummaryAsync(localConnection, cancellationToken);
    }

    public async Task<SyncJobSummaryResponse> RunSyncAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureSyncStateRowAsync(localConnection, cancellationToken);
        await AcquireAppLockAsync(localConnection, cancellationToken);

        try
        {
            await MarkStartedAsync(localConnection, cancellationToken);

            await using var sourceConnection = new SqlConnection(GetSourceConnectionString());
            await sourceConnection.OpenAsync(cancellationToken);

            foreach (var sourceObjectName in GetAllSourceObjectNames())
            {
                await EnsureSourceObjectExistsAsync(sourceConnection, sourceObjectName, cancellationToken);
            }

            var users = await LoadUserRowsAsync(sourceConnection, cancellationToken);
            var groups = await LoadGroupRowsAsync(sourceConnection, cancellationToken);
            var roles = await LoadRoleRowsAsync(sourceConnection, cancellationToken);
            var groupUsers = await LoadGroupUserRowsAsync(sourceConnection, cancellationToken);
            var groupRoles = await LoadGroupRoleRowsAsync(sourceConnection, cancellationToken);
            var userRoles = await LoadUserRoleRowsAsync(sourceConnection, cancellationToken);
            var contactPersons = await LoadContactPersonRowsAsync(sourceConnection, cancellationToken);

            await ReplaceCacheAsync(localConnection, users, groups, roles, groupUsers, groupRoles, userRoles, contactPersons, cancellationToken);
            await SyncCompanyAssignmentsAsync(cancellationToken);

            var totalProcessed = users.Rows.Count + groups.Rows.Count + roles.Rows.Count + groupUsers.Rows.Count + groupRoles.Rows.Count + userRoles.Rows.Count + contactPersons.Rows.Count;
            var message = $"Synced {users.Rows.Count} users, {groups.Rows.Count} groups, {roles.Rows.Count} roles, {groupUsers.Rows.Count} group-user links, {groupRoles.Rows.Count} group-role links, {userRoles.Rows.Count} direct user-role links, and {contactPersons.Rows.Count} contact persons.";

            await MarkCompletedAsync(localConnection, totalProcessed, message, cancellationToken);
            return await BuildSummaryAsync(localConnection, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "InvestMalaysia access sync failed.");
            await TryMarkFailedAsync(localConnection, exception.Message, cancellationToken);
            throw;
        }
        finally
        {
            await ReleaseAppLockAsync(localConnection, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<InvestMalaysiaGroupCatalogResponse>> GetGroupCatalogAsync(CancellationToken cancellationToken = default)
    {
        var syncedGroups = await dbContext.InvestMalaysiaGroups
            .AsNoTracking()
            .Select(group => new
            {
                group.LegacyGroupId,
                group.Name,
                UserCount = group.UserAssignments.Count,
                RoleCount = group.RoleAssignments.Count
            })
            .ToListAsync(cancellationToken);

        var mappings = await dbContext.InvestMalaysiaGroupMappings
            .AsNoTracking()
            .Include(mapping => mapping.PlatformAccessGroup)
            .ToListAsync(cancellationToken);

        var rows = new List<InvestMalaysiaGroupCatalogResponse>();
        var syncedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in syncedGroups.OrderBy(item => item.Name).ThenBy(item => item.LegacyGroupId))
        {
            var normalizedName = NormalizeGroupName(group.Name);
            syncedNames.Add(normalizedName);
            var mapping = mappings.FirstOrDefault(item => item.NormalizedInvestMalaysiaGroupName == normalizedName);

            rows.Add(ToCatalogRow(group.LegacyGroupId, group.Name, group.UserCount, group.RoleCount, true, mapping));
        }

        foreach (var mapping in mappings
                     .Where(item => !syncedNames.Contains(item.NormalizedInvestMalaysiaGroupName))
                     .OrderBy(item => item.InvestMalaysiaGroupName))
        {
            rows.Add(ToCatalogRow(null, mapping.InvestMalaysiaGroupName, 0, 0, false, mapping));
        }

        return rows
            .OrderBy(row => row.InvestMalaysiaGroupName)
            .ThenBy(row => row.LegacyGroupId ?? int.MaxValue)
            .ToArray();
    }

    public async Task<InvestMalaysiaGroupCatalogResponse> CreateGroupMappingAsync(CreateInvestMalaysiaGroupMappingRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedGroupName = NormalizeGroupName(request.InvestMalaysiaGroupName);
        if (string.IsNullOrWhiteSpace(normalizedGroupName))
        {
            throw new InvalidOperationException("InvestMalaysia group name is required.");
        }

        await EnsurePlatformAccessGroupExistsAsync(request.PlatformAccessGroupId, cancellationToken);

        var existing = await dbContext.InvestMalaysiaGroupMappings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                mapping => mapping.NormalizedInvestMalaysiaGroupName == normalizedGroupName &&
                           (!tenantProvider.TenantId.HasValue || mapping.TenantId == tenantProvider.TenantId),
                cancellationToken);

        if (existing is not null && !existing.IsDeleted)
        {
            throw new InvalidOperationException("A mapping for the same InvestMalaysia group name already exists.");
        }

        if (existing is null)
        {
            existing = new InvestMalaysiaGroupMapping();
            await dbContext.InvestMalaysiaGroupMappings.AddAsync(existing, cancellationToken);
        }
        else
        {
            RestoreEntity(existing);
        }

        existing.InvestMalaysiaGroupName = request.InvestMalaysiaGroupName.Trim();
        existing.NormalizedInvestMalaysiaGroupName = normalizedGroupName;
        existing.PlatformAccessGroupId = request.PlatformAccessGroupId;
        existing.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCatalogRowByMappingIdAsync(existing.Id, cancellationToken);
    }

    public async Task<InvestMalaysiaGroupCatalogResponse> UpdateGroupMappingAsync(Guid mappingId, UpdateInvestMalaysiaGroupMappingRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedGroupName = NormalizeGroupName(request.InvestMalaysiaGroupName);
        if (string.IsNullOrWhiteSpace(normalizedGroupName))
        {
            throw new InvalidOperationException("InvestMalaysia group name is required.");
        }

        await EnsurePlatformAccessGroupExistsAsync(request.PlatformAccessGroupId, cancellationToken);

        var mapping = await dbContext.InvestMalaysiaGroupMappings
            .FirstOrDefaultAsync(item => item.Id == mappingId, cancellationToken)
            ?? throw new InvalidOperationException("InvestMalaysia group mapping was not found.");

        var duplicateExists = await dbContext.InvestMalaysiaGroupMappings
            .AnyAsync(
                item => item.Id != mappingId &&
                        item.NormalizedInvestMalaysiaGroupName == normalizedGroupName,
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException("A mapping for the same InvestMalaysia group name already exists.");
        }

        mapping.InvestMalaysiaGroupName = request.InvestMalaysiaGroupName.Trim();
        mapping.NormalizedInvestMalaysiaGroupName = normalizedGroupName;
        mapping.PlatformAccessGroupId = request.PlatformAccessGroupId;
        mapping.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCatalogRowByMappingIdAsync(mapping.Id, cancellationToken);
    }

    public async Task DeleteGroupMappingAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        var mapping = await dbContext.InvestMalaysiaGroupMappings
            .FirstOrDefaultAsync(item => item.Id == mappingId, cancellationToken)
            ?? throw new InvalidOperationException("InvestMalaysia group mapping was not found.");

        dbContext.InvestMalaysiaGroupMappings.Remove(mapping);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private InvestMalaysiaGroupCatalogResponse ToCatalogRow(
        int? legacyGroupId,
        string groupName,
        int userCount,
        int roleCount,
        bool isDiscoveredFromSync,
        InvestMalaysiaGroupMapping? mapping)
    {
        return new InvestMalaysiaGroupCatalogResponse(
            mapping?.Id,
            legacyGroupId,
            groupName,
            userCount,
            roleCount,
            isDiscoveredFromSync,
            mapping?.PlatformAccessGroupId,
            mapping?.PlatformAccessGroup.Code,
            mapping?.PlatformAccessGroup.Name,
            mapping is not null,
            mapping?.IsActive ?? false,
            mapping?.UpdatedAt ?? mapping?.CreatedAt,
            mapping?.UpdatedBy ?? mapping?.CreatedBy);
    }

    private async Task<InvestMalaysiaGroupCatalogResponse> GetCatalogRowByMappingIdAsync(Guid mappingId, CancellationToken cancellationToken)
    {
        var rows = await GetGroupCatalogAsync(cancellationToken);
        return rows.FirstOrDefault(row => row.MappingId == mappingId)
               ?? throw new InvalidOperationException("InvestMalaysia group mapping was not found after save.");
    }

    private async Task EnsurePlatformAccessGroupExistsAsync(Guid platformAccessGroupId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.PlatformAccessGroups
            .AnyAsync(group => group.Id == platformAccessGroupId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Selected local access group was not found.");
        }
    }

    private async Task<SyncJobSummaryResponse> BuildSummaryAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        var state = await ReadStateAsync(localConnection, cancellationToken);
        var localRowCount = await GetLocalRowCountAsync(localConnection, cancellationToken);

        return new SyncJobSummaryResponse(
            SyncKey,
            SyncName,
            SyncDescription,
            BuildSourceObjectSummary(),
            TargetObjectName,
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
            DetailPath);
    }

    private async Task ReplaceCacheAsync(
        SqlConnection localConnection,
        DataTable users,
        DataTable groups,
        DataTable roles,
        DataTable groupUsers,
        DataTable groupRoles,
        DataTable userRoles,
        DataTable contactPersons,
        CancellationToken cancellationToken)
    {
        await using var transaction = (SqlTransaction)await localConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            await ExecuteNonQueryAsync(
                localConnection,
                """
                DELETE FROM dbo.InvestMalaysiaUserRoles;
                DELETE FROM dbo.InvestMalaysiaGroupRoles;
                DELETE FROM dbo.InvestMalaysiaGroupUsers;
                DELETE FROM dbo.InvestMalaysiaRoles;
                DELETE FROM dbo.InvestMalaysiaGroups;
                DELETE FROM dbo.InvestMalaysiaUsers;
                DELETE FROM dbo.InvestMalaysiaContactPersons;
                """,
                cancellationToken,
                transaction);

            await BulkCopyAsync(localConnection, "dbo.InvestMalaysiaUsers", users, cancellationToken, transaction);
            await BulkCopyAsync(localConnection, "dbo.InvestMalaysiaGroups", groups, cancellationToken, transaction);
            await BulkCopyAsync(localConnection, "dbo.InvestMalaysiaRoles", roles, cancellationToken, transaction);
            await BulkCopyAsync(localConnection, "dbo.InvestMalaysiaGroupUsers", groupUsers, cancellationToken, transaction);
            await BulkCopyAsync(localConnection, "dbo.InvestMalaysiaGroupRoles", groupRoles, cancellationToken, transaction);
            await BulkCopyAsync(localConnection, "dbo.InvestMalaysiaUserRoles", userRoles, cancellationToken, transaction);
            await BulkCopyAsync(localConnection, "dbo.InvestMalaysiaContactPersons", contactPersons, cancellationToken, transaction);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<DataTable> LoadUserRowsAsync(SqlConnection sourceConnection, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS INT) AS LegacyUserId,
                CAST(COALESCE(src.TENANT_ID, 0) AS INT) AS LegacyTenantId,
                CAST(COALESCE(src.IS_ACTIVE, 0) AS BIT) AS IsActive,
                CASE WHEN src.CREATION_DATE IS NULL OR CONVERT(date, src.CREATION_DATE) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATION_DATE) END AS SourceCreatedAt,
                CASE WHEN src.LAST_LOGIN IS NULL OR CONVERT(date, src.LAST_LOGIN) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.LAST_LOGIN) END AS LastLoginAt,
                COALESCE(NULLIF(LTRIM(RTRIM(src.[NAME])), N''), CONCAT(N'Legacy User ', CAST(src.ID AS NVARCHAR(20)))) AS Name,
                COALESCE(NULLIF(LTRIM(RTRIM(src.MobilePhone)), N''), N'') AS MobilePhone,
                COALESCE(NULLIF(LTRIM(RTRIM(src.EMAIL)), N''), N'') AS Email,
                COALESCE(NULLIF(LTRIM(RTRIM(src.USERNAME)), N''), N'') AS UserName,
                COALESCE(NULLIF(LTRIM(RTRIM(src.EXTERNAL_ID)), N''), N'') AS ExternalId,
                CAST(SYSUTCDATETIME() AS DATETIME2(3)) AS LastSyncedAt
            FROM {ResolveSourceObjectName(_options.UserSourceObjectName)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(sourceConnection, query, cancellationToken);
    }

    private async Task<DataTable> LoadGroupRowsAsync(SqlConnection sourceConnection, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS INT) AS LegacyGroupId,
                COALESCE(NULLIF(LTRIM(RTRIM(src.[NAME])), N''), CONCAT(N'Legacy Group ', CAST(src.ID AS NVARCHAR(20)))) AS Name,
                CAST(1 AS BIT) AS IsActive,
                CAST(SYSUTCDATETIME() AS DATETIME2(3)) AS LastSyncedAt
            FROM {ResolveSourceObjectName(_options.GroupSourceObjectName)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(sourceConnection, query, cancellationToken);
    }

    private async Task<DataTable> LoadRoleRowsAsync(SqlConnection sourceConnection, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS INT) AS LegacyRoleId,
                COALESCE(NULLIF(LTRIM(RTRIM(src.[NAME])), N''), CONCAT(N'Legacy Role ', CAST(src.ID AS NVARCHAR(20)))) AS Name,
                CAST(1 AS BIT) AS IsActive,
                CAST(SYSUTCDATETIME() AS DATETIME2(3)) AS LastSyncedAt
            FROM {ResolveSourceObjectName(_options.RoleSourceObjectName)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(sourceConnection, query, cancellationToken);
    }

    private async Task<DataTable> LoadGroupUserRowsAsync(SqlConnection sourceConnection, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT DISTINCT
                CAST(src.GROUP_ID AS INT) AS LegacyGroupId,
                CAST(src.USER_ID AS INT) AS LegacyUserId,
                CAST(SYSUTCDATETIME() AS DATETIME2(3)) AS LastSyncedAt
            FROM {ResolveSourceObjectName(_options.GroupUserSourceObjectName)} AS src
            WHERE src.GROUP_ID IS NOT NULL
              AND src.USER_ID IS NOT NULL
            ORDER BY CAST(src.GROUP_ID AS INT), CAST(src.USER_ID AS INT);
            """;

        return await LoadSourceRowsAsync(sourceConnection, query, cancellationToken);
    }

    private async Task<DataTable> LoadGroupRoleRowsAsync(SqlConnection sourceConnection, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT DISTINCT
                CAST(src.GROUP_ID AS INT) AS LegacyGroupId,
                CAST(src.ROLE_ID AS INT) AS LegacyRoleId,
                CAST(SYSUTCDATETIME() AS DATETIME2(3)) AS LastSyncedAt
            FROM {ResolveSourceObjectName(_options.GroupRoleSourceObjectName)} AS src
            WHERE src.GROUP_ID IS NOT NULL
              AND src.ROLE_ID IS NOT NULL
            ORDER BY CAST(src.GROUP_ID AS INT), CAST(src.ROLE_ID AS INT);
            """;

        return await LoadSourceRowsAsync(sourceConnection, query, cancellationToken);
    }

    private async Task<DataTable> LoadUserRoleRowsAsync(SqlConnection sourceConnection, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT DISTINCT
                CAST(src.USER_ID AS INT) AS LegacyUserId,
                CAST(src.ROLE_ID AS INT) AS LegacyRoleId,
                CAST(SYSUTCDATETIME() AS DATETIME2(3)) AS LastSyncedAt
            FROM {ResolveSourceObjectName(_options.UserRoleSourceObjectName)} AS src
            WHERE src.USER_ID IS NOT NULL
              AND src.ROLE_ID IS NOT NULL
            ORDER BY CAST(src.USER_ID AS INT), CAST(src.ROLE_ID AS INT);
            """;

        return await LoadSourceRowsAsync(sourceConnection, query, cancellationToken);
    }

    private async Task<DataTable> LoadContactPersonRowsAsync(SqlConnection sourceConnection, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT
                CAST(src.ID AS BIGINT) AS LegacyContactPersonId,
                COALESCE(NULLIF(LTRIM(RTRIM(src.FULLNAME)), N''), CONCAT(N'Legacy Contact Person ', CAST(src.ID AS NVARCHAR(20)))) AS FullName,
                CAST(src.USERDESIGNATION AS INT) AS UserDesignationId,
                COALESCE(NULLIF(LTRIM(RTRIM(src.EMAIL)), N''), N'') AS Email,
                COALESCE(NULLIF(LTRIM(RTRIM(src.TELEPHONENO)), N''), N'') AS TelephoneNo,
                COALESCE(NULLIF(LTRIM(RTRIM(src.FAXNO)), N''), N'') AS FaxNo,
                CAST(src.COMPANYID AS BIGINT) AS LegacyCompanyId,
                CAST(src.TEMPCONTACTPERSONID AS BIGINT) AS TempContactPersonId,
                CAST(COALESCE(src.STATUS, 0) AS BIT) AS Status,
                CAST(src.CONTACTPERSONAPPROVALSTATUS AS INT) AS ContactPersonApprovalStatus,
                CAST(src.USERID AS INT) AS LegacyUserId,
                CAST(src.CREATEDBYUSERID AS INT) AS CreatedByUserId,
                COALESCE(NULLIF(LTRIM(RTRIM(src.CREATEDBY)), N''), N'') AS CreatedBy,
                CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END AS SourceCreatedDateTime,
                CAST(src.MODIFIEDBYUSERID AS INT) AS ModifiedByUserId,
                COALESCE(NULLIF(LTRIM(RTRIM(src.MODIFIEDBY)), N''), N'') AS ModifiedBy,
                CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END AS SourceModifiedDateTime,
                CAST(src.TITLEID AS INT) AS TitleId,
                COALESCE(NULLIF(LTRIM(RTRIM(src.TITLENAME)), N''), N'') AS TitleName,
                COALESCE(NULLIF(LTRIM(RTRIM(src.OTHERDESIGNATIONNAME)), N''), N'') AS OtherDesignationName,
                CAST(SYSUTCDATETIME() AS DATETIME2(3)) AS LastSyncedAt
            FROM {ResolveSourceObjectName(_options.ContactPersonSourceObjectName)} AS src
            ORDER BY src.ID;
            """;

        return await LoadSourceRowsAsync(sourceConnection, query, cancellationToken);
    }

    private async Task<DataTable> LoadSourceRowsAsync(SqlConnection sourceConnection, string query, CancellationToken cancellationToken)
    {
        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private async Task BulkCopyAsync(
        SqlConnection localConnection,
        string tableName,
        DataTable sourceRows,
        CancellationToken cancellationToken,
        SqlTransaction transaction)
    {
        if (sourceRows.Rows.Count == 0)
        {
            return;
        }

        using var bulkCopy = new SqlBulkCopy(localConnection, SqlBulkCopyOptions.Default, transaction);
        bulkCopy.DestinationTableName = tableName;
        bulkCopy.BatchSize = _options.BatchSize;
        bulkCopy.BulkCopyTimeout = _options.CommandTimeoutSeconds;

        foreach (DataColumn column in sourceRows.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(sourceRows, cancellationToken);
    }

    private async Task ExecuteNonQueryAsync(
        SqlConnection localConnection,
        string sql,
        CancellationToken cancellationToken,
        SqlTransaction? transaction = null)
    {
        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureSyncStateRowAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.InvestMalaysiaUserSyncState WHERE SourceName = @SourceName)
            BEGIN
                INSERT INTO dbo.InvestMalaysiaUserSyncState (SourceName, LastRunSucceeded, LastProcessedRows, LastRunMessage)
                VALUES (@SourceName, NULL, 0, N'Not started');
            END;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceName", SyncStateSourceName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<InvestMalaysiaUserSyncState> ReadStateAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT LastStartedAt, LastCompletedAt, LastRunSucceeded, LastProcessedRows, LastRunMessage
            FROM dbo.InvestMalaysiaUserSyncState
            WHERE SourceName = @SourceName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceName", SyncStateSourceName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new InvestMalaysiaUserSyncState { SourceName = SyncStateSourceName };
        }

        return new InvestMalaysiaUserSyncState
        {
            SourceName = SyncStateSourceName,
            LastStartedAt = reader.IsDBNull(0) ? null : reader.GetDateTime(0),
            LastCompletedAt = reader.IsDBNull(1) ? null : reader.GetDateTime(1),
            LastRunSucceeded = reader.IsDBNull(2) ? null : reader.GetBoolean(2),
            LastProcessedRows = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            LastRunMessage = reader.IsDBNull(4) ? null : reader.GetString(4)
        };
    }

    private async Task<long> GetLocalRowCountAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                (SELECT COUNT_BIG(*) FROM dbo.InvestMalaysiaUsers) +
                (SELECT COUNT_BIG(*) FROM dbo.InvestMalaysiaGroups) +
                (SELECT COUNT_BIG(*) FROM dbo.InvestMalaysiaRoles) +
                (SELECT COUNT_BIG(*) FROM dbo.InvestMalaysiaGroupUsers) +
                (SELECT COUNT_BIG(*) FROM dbo.InvestMalaysiaGroupRoles) +
                (SELECT COUNT_BIG(*) FROM dbo.InvestMalaysiaUserRoles) +
                (SELECT COUNT_BIG(*) FROM dbo.InvestMalaysiaContactPersons) +
                (SELECT COUNT_BIG(*) FROM dbo.CompanyProfileUserAssignments WHERE LegacyContactPersonId IS NOT NULL AND IsDeleted = 0);
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task MarkStartedAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.InvestMalaysiaUserSyncState
            SET LastStartedAt = @LastStartedAt,
                LastRunSucceeded = NULL,
                LastRunMessage = N'Running full refresh'
            WHERE SourceName = @SourceName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@LastStartedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@SourceName", SyncStateSourceName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkCompletedAsync(SqlConnection localConnection, int processedRows, string message, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.InvestMalaysiaUserSyncState
            SET LastCompletedAt = @LastCompletedAt,
                LastRunSucceeded = 1,
                LastProcessedRows = @LastProcessedRows,
                LastRunMessage = @LastRunMessage
            WHERE SourceName = @SourceName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@LastCompletedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@LastProcessedRows", processedRows);
        command.Parameters.AddWithValue("@LastRunMessage", message);
        command.Parameters.AddWithValue("@SourceName", SyncStateSourceName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task TryMarkFailedAsync(SqlConnection localConnection, string message, CancellationToken cancellationToken)
    {
        try
        {
            const string sql = """
                UPDATE dbo.InvestMalaysiaUserSyncState
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
            command.Parameters.AddWithValue("@SourceName", SyncStateSourceName);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to mark InvestMalaysia access sync as failed.");
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
            $"InvestMalaysia access sync source object {sourceObjectName} was not found in source database '{sourceConnection.Database}'. " +
            $"Check ConnectionStrings:{_options.SourceConnectionStringName} or InvestMalaysiaAccessSync configuration.");
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
        command.Parameters.AddWithValue("@Resource", $"InvestMalaysiaAccessSync:{SyncStateSourceName}");

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
        {
            throw new InvalidOperationException("Unable to acquire InvestMalaysia access sync lock.");
        }
    }

    private async Task ReleaseAppLockAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = localConnection.CreateCommand();
            command.CommandText = "EXEC sp_releaseapplock @Resource = @Resource, @LockOwner = 'Session';";
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.AddWithValue("@Resource", $"InvestMalaysiaAccessSync:{SyncStateSourceName}");
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to release InvestMalaysia access sync lock.");
        }
    }

    private string BuildSourceObjectSummary()
    {
        return string.Join(
            " + ",
            ResolveSourceObjectName(_options.UserSourceObjectName),
            ResolveSourceObjectName(_options.GroupSourceObjectName),
            ResolveSourceObjectName(_options.RoleSourceObjectName),
            ResolveSourceObjectName(_options.GroupUserSourceObjectName),
            ResolveSourceObjectName(_options.GroupRoleSourceObjectName),
            ResolveSourceObjectName(_options.UserRoleSourceObjectName),
            ResolveSourceObjectName(_options.ContactPersonSourceObjectName));
    }

    private IEnumerable<string> GetAllSourceObjectNames()
    {
        yield return ResolveSourceObjectName(_options.UserSourceObjectName);
        yield return ResolveSourceObjectName(_options.GroupSourceObjectName);
        yield return ResolveSourceObjectName(_options.RoleSourceObjectName);
        yield return ResolveSourceObjectName(_options.GroupUserSourceObjectName);
        yield return ResolveSourceObjectName(_options.GroupRoleSourceObjectName);
        yield return ResolveSourceObjectName(_options.UserRoleSourceObjectName);
        yield return ResolveSourceObjectName(_options.ContactPersonSourceObjectName);
    }

    private async Task SyncCompanyAssignmentsAsync(CancellationToken cancellationToken)
    {
        var desiredAssignments = await (
            from contact in dbContext.InvestMalaysiaContactPersons.AsNoTracking()
            join user in dbContext.Users on contact.LegacyUserId equals user.LegacyUserId
            join company in dbContext.CompanyProfiles on contact.LegacyCompanyId equals company.MigratedId
            where contact.Status
            select new
            {
                ApplicationUserId = user.Id,
                CompanyProfileId = company.Id,
                contact.LegacyContactPersonId
            })
            .GroupBy(item => new { item.ApplicationUserId, item.CompanyProfileId })
            .Select(group => new
            {
                group.Key.ApplicationUserId,
                group.Key.CompanyProfileId,
                LegacyContactPersonId = group.Min(item => item.LegacyContactPersonId)
            })
            .ToListAsync(cancellationToken);

        var existingAssignments = await dbContext.CompanyProfileUserAssignments
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        var desiredKeys = desiredAssignments
            .Select(item => (item.ApplicationUserId, item.CompanyProfileId))
            .ToHashSet();

        foreach (var assignment in existingAssignments.Where(assignment =>
                     assignment.LegacyContactPersonId != null &&
                     !desiredKeys.Contains((assignment.ApplicationUserId, assignment.CompanyProfileId))))
        {
            dbContext.CompanyProfileUserAssignments.Remove(assignment);
        }

        foreach (var desired in desiredAssignments)
        {
            var existing = existingAssignments.FirstOrDefault(assignment =>
                assignment.ApplicationUserId == desired.ApplicationUserId &&
                assignment.CompanyProfileId == desired.CompanyProfileId);

            if (existing is null)
            {
                dbContext.CompanyProfileUserAssignments.Add(new CompanyProfileUserAssignment
                {
                    ApplicationUserId = desired.ApplicationUserId,
                    CompanyProfileId = desired.CompanyProfileId,
                    LegacyContactPersonId = desired.LegacyContactPersonId,
                    IsActive = true,
                    TenantId = tenantProvider.TenantId
                });
                continue;
            }

            if (existing.IsDeleted)
            {
                RestoreEntity(existing);
            }

            existing.IsActive = true;

            if (existing.LegacyContactPersonId.HasValue)
            {
                existing.LegacyContactPersonId = desired.LegacyContactPersonId;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private string ResolveSourceObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new InvalidOperationException("InvestMalaysia access sync source object name is not configured.");
        }

        if (!ObjectNamePattern.IsMatch(objectName))
        {
            throw new InvalidOperationException("InvestMalaysia access sync source object name contains unsupported characters.");
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

        throw new InvalidOperationException("InvestMalaysia access source connection string is not configured.");
    }

    private bool HasConfiguredSourceConnection()
    {
        var namedConnectionString = string.IsNullOrWhiteSpace(_options.SourceConnectionStringName)
            ? null
            : configuration.GetConnectionString(_options.SourceConnectionStringName);

        return !string.IsNullOrWhiteSpace(namedConnectionString) ||
               !string.IsNullOrWhiteSpace(_options.SourceConnectionString);
    }

    private static string NormalizeGroupName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static DateTimeOffset? ToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }

    private static void RestoreEntity(BaseEntity entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
    }
}
