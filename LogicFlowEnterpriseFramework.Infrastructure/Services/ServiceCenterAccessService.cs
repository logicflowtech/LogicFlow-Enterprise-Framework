using System.Security.Claims;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Domain.Entities;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using LogicFlowEnterpriseFramework.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class ServiceCenterAccessService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ITenantProvider tenantProvider) : IAccessManagementService
{
    private static readonly HashSet<string> SystemRoleNames = [AuthConstants.AdminRole, AuthConstants.UserRole];

    public async Task<ServiceCenterAccessDetailResponse> CreateUserAsync(CreateServiceCenterUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim();
        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            throw new InvalidOperationException("A user with the same email already exists.");
        }

        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        var user = new ApplicationUser
        {
            Email = normalizedEmail,
            UserName = normalizedEmail,
            FullName = request.FullName.Trim(),
            TenantId = tenantId,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "service-center-access"
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        return await UpsertUserAccessAsync(
            user.Id,
            new ServiceCenterAccessUpsertRequest(
                request.RoleIds,
                request.IsAccessEnabled,
                request.Escalations),
            cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCenterAccessUserSummaryResponse>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await GetScopedUsersQuery()
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.FullName
            })
            .ToListAsync(cancellationToken);

        var accessRecords = await dbContext.ServiceCenterUserAccesses
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ApplicationUserId, cancellationToken);

        var roleMap = await LoadUserRoleMapAsync(cancellationToken);

        var escalationMap = await dbContext.EscalationAssignments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .GroupBy(x => x.ApplicationUserId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

        return users
            .Select(user =>
            {
                accessRecords.TryGetValue(user.Id, out var access);
                roleMap.TryGetValue(user.Id, out var roles);
                escalationMap.TryGetValue(user.Id, out var escalationCount);

                return new ServiceCenterAccessUserSummaryResponse(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.FullName,
                    access?.IsAccessEnabled ?? false,
                    roles ?? Array.Empty<UserRoleAssignmentResponse>(),
                    escalationCount,
                    access?.UpdatedAt ?? access?.CreatedAt,
                    access?.UpdatedBy ?? access?.CreatedBy);
            })
            .OrderBy(x => x.FullName)
            .ToArray();
    }

    public async Task<ServiceCenterAccessDetailResponse?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetScopedUsersQuery()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null ? null : await BuildDetailAsync(user, cancellationToken);
    }

    public async Task<ServiceCenterAccessDetailResponse> UpsertUserAccessAsync(Guid userId, ServiceCenterAccessUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetScopedUsersQuery()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Service Center user was not found.");

        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        await SyncUserRolesAsync(user, request.RoleIds, tenantId, cancellationToken);

        var access = await dbContext.ServiceCenterUserAccesses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == userId, cancellationToken);

        var existingEscalations = await dbContext.EscalationAssignments
            .Where(x => x.ApplicationUserId == userId)
            .ToListAsync(cancellationToken);
        dbContext.EscalationAssignments.RemoveRange(existingEscalations);

        if (access is null)
        {
            access = new ServiceCenterUserAccess
            {
                ApplicationUserId = userId,
                TenantId = tenantId
            };
            await dbContext.ServiceCenterUserAccesses.AddAsync(access, cancellationToken);
        }
        else if (access.IsDeleted)
        {
            RestoreEntity(access);
        }

        access.IsAccessEnabled = request.IsAccessEnabled;

        foreach (var escalation in request.Escalations)
        {
            dbContext.EscalationAssignments.Add(new EscalationAssignment
            {
                ApplicationUserId = userId,
                Category = escalation.Category.Trim(),
                Priority = escalation.Priority.Trim(),
                Level = escalation.Level,
                IsActive = escalation.IsActive,
                TenantId = tenantId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildDetailAsync(user, cancellationToken);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .FirstOrDefaultAsync(x => x.Id == userId && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId.Value), cancellationToken)
            ?? throw new InvalidOperationException("Service Center user was not found.");

        dbContext.RefreshTokens.RemoveRange(await dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken));

        dbContext.EscalationAssignments.RemoveRange(await dbContext.EscalationAssignments
            .IgnoreQueryFilters()
            .Where(x => x.ApplicationUserId == userId)
            .ToListAsync(cancellationToken));

        dbContext.ServiceCenterUserAccesses.RemoveRange(await dbContext.ServiceCenterUserAccesses
            .IgnoreQueryFilters()
            .Where(x => x.ApplicationUserId == userId)
            .ToListAsync(cancellationToken));

        dbContext.UserAccessGroupAssignments.RemoveRange(await dbContext.UserAccessGroupAssignments
            .IgnoreQueryFilters()
            .Where(x => x.ApplicationUserId == userId)
            .ToListAsync(cancellationToken));

        dbContext.UserRoles.RemoveRange(await dbContext.UserRoles
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken));

        dbContext.UserClaims.RemoveRange(await dbContext.UserClaims
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken));

        dbContext.UserLogins.RemoveRange(await dbContext.UserLogins
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken));

        dbContext.UserTokens.RemoveRange(await dbContext.UserTokens
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken));

        await dbContext.SaveChangesAsync(cancellationToken);

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
    }

    public async Task<IReadOnlyList<RoleOptionResponse>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PlatformAccessGroups
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new RoleOptionResponse(
                x.Id,
                x.Code,
                x.Name,
                x.IsSystem,
                x.IsActive))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCenterAccessAuditResponse>> GetAuditAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ServiceCenterUserAccesses
            .AsNoTracking()
            .Join(
                GetScopedUsersQuery(),
                access => access.ApplicationUserId,
                user => user.Id,
                (access, user) => new ServiceCenterAccessAuditResponse(
                    user.Id,
                    user.FullName,
                    user.Email ?? string.Empty,
                    access.IsAccessEnabled,
                    access.UpdatedBy ?? access.CreatedBy,
                    access.UpdatedAt ?? access.CreatedAt))
            .OrderByDescending(x => x.UpdatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformFeatureCatalogResponse>> GetFeatureCatalogAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PlatformFeatures
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PlatformFeatureCatalogResponse(
                x.Id,
                x.Code,
                x.Name,
                x.Description,
                x.Category,
                x.DisplayOrder,
                x.IsActive,
                x.IsDeprecated,
                x.RoleFeatures.Count(link => link.IsEnabled)))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PlatformFeatureCatalogResponse> CreateFeatureAsync(CreatePlatformFeatureRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        var normalizedCode = request.FeatureCode.Trim().ToUpperInvariant();
        var normalizedName = request.FeatureName.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new InvalidOperationException("Feature code is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Feature name is required.");
        }

        var existing = await dbContext.PlatformFeatures
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (existing is not null && !existing.IsDeleted)
        {
            throw new InvalidOperationException("A feature with the same code already exists.");
        }

        var feature = existing ?? new PlatformFeature
        {
            TenantId = tenantId,
            Code = normalizedCode
        };

        if (existing is null)
        {
            await dbContext.PlatformFeatures.AddAsync(feature, cancellationToken);
        }
        else
        {
            RestoreEntity(feature);
        }

        feature.Name = normalizedName;
        feature.Description = request.Description.Trim();
        feature.Category = request.Category.Trim();
        feature.DisplayOrder = request.DisplayOrder;
        feature.IsActive = request.IsActive;
        feature.IsDeprecated = false;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildFeatureCatalogResponseAsync(feature.Id, cancellationToken);
    }

    public async Task<PlatformFeatureCatalogResponse> UpdateFeatureAsync(Guid featureId, UpdatePlatformFeatureRequest request, CancellationToken cancellationToken = default)
    {
        var feature = await dbContext.PlatformFeatures
            .FirstOrDefaultAsync(x => x.Id == featureId, cancellationToken)
            ?? throw new InvalidOperationException("Platform feature was not found.");

        var normalizedName = request.FeatureName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Feature name is required.");
        }

        feature.Name = normalizedName;
        feature.Description = request.Description.Trim();
        feature.Category = request.Category.Trim();
        feature.DisplayOrder = request.DisplayOrder;
        feature.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildFeatureCatalogResponseAsync(feature.Id, cancellationToken);
    }

    public async Task DeleteFeatureAsync(Guid featureId, CancellationToken cancellationToken = default)
    {
        var feature = await dbContext.PlatformFeatures
            .FirstOrDefaultAsync(x => x.Id == featureId, cancellationToken)
            ?? throw new InvalidOperationException("Platform feature was not found.");

        var groupLinks = await dbContext.PlatformGroupFeatures
            .IgnoreQueryFilters()
            .Where(x => x.PlatformFeatureId == featureId)
            .ToListAsync(cancellationToken);

        var roleLinks = await dbContext.PlatformRoleFeatures
            .IgnoreQueryFilters()
            .Where(x => x.PlatformFeatureId == featureId)
            .ToListAsync(cancellationToken);

        dbContext.PlatformGroupFeatures.RemoveRange(groupLinks);
        dbContext.PlatformRoleFeatures.RemoveRange(roleLinks);
        dbContext.PlatformFeatures.Remove(feature);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccessRoleCatalogResponse>> GetAccessRolesCatalogAsync(CancellationToken cancellationToken = default)
    {
        var roles = await dbContext.PlatformAccessRoles
            .AsNoTracking()
            .Include(x => x.RoleFeatures.Where(link => link.IsEnabled))
                .ThenInclude(x => x.PlatformFeature)
            .Include(x => x.GroupAssignments.Where(assignment => assignment.IsEnabled))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return roles
            .Select(role => new AccessRoleCatalogResponse(
                role.Id,
                role.Code,
                role.Name,
                role.Description,
                role.IsSystem,
                role.IsActive,
                role.RoleFeatures.Count(link => link.IsEnabled),
                role.GroupAssignments.Count(assignment => assignment.IsEnabled),
                role.RoleFeatures
                    .Where(link => link.IsEnabled)
                    .OrderBy(link => link.PlatformFeature.Name)
                    .Select(link => link.PlatformFeature.Code)
                    .ToArray(),
                role.RoleFeatures
                    .Where(link => link.IsEnabled)
                    .OrderBy(link => link.PlatformFeature.Name)
                    .Select(link => link.PlatformFeature.Name)
                    .ToArray()))
            .ToArray();
    }

    public async Task<AccessRoleCatalogResponse> CreateAccessRoleAsync(CreateAccessRoleRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        var normalizedCode = request.RoleCode.Trim().ToUpperInvariant();
        var normalizedName = request.RoleName.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new InvalidOperationException("Role code is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Role name is required.");
        }

        var existing = await dbContext.PlatformAccessRoles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (existing is not null && !existing.IsDeleted)
        {
            throw new InvalidOperationException("A role with the same code already exists.");
        }

        var normalizedFeatureIds = request.FeatureIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        var features = await dbContext.PlatformFeatures
            .Where(x => normalizedFeatureIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (normalizedFeatureIds.Except(features.Select(x => x.Id)).Any())
        {
            throw new InvalidOperationException("One or more selected features were not found.");
        }

        var role = existing ?? new PlatformAccessRole
        {
            TenantId = tenantId,
            Code = normalizedCode
        };

        if (existing is null)
        {
            await dbContext.PlatformAccessRoles.AddAsync(role, cancellationToken);
        }
        else
        {
            RestoreEntity(role);
        }

        role.Name = normalizedName;
        role.Description = request.Description.Trim();
        role.IsSystem = false;
        role.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncRoleFeaturesAsync(role.Id, normalizedFeatureIds, features, tenantId, cancellationToken);
        return await BuildAccessRoleCatalogResponseAsync(role.Id, cancellationToken);
    }

    public async Task<AccessRoleCatalogResponse> UpdateAccessRoleAsync(Guid roleId, UpdateAccessRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.PlatformAccessRoles
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken)
            ?? throw new InvalidOperationException("Access role was not found.");

        var normalizedName = request.RoleName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Role name is required.");
        }

        var normalizedFeatureIds = request.FeatureIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        var features = await dbContext.PlatformFeatures
            .Where(x => normalizedFeatureIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (normalizedFeatureIds.Except(features.Select(x => x.Id)).Any())
        {
            throw new InvalidOperationException("One or more selected features were not found.");
        }

        var tenantId = role.TenantId ?? await ResolveTenantIdAsync(cancellationToken);
        role.Name = normalizedName;
        role.Description = request.Description.Trim();
        role.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncRoleFeaturesAsync(role.Id, normalizedFeatureIds, features, tenantId, cancellationToken);
        return await BuildAccessRoleCatalogResponseAsync(role.Id, cancellationToken);
    }

    public async Task DeleteAccessRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.PlatformAccessRoles
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken)
            ?? throw new InvalidOperationException("Access role was not found.");

        dbContext.GroupAccessRoleAssignments.RemoveRange(await dbContext.GroupAccessRoleAssignments
            .IgnoreQueryFilters()
            .Where(x => x.PlatformAccessRoleId == roleId)
            .ToListAsync(cancellationToken));

        dbContext.PlatformRoleFeatures.RemoveRange(await dbContext.PlatformRoleFeatures
            .IgnoreQueryFilters()
            .Where(x => x.PlatformAccessRoleId == roleId)
            .ToListAsync(cancellationToken));

        dbContext.PlatformAccessRoles.Remove(role);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccessGroupCatalogResponse>> GetAccessGroupsCatalogAsync(CancellationToken cancellationToken = default)
    {
        var groups = await dbContext.PlatformAccessGroups
            .AsNoTracking()
            .Include(x => x.GroupRoles.Where(link => link.IsEnabled))
                .ThenInclude(x => x.PlatformAccessRole)
            .Include(x => x.UserAssignments.Where(assignment => assignment.IsEnabled))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return groups
            .Select(group => new AccessGroupCatalogResponse(
                group.Id,
                group.Code,
                group.Name,
                group.Description,
                group.IsSystem,
                group.IsActive,
                group.GroupRoles.Count(link => link.IsEnabled),
                group.UserAssignments.Count(assignment => assignment.IsEnabled),
                group.GroupRoles
                    .Where(link => link.IsEnabled)
                    .OrderBy(link => link.PlatformAccessRole.Name)
                    .Select(link => link.PlatformAccessRole.Code)
                    .ToArray(),
                group.GroupRoles
                    .Where(link => link.IsEnabled)
                    .OrderBy(link => link.PlatformAccessRole.Name)
                    .Select(link => link.PlatformAccessRole.Name)
                    .ToArray()))
            .ToArray();
    }

    public async Task<AccessGroupCatalogResponse> CreateAccessGroupAsync(CreateAccessGroupRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        var normalizedCode = request.GroupCode.Trim().ToUpperInvariant();
        var normalizedName = request.GroupName.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new InvalidOperationException("Group code is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Group name is required.");
        }

        var existing = await dbContext.PlatformAccessGroups
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (existing is not null && !existing.IsDeleted)
        {
            throw new InvalidOperationException("A group with the same code already exists.");
        }

        var normalizedRoleIds = request.RoleIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        var roles = await dbContext.PlatformAccessRoles
            .Where(x => normalizedRoleIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (normalizedRoleIds.Except(roles.Select(x => x.Id)).Any())
        {
            throw new InvalidOperationException("One or more selected roles were not found.");
        }

        var group = existing ?? new PlatformAccessGroup
        {
            TenantId = tenantId,
            Code = normalizedCode
        };

        if (existing is null)
        {
            await dbContext.PlatformAccessGroups.AddAsync(group, cancellationToken);
        }
        else
        {
            RestoreEntity(group);
        }

        group.Name = normalizedName;
        group.Description = request.Description.Trim();
        group.IsSystem = false;
        group.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncGroupRolesAsync(group.Id, normalizedRoleIds, roles, tenantId, cancellationToken);
        return await BuildAccessGroupCatalogResponseAsync(group.Id, cancellationToken);
    }

    public async Task<AccessGroupCatalogResponse> UpdateAccessGroupAsync(Guid groupId, UpdateAccessGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.PlatformAccessGroups
            .FirstOrDefaultAsync(x => x.Id == groupId, cancellationToken)
            ?? throw new InvalidOperationException("Access group was not found.");

        var normalizedName = request.GroupName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Group name is required.");
        }

        var normalizedRoleIds = request.RoleIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        var roles = await dbContext.PlatformAccessRoles
            .Where(x => normalizedRoleIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (normalizedRoleIds.Except(roles.Select(x => x.Id)).Any())
        {
            throw new InvalidOperationException("One or more selected roles were not found.");
        }

        var tenantId = group.TenantId ?? await ResolveTenantIdAsync(cancellationToken);
        group.Name = normalizedName;
        group.Description = request.Description.Trim();
        group.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncGroupRolesAsync(group.Id, normalizedRoleIds, roles, tenantId, cancellationToken);
        return await BuildAccessGroupCatalogResponseAsync(group.Id, cancellationToken);
    }

    public async Task DeleteAccessGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.PlatformAccessGroups
            .FirstOrDefaultAsync(x => x.Id == groupId, cancellationToken)
            ?? throw new InvalidOperationException("Access group was not found.");

        dbContext.UserAccessGroupAssignments.RemoveRange(await dbContext.UserAccessGroupAssignments
            .IgnoreQueryFilters()
            .Where(x => x.PlatformAccessGroupId == groupId)
            .ToListAsync(cancellationToken));

        dbContext.GroupAccessRoleAssignments.RemoveRange(await dbContext.GroupAccessRoleAssignments
            .IgnoreQueryFilters()
            .Where(x => x.PlatformAccessGroupId == groupId)
            .ToListAsync(cancellationToken));

        dbContext.PlatformGroupFeatures.RemoveRange(await dbContext.PlatformGroupFeatures
            .IgnoreQueryFilters()
            .Where(x => x.PlatformAccessGroupId == groupId)
            .ToListAsync(cancellationToken));

        dbContext.PlatformAccessGroups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionCatalogResponse>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PlatformFeatures
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Code)
            .Select(x => new PermissionCatalogResponse(
                x.Code,
                string.IsNullOrWhiteSpace(x.Name) ? x.Code : x.Name,
                string.IsNullOrWhiteSpace(x.Description) ? $"Feature code {x.Code}" : x.Description))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityRoleCatalogResponse>> GetSecurityRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var userAssignments = await dbContext.UserRoles
            .AsNoTracking()
            .GroupBy(x => x.RoleId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

        var roleSummaries = new List<SecurityRoleCatalogResponse>(roles.Count);
        foreach (var role in roles)
        {
            var claims = await roleManager.GetClaimsAsync(role);
            var permissionCodes = claims
                .Where(x => x.Type == AuthConstants.PermissionClaimType)
                .Select(x => x.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            roleSummaries.Add(new SecurityRoleCatalogResponse(
                role.Id,
                role.Name ?? string.Empty,
                !string.IsNullOrWhiteSpace(role.Name) && SystemRoleNames.Contains(role.Name),
                permissionCodes.Length,
                userAssignments.GetValueOrDefault(role.Id),
                permissionCodes));
        }

        return roleSummaries;
    }

    public async Task<SecurityRoleCatalogResponse> CreateSecurityRoleAsync(CreateSecurityRoleRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedName = request.RoleName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Role name is required.");
        }

        var validPermissionCodes = (await GetPermissionCatalogAsync(cancellationToken))
            .Select(x => x.PermissionCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selectedPermissions = request.PermissionCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var invalidPermissions = selectedPermissions
            .Where(permission => !validPermissionCodes.Contains(permission))
            .ToArray();

        if (invalidPermissions.Length > 0)
        {
            throw new InvalidOperationException($"Unknown permissions: {string.Join(", ", invalidPermissions)}");
        }

        if (await roleManager.FindByNameAsync(normalizedName) is not null)
        {
            throw new InvalidOperationException("A role with the same name already exists.");
        }

        var role = new ApplicationRole(normalizedName);
        var createResult = await roleManager.CreateAsync(role);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(x => x.Description)));
        }

        foreach (var permission in selectedPermissions)
        {
            await roleManager.AddClaimAsync(role, new Claim(AuthConstants.PermissionClaimType, permission));
        }

        return new SecurityRoleCatalogResponse(
            role.Id,
            role.Name ?? string.Empty,
            false,
            selectedPermissions.Length,
            0,
            selectedPermissions);
    }

    public async Task DeleteSecurityRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString())
            ?? throw new InvalidOperationException("Security role was not found.");

        var claims = await roleManager.GetClaimsAsync(role);
        foreach (var claim in claims)
        {
            var removeClaimResult = await roleManager.RemoveClaimAsync(role, claim);
            if (!removeClaimResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", removeClaimResult.Errors.Select(x => x.Description)));
            }
        }

        dbContext.UserRoles.RemoveRange(await dbContext.UserRoles
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken));

        await dbContext.SaveChangesAsync(cancellationToken);

        var deleteResult = await roleManager.DeleteAsync(role);
        if (!deleteResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", deleteResult.Errors.Select(x => x.Description)));
        }
    }

    private IQueryable<ApplicationUser> GetScopedUsersQuery()
    {
        var query = userManager.Users.AsNoTracking();
        if (tenantProvider.TenantId.HasValue)
        {
            query = query.Where(x => x.TenantId == tenantProvider.TenantId.Value);
        }

        return query;
    }

    private async Task<Guid> ResolveTenantIdAsync(CancellationToken cancellationToken)
    {
        if (tenantProvider.TenantId.HasValue)
        {
            return tenantProvider.TenantId.Value;
        }

        return await dbContext.Tenants
            .Where(x => x.Identifier == "default")
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
    }

    private async Task<ServiceCenterAccessDetailResponse> BuildDetailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var access = await dbContext.ServiceCenterUserAccesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id, cancellationToken);
        var roles = await LoadUserRolesAsync(user.Id, cancellationToken);

        var escalations = await dbContext.EscalationAssignments
            .AsNoTracking()
            .Where(x => x.ApplicationUserId == user.Id)
            .OrderBy(x => x.Level)
            .ThenBy(x => x.Category)
            .Select(x => new ServiceCenterEscalationAssignmentResponse(x.Id, x.Category, x.Priority, x.Level, x.IsActive))
            .ToArrayAsync(cancellationToken);

        return new ServiceCenterAccessDetailResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            access?.IsAccessEnabled ?? false,
            roles,
            escalations,
            access?.UpdatedAt ?? access?.CreatedAt,
            access?.UpdatedBy ?? access?.CreatedBy);
    }

    private async Task<IReadOnlyCollection<UserRoleAssignmentResponse>> LoadUserRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserAccessGroupAssignments
            .AsNoTracking()
            .Where(x => x.ApplicationUserId == userId && x.IsEnabled)
            .Include(x => x.PlatformAccessGroup)
            .OrderBy(x => x.PlatformAccessGroup.Name)
            .Select(x => new UserRoleAssignmentResponse(
                x.PlatformAccessGroupId,
                x.PlatformAccessGroup.Code,
                x.PlatformAccessGroup.Name))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, IReadOnlyCollection<UserRoleAssignmentResponse>>> LoadUserRoleMapAsync(CancellationToken cancellationToken)
    {
        return await dbContext.UserAccessGroupAssignments
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .Include(x => x.PlatformAccessGroup)
            .GroupBy(x => x.ApplicationUserId)
            .ToDictionaryAsync(
                group => group.Key,
                group => (IReadOnlyCollection<UserRoleAssignmentResponse>)group
                    .OrderBy(x => x.PlatformAccessGroup.Name)
                    .Select(x => new UserRoleAssignmentResponse(
                        x.PlatformAccessGroupId,
                        x.PlatformAccessGroup.Code,
                        x.PlatformAccessGroup.Name))
                    .ToArray(),
                cancellationToken);
    }

    private async Task SyncUserRolesAsync(ApplicationUser user, IReadOnlyCollection<Guid> requestedRoleIds, Guid tenantId, CancellationToken cancellationToken)
    {
        var normalizedRoleIds = requestedRoleIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        var availableRoles = await dbContext.PlatformAccessGroups
            .IgnoreQueryFilters()
            .Where(x => normalizedRoleIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var unknownRoleIds = normalizedRoleIds.Except(availableRoles.Select(x => x.Id)).ToArray();
        if (unknownRoleIds.Length > 0)
        {
            throw new InvalidOperationException("One or more selected roles were not found.");
        }

        var existingAssignments = await dbContext.UserAccessGroupAssignments
            .IgnoreQueryFilters()
            .Where(x => x.ApplicationUserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var assignment in existingAssignments.Where(x => !normalizedRoleIds.Contains(x.PlatformAccessGroupId)))
        {
            dbContext.UserAccessGroupAssignments.Remove(assignment);
        }

        foreach (var role in availableRoles)
        {
            var existing = existingAssignments.FirstOrDefault(x => x.PlatformAccessGroupId == role.Id);
            if (existing is null)
            {
                dbContext.UserAccessGroupAssignments.Add(new UserAccessGroupAssignment
                {
                    ApplicationUserId = user.Id,
                    PlatformAccessGroupId = role.Id,
                    TenantId = tenantId,
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
        await SyncAlignedIdentityRolesAsync(user, normalizedRoleIds);
    }

    private async Task SyncAlignedIdentityRolesAsync(ApplicationUser user, IReadOnlyCollection<Guid> accessGroupIds)
    {
        var desiredRoleNames = await dbContext.GroupAccessRoleAssignments
            .AsNoTracking()
            .Where(x => accessGroupIds.Contains(x.PlatformAccessGroupId) && x.IsEnabled && x.PlatformAccessRole.IsActive)
            .Select(x => x.PlatformAccessRole.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArrayAsync();

        var availableIdentityRoleNames = await roleManager.Roles
            .AsNoTracking()
            .Where(x => x.Name != null)
            .Select(x => x.Name!)
            .ToArrayAsync();

        var managedRoleNames = await dbContext.PlatformAccessRoles
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArrayAsync();

        var availableIdentityRoleSet = availableIdentityRoleNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var managedRoleSet = managedRoleNames
            .Where(x => !string.Equals(x, AuthConstants.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(x, AuthConstants.UserRole, StringComparison.OrdinalIgnoreCase) &&
                        availableIdentityRoleSet.Contains(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var desiredManagedRoles = desiredRoleNames
            .Where(managedRoleSet.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var currentRoles = await userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles
            .Where(managedRoleSet.Contains)
            .Where(roleName => !desiredManagedRoles.Contains(roleName))
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", removeResult.Errors.Select(x => x.Description)));
            }
        }

        var rolesToAdd = desiredManagedRoles
            .Where(roleName => !currentRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (rolesToAdd.Length > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", addResult.Errors.Select(x => x.Description)));
            }
        }
    }

    private async Task SyncRoleFeaturesAsync(Guid roleId, IReadOnlyCollection<Guid> normalizedFeatureIds, IReadOnlyCollection<PlatformFeature> features, Guid tenantId, CancellationToken cancellationToken)
    {
        var existingLinks = await dbContext.PlatformRoleFeatures
            .IgnoreQueryFilters()
            .Where(x => x.PlatformAccessRoleId == roleId)
            .ToListAsync(cancellationToken);

        foreach (var link in existingLinks.Where(x => !normalizedFeatureIds.Contains(x.PlatformFeatureId)))
        {
            dbContext.PlatformRoleFeatures.Remove(link);
        }

        foreach (var feature in features)
        {
            var existingLink = existingLinks.FirstOrDefault(x => x.PlatformFeatureId == feature.Id);
            if (existingLink is null)
            {
                dbContext.PlatformRoleFeatures.Add(new PlatformRoleFeature
                {
                    TenantId = tenantId,
                    PlatformAccessRoleId = roleId,
                    PlatformFeatureId = feature.Id,
                    IsEnabled = true
                });
                continue;
            }

            if (existingLink.IsDeleted)
            {
                RestoreEntity(existingLink);
            }

            existingLink.IsEnabled = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncGroupRolesAsync(Guid groupId, IReadOnlyCollection<Guid> normalizedRoleIds, IReadOnlyCollection<PlatformAccessRole> roles, Guid tenantId, CancellationToken cancellationToken)
    {
        var existingLinks = await dbContext.GroupAccessRoleAssignments
            .IgnoreQueryFilters()
            .Where(x => x.PlatformAccessGroupId == groupId)
            .ToListAsync(cancellationToken);

        foreach (var link in existingLinks.Where(x => !normalizedRoleIds.Contains(x.PlatformAccessRoleId)))
        {
            dbContext.GroupAccessRoleAssignments.Remove(link);
        }

        foreach (var role in roles)
        {
            var existingLink = existingLinks.FirstOrDefault(x => x.PlatformAccessRoleId == role.Id);
            if (existingLink is null)
            {
                dbContext.GroupAccessRoleAssignments.Add(new GroupAccessRoleAssignment
                {
                    TenantId = tenantId,
                    PlatformAccessGroupId = groupId,
                    PlatformAccessRoleId = role.Id,
                    IsEnabled = true
                });
                continue;
            }

            if (existingLink.IsDeleted)
            {
                RestoreEntity(existingLink);
            }

            existingLink.IsEnabled = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<PlatformFeatureCatalogResponse> BuildFeatureCatalogResponseAsync(Guid featureId, CancellationToken cancellationToken)
    {
        return dbContext.PlatformFeatures
            .AsNoTracking()
            .Where(x => x.Id == featureId)
            .Select(x => new PlatformFeatureCatalogResponse(
                x.Id,
                x.Code,
                x.Name,
                x.Description,
                x.Category,
                x.DisplayOrder,
                x.IsActive,
                x.IsDeprecated,
                x.RoleFeatures.Count(link => link.IsEnabled)))
            .FirstAsync(cancellationToken);
    }

    private async Task<AccessRoleCatalogResponse> BuildAccessRoleCatalogResponseAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await dbContext.PlatformAccessRoles
            .AsNoTracking()
            .Where(x => x.Id == roleId)
            .Include(x => x.RoleFeatures.Where(link => link.IsEnabled))
                .ThenInclude(x => x.PlatformFeature)
            .Include(x => x.GroupAssignments.Where(assignment => assignment.IsEnabled))
            .FirstAsync(cancellationToken);

        return new AccessRoleCatalogResponse(
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            role.IsSystem,
            role.IsActive,
            role.RoleFeatures.Count(link => link.IsEnabled),
            role.GroupAssignments.Count(assignment => assignment.IsEnabled),
            role.RoleFeatures
                .Where(link => link.IsEnabled)
                .OrderBy(link => link.PlatformFeature.Name)
                .Select(link => link.PlatformFeature.Code)
                .ToArray(),
            role.RoleFeatures
                .Where(link => link.IsEnabled)
                .OrderBy(link => link.PlatformFeature.Name)
                .Select(link => link.PlatformFeature.Name)
                .ToArray());
    }

    private async Task<AccessGroupCatalogResponse> BuildAccessGroupCatalogResponseAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await dbContext.PlatformAccessGroups
            .AsNoTracking()
            .Where(x => x.Id == groupId)
            .Include(x => x.GroupRoles.Where(link => link.IsEnabled))
                .ThenInclude(x => x.PlatformAccessRole)
            .Include(x => x.UserAssignments.Where(assignment => assignment.IsEnabled))
            .FirstAsync(cancellationToken);

        return new AccessGroupCatalogResponse(
            group.Id,
            group.Code,
            group.Name,
            group.Description,
            group.IsSystem,
            group.IsActive,
            group.GroupRoles.Count(link => link.IsEnabled),
            group.UserAssignments.Count(assignment => assignment.IsEnabled),
            group.GroupRoles
                .Where(link => link.IsEnabled)
                .OrderBy(link => link.PlatformAccessRole.Name)
                .Select(link => link.PlatformAccessRole.Code)
                .ToArray(),
            group.GroupRoles
                .Where(link => link.IsEnabled)
                .OrderBy(link => link.PlatformAccessRole.Name)
                .Select(link => link.PlatformAccessRole.Name)
                .ToArray());
    }

    private static void RestoreEntity(BaseEntity entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
    }
}
