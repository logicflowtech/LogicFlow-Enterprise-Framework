using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface IAccessManagementService
{
    Task<ServiceCenterAccessDetailResponse> CreateUserAsync(CreateServiceCenterUserRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCenterAccessUserSummaryResponse>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<ServiceCenterAccessDetailResponse?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceCenterAccessDetailResponse> UpsertUserAccessAsync(Guid userId, ServiceCenterAccessUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleOptionResponse>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCenterAccessAuditResponse>> GetAuditAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlatformFeatureCatalogResponse>> GetFeatureCatalogAsync(CancellationToken cancellationToken = default);
    Task<PlatformFeatureCatalogResponse> CreateFeatureAsync(CreatePlatformFeatureRequest request, CancellationToken cancellationToken = default);
    Task<PlatformFeatureCatalogResponse> UpdateFeatureAsync(Guid featureId, UpdatePlatformFeatureRequest request, CancellationToken cancellationToken = default);
    Task DeleteFeatureAsync(Guid featureId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessRoleCatalogResponse>> GetAccessRolesCatalogAsync(CancellationToken cancellationToken = default);
    Task<AccessRoleCatalogResponse> CreateAccessRoleAsync(CreateAccessRoleRequest request, CancellationToken cancellationToken = default);
    Task<AccessRoleCatalogResponse> UpdateAccessRoleAsync(Guid roleId, UpdateAccessRoleRequest request, CancellationToken cancellationToken = default);
    Task DeleteAccessRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessGroupCatalogResponse>> GetAccessGroupsCatalogAsync(CancellationToken cancellationToken = default);
    Task<AccessGroupCatalogResponse> CreateAccessGroupAsync(CreateAccessGroupRequest request, CancellationToken cancellationToken = default);
    Task<AccessGroupCatalogResponse> UpdateAccessGroupAsync(Guid groupId, UpdateAccessGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteAccessGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionCatalogResponse>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityRoleCatalogResponse>> GetSecurityRolesAsync(CancellationToken cancellationToken = default);
    Task<SecurityRoleCatalogResponse> CreateSecurityRoleAsync(CreateSecurityRoleRequest request, CancellationToken cancellationToken = default);
    Task DeleteSecurityRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
