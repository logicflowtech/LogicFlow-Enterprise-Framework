using System.Data;
using System.Net.Mail;
using System.Text.RegularExpressions;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Domain.Entities;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class CompanyUserSyncService(
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    ITenantProvider tenantProvider,
    UserManager<ApplicationUser> userManager,
    IOptions<CompanyUserSyncOptions> options,
    ILogger<CompanyUserSyncService> logger) : ICompanyUserSyncService
{
    private const string SyncKey = "company-users";
    private const string SyncName = "Company User Sync";
    private const string SyncDescription = "Sync users, profiles, groups, roles, and company assignments from OutSystems contact persons into framework tables.";
    private const string SyncStateSourceSystem = "outsystems";
    private const string DefaultMigratedUserPassword = "P@ssw0rd";
    private static readonly Regex ObjectNamePattern = new("^[A-Za-z0-9_\\.\\[\\]]+$", RegexOptions.Compiled);
    private static readonly Regex AllowedUserNamePattern = new("^[A-Za-z0-9\\-\\._@\\+]+$", RegexOptions.Compiled);
    private readonly CompanyUserSyncOptions _options = options.Value;

    public async Task<SyncJobSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(cancellationToken);
        return ToSummary(status);
    }

    public async Task<CompanyUserSyncStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureSyncStateTableAsync(localConnection, cancellationToken);
        await EnsureSyncStateRowAsync(localConnection, cancellationToken);

        var state = await ReadStateAsync(localConnection, cancellationToken);
        var localRowCount = await GetLocalRowCountAsync(localConnection, cancellationToken);

        return new CompanyUserSyncStatusResponse(
            _options.SourceConnectionStringName,
            HasConfiguredSourceConnection(),
            ResolveSourceObjectName(_options.ContactPersonSourceObjectName),
            ResolveSourceObjectName(_options.UserSourceObjectName),
            ResolveSourceObjectName(_options.IndividualSourceObjectName),
            ResolveSourceObjectName(_options.GroupSourceObjectName),
            ResolveSourceObjectName(_options.RoleSourceObjectName),
            _options.BatchSize,
            localRowCount,
            ToUtc(state.LastStartedAt),
            ToUtc(state.LastCompletedAt),
            state.LastRunSucceeded,
            state.LastProcessedRows,
            state.LastRunMessage,
            state.LastSourceCompanyId);
    }

    public async Task<CompanyUserSyncStatusResponse> RunSyncAsync(long? sourceCompanyId = null, CancellationToken cancellationToken = default)
    {
        await using var localConnection = new SqlConnection(GetLocalConnectionString());
        await localConnection.OpenAsync(cancellationToken);
        await EnsureSyncStateTableAsync(localConnection, cancellationToken);
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

            var users = await LoadSourceUsersAsync(sourceConnection, sourceCompanyId, cancellationToken);
            var companies = await LoadLocalCompaniesAsync(users.SelectMany(x => x.CompanyIds), cancellationToken);
            var groups = await LoadSourceGroupsAsync(sourceConnection, sourceCompanyId, cancellationToken);
            var roles = await LoadSourceRolesAsync(sourceConnection, sourceCompanyId, cancellationToken);
            var userGroups = await LoadSourceUserGroupsAsync(sourceConnection, sourceCompanyId, cancellationToken);
            var groupRoles = await LoadSourceGroupRolesAsync(sourceConnection, sourceCompanyId, cancellationToken);
            var directUserRoles = await LoadSourceDirectUserRolesAsync(sourceConnection, sourceCompanyId, cancellationToken);
            var companyAssignments = BuildDesiredCompanyAssignments(users, companies);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var tenantId = await ResolveTenantIdAsync(cancellationToken);
            var userMap = await UpsertUsersAsync(users, tenantId, cancellationToken);
            await UpsertUserProfilesAsync(users, userMap, cancellationToken);
            var roleMap = await UpsertRolesAsync(roles, tenantId, cancellationToken);
            var groupMap = await UpsertGroupsAsync(groups, roles, directUserRoles, tenantId, cancellationToken);
            await SyncGroupRoleAssignmentsAsync(groupRoles, directUserRoles, groupMap, roleMap, tenantId, cancellationToken);
            await SyncUserGroupAssignmentsAsync(users, userGroups, directUserRoles, userMap, groupMap, tenantId, cancellationToken);
            await SyncCompanyAssignmentsAsync(users, companyAssignments, userMap, companies, tenantId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var processedRows = users.Count + groups.Count + roles.Count + userGroups.Count + groupRoles.Count + directUserRoles.Count + companyAssignments.Count;
            var message = BuildCompletedMessage(users.Count, groups.Count, roles.Count, userGroups.Count, groupRoles.Count, directUserRoles.Count, companyAssignments.Count, sourceCompanyId);
            await MarkCompletedAsync(localConnection, sourceCompanyId, processedRows, message, cancellationToken);

            return await GetStatusAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Company user sync failed for source company id {SourceCompanyId}.", sourceCompanyId);
            await TryMarkFailedAsync(localConnection, sourceCompanyId, exception.Message, cancellationToken);
            throw;
        }
        finally
        {
            await ReleaseAppLockAsync(localConnection, cancellationToken);
        }
    }

    private async Task<List<SourceUserRow>> LoadSourceUsersAsync(SqlConnection sourceConnection, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        var query = $"""
            WITH TargetContacts AS
            (
                SELECT
                    CAST(cp.ID AS BIGINT) AS LegacyContactPersonId,
                    CAST(cp.COMPANYID AS BIGINT) AS LegacyCompanyId,
                    CAST(cp.USERID AS INT) AS LegacyUserId,
                    CAST(COALESCE(cp.STATUS, 0) AS BIT) AS ContactIsActive,
                    CAST(cp.TITLEID AS INT) AS LegacyTitleId,
                    COALESCE(NULLIF(LTRIM(RTRIM(cp.TITLENAME)), N''), N'') AS TitleDisplayName,
                    COALESCE(NULLIF(LTRIM(RTRIM(cp.OTHERDESIGNATIONNAME)), N''), N'') AS CustomDesignationName,
                    COALESCE(NULLIF(LTRIM(RTRIM(cp.FAXNO)), N''), N'') AS FaxNumber,
                    CASE WHEN cp.CREATEDDATETIME IS NULL OR CONVERT(date, cp.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), cp.CREATEDDATETIME) END AS SourceCreatedAt,
                    CASE WHEN cp.MODIFIEDDATETIME IS NULL OR CONVERT(date, cp.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), cp.MODIFIEDDATETIME) END AS SourceUpdatedAt,
                    CAST(cp.CREATEDBYUSERID AS INT) AS SourceCreatedByLegacyUserId,
                    CAST(cp.MODIFIEDBYUSERID AS INT) AS SourceUpdatedByLegacyUserId
                FROM {ResolveSourceObjectName(_options.ContactPersonSourceObjectName)} cp
                WHERE cp.USERID IS NOT NULL
                  AND cp.COMPANYID IS NOT NULL
                  AND (@SourceCompanyId IS NULL OR cp.COMPANYID = @SourceCompanyId)
            ),
            CanonicalContact AS
            (
                SELECT *,
                       ROW_NUMBER() OVER (
                           PARTITION BY LegacyUserId
                           ORDER BY COALESCE(SourceUpdatedAt, SourceCreatedAt) DESC, LegacyContactPersonId DESC) AS RowNumber
                FROM TargetContacts
            )
            SELECT
                CAST(u.ID AS INT) AS LegacyUserId,
                COALESCE(NULLIF(LTRIM(RTRIM(u.[NAME])), N''), CONCAT(N'Legacy User ', CAST(u.ID AS NVARCHAR(20)))) AS FullName,
                COALESCE(NULLIF(LTRIM(RTRIM(u.EMAIL)), N''), N'') AS Email,
                COALESCE(NULLIF(LTRIM(RTRIM(u.USERNAME)), N''), N'') AS SourceUserName,
                COALESCE(NULLIF(LTRIM(RTRIM(u.MobilePhone)), N''), N'') AS PhoneNumber,
                CAST(COALESCE(u.IS_ACTIVE, 0) AS BIT) AS UserIsActive,
                CASE WHEN u.LAST_LOGIN IS NULL OR CONVERT(date, u.LAST_LOGIN) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), u.LAST_LOGIN) END AS LastLoginAt,
                COALESCE(NULLIF(LTRIM(RTRIM(ind.NRIC)), N''), N'') AS Nric,
                COALESCE(NULLIF(LTRIM(RTRIM(ind.PASSPORTNO)), N''), N'') AS PassportNumber,
                CAST(ind.IDENTIFICATIONTYPEID AS INT) AS LegacyIdentificationTypeId,
                CAST(ind.ADDRESSID AS BIGINT) AS LegacyAddressId,
                CAST(cc.LegacyCompanyId AS BIGINT) AS LegacyCompanyId,
                CAST(cc.LegacyContactPersonId AS BIGINT) AS LegacyContactPersonId,
                CAST(cc.ContactIsActive AS BIT) AS ContactIsActive,
                CAST(cc.LegacyTitleId AS INT) AS LegacyTitleId,
                cc.TitleDisplayName,
                CAST(ind.USERDESIGNATIONID AS INT) AS LegacyDesignationId,
                COALESCE(NULLIF(LTRIM(RTRIM(ind.OTHERDESIGNATIONNAME)), N''), cc.CustomDesignationName, N'') AS CustomDesignationName,
                COALESCE(NULLIF(LTRIM(RTRIM(ind.FAXNO)), N''), cc.FaxNumber, N'') AS FaxNumber,
                CAST(cc.SourceCreatedByLegacyUserId AS INT) AS SourceCreatedByLegacyUserId,
                cc.SourceCreatedAt,
                CAST(cc.SourceUpdatedByLegacyUserId AS INT) AS SourceUpdatedByLegacyUserId,
                cc.SourceUpdatedAt
            FROM {ResolveSourceObjectName(_options.UserSourceObjectName)} u
            INNER JOIN CanonicalContact cc
                ON cc.LegacyUserId = u.ID
               AND cc.RowNumber = 1
            LEFT JOIN {ResolveSourceObjectName(_options.IndividualSourceObjectName)} ind
                ON ind.ID = u.ID
            ORDER BY u.ID;
            """;

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@SourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<SourceUserRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SourceUserRow(
                reader.GetInt32(0),
                reader.GetString(1),
                GetNullableString(reader, 2),
                GetNullableString(reader, 3),
                GetNullableString(reader, 4),
                reader.GetBoolean(5),
                GetNullableDateTime(reader, 6),
                GetNullableString(reader, 7),
                GetNullableString(reader, 8),
                GetNullableInt32(reader, 9),
                GetNullableInt64(reader, 10),
                [reader.GetInt64(11)],
                [new SourceCompanyAssignmentRow(reader.GetInt64(11), reader.GetInt64(12), reader.GetBoolean(13))],
                GetNullableInt32(reader, 14),
                GetNullableString(reader, 15),
                GetNullableInt32(reader, 16),
                GetNullableString(reader, 17),
                GetNullableString(reader, 18),
                GetNullableInt32(reader, 19),
                GetNullableDateTime(reader, 20),
                GetNullableInt32(reader, 21),
                GetNullableDateTime(reader, 22)));
        }

        return rows
            .GroupBy(row => row.LegacyUserId)
            .Select(group =>
            {
                var first = group.First();
                return first with
                {
                    CompanyIds = group.SelectMany(item => item.CompanyIds).Distinct().OrderBy(id => id).ToArray(),
                    CompanyAssignments = group.SelectMany(item => item.CompanyAssignments)
                        .GroupBy(item => new { item.LegacyCompanyId, item.LegacyContactPersonId })
                        .Select(item => item.First())
                        .OrderBy(item => item.LegacyCompanyId)
                        .ThenBy(item => item.LegacyContactPersonId)
                        .ToArray()
                };
            })
            .ToList();
    }

    private async Task<Dictionary<long, Guid>> LoadLocalCompaniesAsync(IEnumerable<long> sourceCompanyIds, CancellationToken cancellationToken)
    {
        var companyIds = sourceCompanyIds.Distinct().ToArray();
        if (companyIds.Length == 0)
        {
            return [];
        }

        return await dbContext.CompanyProfiles
            .AsNoTracking()
            .Where(company => company.MigratedId.HasValue && companyIds.Contains(company.MigratedId.Value))
            .ToDictionaryAsync(company => company.MigratedId!.Value, company => company.Id, cancellationToken);
    }

    private async Task<List<SourceGroupRow>> LoadSourceGroupsAsync(SqlConnection sourceConnection, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT DISTINCT
                CAST(g.ID AS INT) AS LegacyGroupId,
                COALESCE(NULLIF(LTRIM(RTRIM(g.[NAME])), N''), CONCAT(N'Legacy Group ', CAST(g.ID AS NVARCHAR(20)))) AS GroupName
            FROM {ResolveSourceObjectName(_options.GroupSourceObjectName)} g
            INNER JOIN {ResolveSourceObjectName(_options.GroupUserSourceObjectName)} gu
                ON gu.GROUP_ID = g.ID
            INNER JOIN {ResolveSourceObjectName(_options.ContactPersonSourceObjectName)} cp
                ON cp.USERID = gu.USER_ID
            WHERE cp.USERID IS NOT NULL
              AND cp.COMPANYID IS NOT NULL
              AND (@SourceCompanyId IS NULL OR cp.COMPANYID = @SourceCompanyId)
            ORDER BY LegacyGroupId;
            """;

        return await LoadGroupsAsync(sourceConnection, query, sourceCompanyId, cancellationToken);
    }

    private async Task<List<SourceRoleRow>> LoadSourceRolesAsync(SqlConnection sourceConnection, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        var query = $"""
            WITH TargetUsers AS
            (
                SELECT DISTINCT CAST(cp.USERID AS INT) AS LegacyUserId
                FROM {ResolveSourceObjectName(_options.ContactPersonSourceObjectName)} cp
                WHERE cp.USERID IS NOT NULL
                  AND cp.COMPANYID IS NOT NULL
                  AND (@SourceCompanyId IS NULL OR cp.COMPANYID = @SourceCompanyId)
            ),
            GroupRoles AS
            (
                SELECT DISTINCT CAST(gr.ROLE_ID AS INT) AS LegacyRoleId
                FROM {ResolveSourceObjectName(_options.GroupRoleSourceObjectName)} gr
                INNER JOIN {ResolveSourceObjectName(_options.GroupUserSourceObjectName)} gu
                    ON gu.GROUP_ID = gr.GROUP_ID
                INNER JOIN TargetUsers tu
                    ON tu.LegacyUserId = gu.USER_ID
                WHERE gr.ROLE_ID IS NOT NULL
            ),
            DirectRoles AS
            (
                SELECT DISTINCT CAST(ur.ROLE_ID AS INT) AS LegacyRoleId
                FROM {ResolveSourceObjectName(_options.UserRoleSourceObjectName)} ur
                INNER JOIN TargetUsers tu
                    ON tu.LegacyUserId = ur.USER_ID
                WHERE ur.ROLE_ID IS NOT NULL
            )
            SELECT DISTINCT
                CAST(r.ID AS INT) AS LegacyRoleId,
                COALESCE(NULLIF(LTRIM(RTRIM(r.[NAME])), N''), CONCAT(N'Legacy Role ', CAST(r.ID AS NVARCHAR(20)))) AS RoleName
            FROM {ResolveSourceObjectName(_options.RoleSourceObjectName)} r
            WHERE r.ID IN
            (
                SELECT LegacyRoleId FROM GroupRoles
                UNION
                SELECT LegacyRoleId FROM DirectRoles
            )
            ORDER BY LegacyRoleId;
            """;

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@SourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<SourceRoleRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SourceRoleRow(reader.GetInt32(0), reader.GetString(1)));
        }

        return rows;
    }

    private async Task<List<SourceUserGroupRow>> LoadSourceUserGroupsAsync(SqlConnection sourceConnection, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT DISTINCT
                CAST(gu.USER_ID AS INT) AS LegacyUserId,
                CAST(gu.GROUP_ID AS INT) AS LegacyGroupId
            FROM {ResolveSourceObjectName(_options.GroupUserSourceObjectName)} gu
            INNER JOIN {ResolveSourceObjectName(_options.ContactPersonSourceObjectName)} cp
                ON cp.USERID = gu.USER_ID
            WHERE gu.USER_ID IS NOT NULL
              AND gu.GROUP_ID IS NOT NULL
              AND cp.COMPANYID IS NOT NULL
              AND (@SourceCompanyId IS NULL OR cp.COMPANYID = @SourceCompanyId)
            ORDER BY LegacyUserId, LegacyGroupId;
            """;

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@SourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<SourceUserGroupRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SourceUserGroupRow(reader.GetInt32(0), reader.GetInt32(1)));
        }

        return rows;
    }

    private async Task<List<SourceGroupRoleRow>> LoadSourceGroupRolesAsync(SqlConnection sourceConnection, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT DISTINCT
                CAST(gr.GROUP_ID AS INT) AS LegacyGroupId,
                CAST(gr.ROLE_ID AS INT) AS LegacyRoleId
            FROM {ResolveSourceObjectName(_options.GroupRoleSourceObjectName)} gr
            INNER JOIN {ResolveSourceObjectName(_options.GroupUserSourceObjectName)} gu
                ON gu.GROUP_ID = gr.GROUP_ID
            INNER JOIN {ResolveSourceObjectName(_options.ContactPersonSourceObjectName)} cp
                ON cp.USERID = gu.USER_ID
            WHERE gr.GROUP_ID IS NOT NULL
              AND gr.ROLE_ID IS NOT NULL
              AND cp.COMPANYID IS NOT NULL
              AND (@SourceCompanyId IS NULL OR cp.COMPANYID = @SourceCompanyId)
            ORDER BY LegacyGroupId, LegacyRoleId;
            """;

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@SourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<SourceGroupRoleRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SourceGroupRoleRow(reader.GetInt32(0), reader.GetInt32(1)));
        }

        return rows;
    }

    private async Task<List<SourceDirectUserRoleRow>> LoadSourceDirectUserRolesAsync(SqlConnection sourceConnection, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        var query = $"""
            SELECT DISTINCT
                CAST(ur.USER_ID AS INT) AS LegacyUserId,
                CAST(ur.ROLE_ID AS INT) AS LegacyRoleId
            FROM {ResolveSourceObjectName(_options.UserRoleSourceObjectName)} ur
            INNER JOIN {ResolveSourceObjectName(_options.ContactPersonSourceObjectName)} cp
                ON cp.USERID = ur.USER_ID
            WHERE ur.USER_ID IS NOT NULL
              AND ur.ROLE_ID IS NOT NULL
              AND cp.COMPANYID IS NOT NULL
              AND (@SourceCompanyId IS NULL OR cp.COMPANYID = @SourceCompanyId)
            ORDER BY LegacyUserId, LegacyRoleId;
            """;

        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@SourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<SourceDirectUserRoleRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SourceDirectUserRoleRow(reader.GetInt32(0), reader.GetInt32(1)));
        }

        return rows;
    }

    private static List<DesiredCompanyAssignment> BuildDesiredCompanyAssignments(IEnumerable<SourceUserRow> users, IReadOnlyDictionary<long, Guid> companyMap)
    {
        return users
            .SelectMany(user => user.CompanyAssignments.Select(assignment => new { user.LegacyUserId, Assignment = assignment }))
            .Where(item => item.Assignment.IsActive && companyMap.ContainsKey(item.Assignment.LegacyCompanyId))
            .GroupBy(item => new { item.LegacyUserId, item.Assignment.LegacyCompanyId })
            .Select(group => new DesiredCompanyAssignment(
                group.Key.LegacyUserId,
                companyMap[group.Key.LegacyCompanyId],
                group.Min(item => item.Assignment.LegacyContactPersonId)))
            .ToList();
    }

    private async Task<Dictionary<int, Guid>> UpsertUsersAsync(IReadOnlyCollection<SourceUserRow> users, Guid tenantId, CancellationToken cancellationToken)
    {
        var legacyUserIds = users.Select(user => user.LegacyUserId).ToArray();
        var existingUsers = await dbContext.Users
            .Where(user => user.LegacyUserId.HasValue && legacyUserIds.Contains(user.LegacyUserId.Value))
            .ToListAsync(cancellationToken);

        var existingByLegacyId = existingUsers.ToDictionary(user => user.LegacyUserId!.Value);
        var existingByEmail = (await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Email != null)
                .Select(user => new { user.Email, user.Id })
                .ToListAsync(cancellationToken))
            .GroupBy(user => user.Email!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);

        foreach (var sourceUser in users)
        {
            var preferredEmail = NormalizeEmail(sourceUser.Email);
            var resolvedEmail = ResolveUserEmail(sourceUser.LegacyUserId, preferredEmail, existingByLegacyId.GetValueOrDefault(sourceUser.LegacyUserId), existingByEmail);
            var resolvedUserName = ResolveUserName(sourceUser.LegacyUserId, sourceUser.SourceUserName);

            if (!existingByLegacyId.TryGetValue(sourceUser.LegacyUserId, out var user))
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    LegacyUserId = sourceUser.LegacyUserId,
                    FullName = sourceUser.FullName,
                    Email = resolvedEmail,
                    UserName = resolvedUserName,
                    PhoneNumber = NormalizeNullable(sourceUser.PhoneNumber),
                    IsActive = sourceUser.UserIsActive,
                    LastLoginAt = sourceUser.LastLoginAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(sourceUser.LastLoginAt.Value, DateTimeKind.Utc)) : null,
                    EmailConfirmed = !string.IsNullOrWhiteSpace(resolvedEmail),
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = SyncKey
                };

                var createResult = await userManager.CreateAsync(user, DefaultMigratedUserPassword);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Unable to create migrated user {sourceUser.LegacyUserId}: {string.Join("; ", createResult.Errors.Select(x => x.Description))}");
                }

                existingByLegacyId[sourceUser.LegacyUserId] = user;
                existingByEmail[user.Email ?? string.Empty] = user.Id;
                continue;
            }

            user.TenantId = tenantId;
            user.FullName = sourceUser.FullName;
            user.Email = resolvedEmail;
            user.UserName = resolvedUserName;
            user.PhoneNumber = NormalizeNullable(sourceUser.PhoneNumber);
            user.IsActive = sourceUser.UserIsActive;
            user.EmailConfirmed = !string.IsNullOrWhiteSpace(resolvedEmail);
            user.LastLoginAt = sourceUser.LastLoginAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(sourceUser.LastLoginAt.Value, DateTimeKind.Utc)) : null;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException($"Unable to update migrated user {sourceUser.LegacyUserId}: {string.Join("; ", updateResult.Errors.Select(x => x.Description))}");
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                existingByEmail[user.Email] = user.Id;
            }
        }

        return existingByLegacyId.ToDictionary(item => item.Key, item => item.Value.Id);
    }

    private async Task UpsertUserProfilesAsync(IReadOnlyCollection<SourceUserRow> users, IReadOnlyDictionary<int, Guid> userMap, CancellationToken cancellationToken)
    {
        var applicationUserIds = userMap.Values.ToArray();
        var existingProfiles = await dbContext.UserProfiles
            .Where(profile => applicationUserIds.Contains(profile.ApplicationUserId))
            .ToDictionaryAsync(profile => profile.ApplicationUserId, cancellationToken);

        foreach (var sourceUser in users)
        {
            var applicationUserId = userMap[sourceUser.LegacyUserId];
            if (!existingProfiles.TryGetValue(applicationUserId, out var profile))
            {
                profile = new UserProfile
                {
                    ApplicationUserId = applicationUserId
                };
                await dbContext.UserProfiles.AddAsync(profile, cancellationToken);
                existingProfiles[applicationUserId] = profile;
            }

            profile.Nric = NormalizeNullable(sourceUser.Nric);
            profile.PassportNumber = NormalizeNullable(sourceUser.PassportNumber);
            profile.LegacyIdentificationTypeId = sourceUser.LegacyIdentificationTypeId;
            profile.LegacyAddressId = sourceUser.LegacyAddressId;
            profile.LegacyDesignationId = sourceUser.LegacyDesignationId;
            profile.LegacyTitleId = sourceUser.LegacyTitleId;
            profile.TitleDisplayName = NormalizeNullable(sourceUser.TitleDisplayName);
            profile.CustomDesignationName = NormalizeNullable(sourceUser.CustomDesignationName);
            profile.FaxNumber = NormalizeNullable(sourceUser.FaxNumber);
            profile.SourceCreatedByLegacyUserId = sourceUser.SourceCreatedByLegacyUserId;
            profile.SourceCreatedAt = sourceUser.SourceCreatedAt;
            profile.SourceUpdatedByLegacyUserId = sourceUser.SourceUpdatedByLegacyUserId;
            profile.SourceUpdatedAt = sourceUser.SourceUpdatedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<int, Guid>> UpsertRolesAsync(IReadOnlyCollection<SourceRoleRow> roles, Guid tenantId, CancellationToken cancellationToken)
    {
        var roleCodes = roles.Select(role => BuildRoleCode(role.LegacyRoleId)).ToArray();
        var existingRoles = await dbContext.PlatformAccessRoles
            .IgnoreQueryFilters()
            .Where(role => roleCodes.Contains(role.Code))
            .ToListAsync(cancellationToken);

        var roleMap = new Dictionary<int, Guid>();
        foreach (var sourceRole in roles)
        {
            var code = BuildRoleCode(sourceRole.LegacyRoleId);
            var existing = existingRoles.FirstOrDefault(role => role.Code == code);
            if (existing is null)
            {
                existing = new PlatformAccessRole
                {
                    TenantId = tenantId,
                    Code = code
                };
                await dbContext.PlatformAccessRoles.AddAsync(existing, cancellationToken);
                existingRoles.Add(existing);
            }
            else if (existing.IsDeleted)
            {
                RestoreEntity(existing);
            }

            existing.Name = sourceRole.Name;
            existing.Description = $"Migrated source role {sourceRole.LegacyRoleId}.";
            existing.IsSystem = false;
            existing.IsActive = true;
            roleMap[sourceRole.LegacyRoleId] = existing.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return roleMap;
    }

    private async Task<Dictionary<string, Guid>> UpsertGroupsAsync(
        IReadOnlyCollection<SourceGroupRow> groups,
        IReadOnlyCollection<SourceRoleRow> roles,
        IReadOnlyCollection<SourceDirectUserRoleRow> directUserRoles,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var directRoleIds = directUserRoles.Select(item => item.LegacyRoleId).Distinct().ToHashSet();
        var desiredGroups = groups
            .Select(group => new DesiredGroup(BuildSourceGroupCode(group.LegacyGroupId), group.Name, $"Migrated source group {group.LegacyGroupId}."))
            .Concat(roles.Where(role => directRoleIds.Contains(role.LegacyRoleId))
                .Select(role => new DesiredGroup(BuildDirectRoleGroupCode(role.LegacyRoleId), $"{role.Name} (Direct Assignment)", $"Synthetic group for direct source role {role.LegacyRoleId}.")))
            .ToList();

        var groupCodes = desiredGroups.Select(group => group.Code).ToArray();
        var existingGroups = await dbContext.PlatformAccessGroups
            .IgnoreQueryFilters()
            .Where(group => groupCodes.Contains(group.Code))
            .ToListAsync(cancellationToken);

        var groupMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var desiredGroup in desiredGroups)
        {
            var existing = existingGroups.FirstOrDefault(group => group.Code == desiredGroup.Code);
            if (existing is null)
            {
                existing = new PlatformAccessGroup
                {
                    TenantId = tenantId,
                    Code = desiredGroup.Code
                };
                await dbContext.PlatformAccessGroups.AddAsync(existing, cancellationToken);
                existingGroups.Add(existing);
            }
            else if (existing.IsDeleted)
            {
                RestoreEntity(existing);
            }

            existing.Name = desiredGroup.Name;
            existing.Description = desiredGroup.Description;
            existing.IsSystem = false;
            existing.IsActive = true;
            groupMap[desiredGroup.Code] = existing.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return groupMap;
    }

    private async Task SyncGroupRoleAssignmentsAsync(
        IReadOnlyCollection<SourceGroupRoleRow> groupRoles,
        IReadOnlyCollection<SourceDirectUserRoleRow> directUserRoles,
        IReadOnlyDictionary<string, Guid> groupMap,
        IReadOnlyDictionary<int, Guid> roleMap,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var desiredLinks = groupRoles
            .Select(link => new DesiredGroupRoleAssignment(BuildSourceGroupCode(link.LegacyGroupId), link.LegacyRoleId))
            .Concat(directUserRoles
                .Select(link => new DesiredGroupRoleAssignment(BuildDirectRoleGroupCode(link.LegacyRoleId), link.LegacyRoleId)))
            .Distinct()
            .ToList();

        var managedGroupIds = groupMap.Values.Distinct().ToArray();

        var existingLinks = await dbContext.GroupAccessRoleAssignments
            .IgnoreQueryFilters()
            .Where(link => managedGroupIds.Contains(link.PlatformAccessGroupId))
            .ToListAsync(cancellationToken);

        var desiredPairs = desiredLinks
            .Select(link => (PlatformAccessGroupId: groupMap[link.GroupCode], PlatformAccessRoleId: roleMap[link.LegacyRoleId]))
            .ToHashSet();

        foreach (var existing in existingLinks.Where(link => !desiredPairs.Contains((link.PlatformAccessGroupId, link.PlatformAccessRoleId))))
        {
            dbContext.GroupAccessRoleAssignments.Remove(existing);
        }

        foreach (var desired in desiredPairs)
        {
            var existing = existingLinks.FirstOrDefault(link =>
                link.PlatformAccessGroupId == desired.PlatformAccessGroupId &&
                link.PlatformAccessRoleId == desired.PlatformAccessRoleId);

            if (existing is null)
            {
                dbContext.GroupAccessRoleAssignments.Add(new GroupAccessRoleAssignment
                {
                    TenantId = tenantId,
                    PlatformAccessGroupId = desired.PlatformAccessGroupId,
                    PlatformAccessRoleId = desired.PlatformAccessRoleId,
                    IsEnabled = true
                });
                continue;
            }

            if (existing.IsDeleted)
            {
                RestoreEntity(existing);
            }

            existing.IsEnabled = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncUserGroupAssignmentsAsync(
        IReadOnlyCollection<SourceUserRow> users,
        IReadOnlyCollection<SourceUserGroupRow> userGroups,
        IReadOnlyCollection<SourceDirectUserRoleRow> directUserRoles,
        IReadOnlyDictionary<int, Guid> userMap,
        IReadOnlyDictionary<string, Guid> groupMap,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var desiredAssignments = userGroups
            .Select(link => new DesiredUserGroupAssignment(link.LegacyUserId, BuildSourceGroupCode(link.LegacyGroupId)))
            .Concat(directUserRoles.Select(link => new DesiredUserGroupAssignment(link.LegacyUserId, BuildDirectRoleGroupCode(link.LegacyRoleId))))
            .Distinct()
            .Where(link => userMap.ContainsKey(link.LegacyUserId))
            .ToList();

        var touchedUserIds = users
            .Where(user => userMap.ContainsKey(user.LegacyUserId))
            .Select(user => userMap[user.LegacyUserId])
            .Distinct()
            .ToArray();

        var existingAssignments = await dbContext.UserAccessGroupAssignments
            .IgnoreQueryFilters()
            .Where(assignment => touchedUserIds.Contains(assignment.ApplicationUserId))
            .Include(assignment => assignment.PlatformAccessGroup)
            .ToListAsync(cancellationToken);

        var desiredPairs = desiredAssignments
            .Select(link => (ApplicationUserId: userMap[link.LegacyUserId], PlatformAccessGroupId: groupMap[link.GroupCode]))
            .ToHashSet();

        foreach (var existing in existingAssignments.Where(assignment =>
                     IsManagedGroupCode(assignment.PlatformAccessGroup.Code) &&
                     !desiredPairs.Contains((assignment.ApplicationUserId, assignment.PlatformAccessGroupId))))
        {
            dbContext.UserAccessGroupAssignments.Remove(existing);
        }

        foreach (var desired in desiredPairs)
        {
            var existing = existingAssignments.FirstOrDefault(assignment =>
                assignment.ApplicationUserId == desired.ApplicationUserId &&
                assignment.PlatformAccessGroupId == desired.PlatformAccessGroupId);

            if (existing is null)
            {
                dbContext.UserAccessGroupAssignments.Add(new UserAccessGroupAssignment
                {
                    TenantId = tenantId,
                    ApplicationUserId = desired.ApplicationUserId,
                    PlatformAccessGroupId = desired.PlatformAccessGroupId,
                    IsEnabled = true
                });
                continue;
            }

            if (existing.IsDeleted)
            {
                RestoreEntity(existing);
            }

            existing.IsEnabled = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncCompanyAssignmentsAsync(
        IReadOnlyCollection<SourceUserRow> users,
        IReadOnlyCollection<DesiredCompanyAssignment> desiredAssignments,
        IReadOnlyDictionary<int, Guid> userMap,
        IReadOnlyDictionary<long, Guid> companies,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var desired = desiredAssignments
            .Where(item => userMap.ContainsKey(item.LegacyUserId))
            .Select(item => new
            {
                ApplicationUserId = userMap[item.LegacyUserId],
                item.CompanyProfileId,
                item.LegacyContactPersonId
            })
            .ToList();

        var touchedCompanyIds = users
            .SelectMany(user => user.CompanyAssignments)
            .Select(assignment => TryGetCompanyId(companies, assignment.LegacyCompanyId))
            .Where(companyProfileId => companyProfileId.HasValue)
            .Select(companyProfileId => companyProfileId!.Value)
            .Distinct()
            .ToArray();
        var existingAssignments = await dbContext.CompanyProfileUserAssignments
            .IgnoreQueryFilters()
            .Where(assignment => assignment.LegacyContactPersonId != null && touchedCompanyIds.Contains(assignment.CompanyProfileId))
            .ToListAsync(cancellationToken);

        var desiredKeys = desired
            .Select(item => (item.ApplicationUserId, item.CompanyProfileId))
            .ToHashSet();

        foreach (var existing in existingAssignments.Where(assignment => !desiredKeys.Contains((assignment.ApplicationUserId, assignment.CompanyProfileId))))
        {
            dbContext.CompanyProfileUserAssignments.Remove(existing);
        }

        foreach (var item in desired)
        {
            var existing = existingAssignments.FirstOrDefault(assignment =>
                assignment.ApplicationUserId == item.ApplicationUserId &&
                assignment.CompanyProfileId == item.CompanyProfileId);

            if (existing is null)
            {
                dbContext.CompanyProfileUserAssignments.Add(new CompanyProfileUserAssignment
                {
                    TenantId = tenantId,
                    ApplicationUserId = item.ApplicationUserId,
                    CompanyProfileId = item.CompanyProfileId,
                    LegacyContactPersonId = item.LegacyContactPersonId,
                    IsActive = true
                });
                continue;
            }

            if (existing.IsDeleted)
            {
                RestoreEntity(existing);
            }

            existing.IsActive = true;
            existing.LegacyContactPersonId = item.LegacyContactPersonId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<SourceGroupRow>> LoadGroupsAsync(SqlConnection sourceConnection, string query, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        await using var command = sourceConnection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@SourceCompanyId", SqlDbType.BigInt) { Value = (object?)sourceCompanyId ?? DBNull.Value });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<SourceGroupRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SourceGroupRow(reader.GetInt32(0), reader.GetString(1)));
        }

        return rows;
    }

    private async Task EnsureSyncStateTableAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.CompanyUserSyncStates', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CompanyUserSyncStates
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyUserSyncStates PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
                    SourceSystem NVARCHAR(50) NOT NULL,
                    SyncName NVARCHAR(100) NOT NULL,
                    LastSourceCompanyId BIGINT NULL,
                    LastStartedAt DATETIME2(3) NULL,
                    LastCompletedAt DATETIME2(3) NULL,
                    LastRunSucceeded BIT NULL,
                    LastProcessedRows INT NOT NULL CONSTRAINT DF_CompanyUserSyncStates_LastProcessedRows DEFAULT (0),
                    LastRunMessage NVARCHAR(4000) NULL
                );

                CREATE UNIQUE INDEX IX_CompanyUserSyncStates_SourceSystem_SyncName
                    ON dbo.CompanyUserSyncStates (SourceSystem, SyncName);
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
                FROM dbo.CompanyUserSyncStates
                WHERE SourceSystem = @SourceSystem
                  AND SyncName = @SyncName
            )
            BEGIN
                INSERT INTO dbo.CompanyUserSyncStates (SourceSystem, SyncName, LastRunSucceeded, LastProcessedRows, LastRunMessage)
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

    private async Task<CompanyUserSyncStateRow> ReadStateAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT LastSourceCompanyId, LastStartedAt, LastCompletedAt, LastRunSucceeded, LastProcessedRows, LastRunMessage
            FROM dbo.CompanyUserSyncStates
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
            return new CompanyUserSyncStateRow(null, null, null, null, 0, "Not started");
        }

        return new CompanyUserSyncStateRow(
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
                (SELECT COUNT_BIG(*) FROM dbo.AspNetUsers WHERE LegacyUserId IS NOT NULL) +
                (SELECT COUNT_BIG(*) FROM dbo.UserProfiles) +
                (SELECT COUNT_BIG(*) FROM dbo.PlatformAccessGroups WHERE Code LIKE N'OS-GROUP-%' OR Code LIKE N'OS-DIRECT-ROLE-%') +
                (SELECT COUNT_BIG(*) FROM dbo.PlatformAccessRoles WHERE Code LIKE N'OS-ROLE-%') +
                (SELECT COUNT_BIG(*) FROM dbo.UserAccessGroupAssignments WHERE IsDeleted = 0) +
                (SELECT COUNT_BIG(*) FROM dbo.GroupAccessRoleAssignments WHERE IsDeleted = 0) +
                (SELECT COUNT_BIG(*) FROM dbo.CompanyProfileUserAssignments WHERE LegacyContactPersonId IS NOT NULL AND IsDeleted = 0);
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task MarkStartedAsync(SqlConnection localConnection, long? sourceCompanyId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.CompanyUserSyncStates
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
        command.Parameters.AddWithValue("@LastRunMessage", sourceCompanyId.HasValue ? $"Running sync for source company {sourceCompanyId.Value}." : "Running full company user sync.");
        command.Parameters.AddWithValue("@SourceSystem", SyncStateSourceSystem);
        command.Parameters.AddWithValue("@SyncName", SyncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkCompletedAsync(SqlConnection localConnection, long? sourceCompanyId, int processedRows, string message, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.CompanyUserSyncStates
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
                UPDATE dbo.CompanyUserSyncStates
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
            logger.LogWarning(exception, "Failed to mark company user sync as failed.");
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
            $"Company user sync source object {sourceObjectName} was not found in source database '{sourceConnection.Database}'.");
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
        command.Parameters.AddWithValue("@Resource", $"CompanyUserSync:{SyncStateSourceSystem}:{SyncKey}");

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
        {
            throw new InvalidOperationException("Unable to acquire company user sync lock.");
        }
    }

    private async Task ReleaseAppLockAsync(SqlConnection localConnection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = localConnection.CreateCommand();
            command.CommandText = "EXEC sp_releaseapplock @Resource = @Resource, @LockOwner = 'Session';";
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.AddWithValue("@Resource", $"CompanyUserSync:{SyncStateSourceSystem}:{SyncKey}");
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to release company user sync lock.");
        }
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
        yield return ResolveSourceObjectName(_options.IndividualSourceObjectName);
    }

    private string ResolveSourceObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new InvalidOperationException("Company user sync source object name is not configured.");
        }

        if (!ObjectNamePattern.IsMatch(objectName))
        {
            throw new InvalidOperationException("Company user sync source object name contains unsupported characters.");
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

        throw new InvalidOperationException("Company user sync source connection string is not configured.");
    }

    private bool HasConfiguredSourceConnection()
    {
        var namedConnectionString = string.IsNullOrWhiteSpace(_options.SourceConnectionStringName)
            ? null
            : configuration.GetConnectionString(_options.SourceConnectionStringName);

        return !string.IsNullOrWhiteSpace(namedConnectionString) ||
               !string.IsNullOrWhiteSpace(_options.SourceConnectionString);
    }

    private static SyncJobSummaryResponse ToSummary(CompanyUserSyncStatusResponse status)
    {
        return new SyncJobSummaryResponse(
            SyncKey,
            SyncName,
            SyncDescription,
            string.Join(" + ", status.ContactPersonSourceObjectName, status.UserSourceObjectName, status.IndividualSourceObjectName, status.GroupSourceObjectName, status.RoleSourceObjectName),
            "[dbo].[AspNetUsers] + [dbo].[UserProfiles] + [dbo].[PlatformAccessGroups] + [dbo].[PlatformAccessRoles] + [dbo].[CompanyProfileUserAssignments]",
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
            "/configuration/company-users");
    }

    private static string BuildCompletedMessage(int userCount, int groupCount, int roleCount, int userGroupCount, int groupRoleCount, int directRoleCount, int assignmentCount, long? sourceCompanyId)
    {
        var scope = sourceCompanyId.HasValue ? $"source company {sourceCompanyId.Value}" : "all companies";
        return $"Synced {userCount} users, {groupCount} source groups, {roleCount} roles, {userGroupCount} user-group links, {groupRoleCount} group-role links, {directRoleCount} direct user-role links, and {assignmentCount} company assignments for {scope}.";
    }

    private static string BuildRoleCode(int legacyRoleId) => $"OS-ROLE-{legacyRoleId}";

    private static string BuildSourceGroupCode(int legacyGroupId) => $"OS-GROUP-{legacyGroupId}";

    private static string BuildDirectRoleGroupCode(int legacyRoleId) => $"OS-DIRECT-ROLE-{legacyRoleId}";

    private static string? NormalizeEmail(string? value)
    {
        var normalized = NormalizeNullable(value);
        return normalized?.ToLowerInvariant();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ResolveUserEmail(int legacyUserId, string? preferredEmail, ApplicationUser? existingUser, IReadOnlyDictionary<string, Guid> existingByEmail)
    {
        if (IsValidEmail(preferredEmail))
        {
            if (!existingByEmail.TryGetValue(preferredEmail, out var ownerId) || ownerId == existingUser?.Id)
            {
                return preferredEmail;
            }
        }

        return $"legacy-user-{legacyUserId}@migration.local";
    }

    private static string ResolveUserName(int legacyUserId, string? sourceUserName)
    {
        var normalizedUserName = NormalizeNullable(sourceUserName);
        if (!string.IsNullOrWhiteSpace(normalizedUserName) && AllowedUserNamePattern.IsMatch(normalizedUserName))
        {
            return normalizedUserName;
        }

        return $"legacy-user-{legacyUserId}";
    }

    private static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(value);
            return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static Guid? TryGetCompanyId(IReadOnlyDictionary<long, Guid> companies, long legacyCompanyId)
    {
        return companies.TryGetValue(legacyCompanyId, out var companyId) ? companyId : null;
    }

    private static bool IsManagedGroupCode(string? value)
    {
        return value?.StartsWith("OS-GROUP-", StringComparison.OrdinalIgnoreCase) == true ||
               value?.StartsWith("OS-DIRECT-ROLE-", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? GetNullableString(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : NormalizeNullable(reader.GetString(ordinal));
    }

    private static int? GetNullableInt32(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static long? GetNullableInt64(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
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

    private static void RestoreEntity(BaseEntity entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
    }

    private sealed record SourceUserRow(
        int LegacyUserId,
        string FullName,
        string? Email,
        string? SourceUserName,
        string? PhoneNumber,
        bool UserIsActive,
        DateTime? LastLoginAt,
        string? Nric,
        string? PassportNumber,
        int? LegacyIdentificationTypeId,
        long? LegacyAddressId,
        IReadOnlyCollection<long> CompanyIds,
        IReadOnlyCollection<SourceCompanyAssignmentRow> CompanyAssignments,
        int? LegacyTitleId,
        string? TitleDisplayName,
        int? LegacyDesignationId,
        string? CustomDesignationName,
        string? FaxNumber,
        int? SourceCreatedByLegacyUserId,
        DateTime? SourceCreatedAt,
        int? SourceUpdatedByLegacyUserId,
        DateTime? SourceUpdatedAt);

    private sealed record SourceCompanyAssignmentRow(long LegacyCompanyId, long LegacyContactPersonId, bool IsActive);
    private sealed record SourceGroupRow(int LegacyGroupId, string Name);
    private sealed record SourceRoleRow(int LegacyRoleId, string Name);
    private sealed record SourceUserGroupRow(int LegacyUserId, int LegacyGroupId);
    private sealed record SourceGroupRoleRow(int LegacyGroupId, int LegacyRoleId);
    private sealed record SourceDirectUserRoleRow(int LegacyUserId, int LegacyRoleId);
    private sealed record DesiredGroup(string Code, string Name, string Description);
    private sealed record DesiredGroupRoleAssignment(string GroupCode, int LegacyRoleId);
    private sealed record DesiredUserGroupAssignment(int LegacyUserId, string GroupCode);
    private sealed record DesiredCompanyAssignment(int LegacyUserId, Guid CompanyProfileId, long LegacyContactPersonId);
    private sealed record CompanyUserSyncStateRow(long? LastSourceCompanyId, DateTime? LastStartedAt, DateTime? LastCompletedAt, bool? LastRunSucceeded, int LastProcessedRows, string? LastRunMessage);
}
