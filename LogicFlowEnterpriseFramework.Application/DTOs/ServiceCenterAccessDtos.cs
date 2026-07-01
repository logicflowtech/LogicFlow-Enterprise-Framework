namespace LogicFlowEnterpriseFramework.Application.DTOs;

public sealed record ServiceCenterEscalationAssignmentRequest(string Category, string Priority, int Level, bool IsActive);

public sealed record ServiceCenterAccessUpsertRequest(
    IReadOnlyCollection<Guid> RoleIds,
    bool IsAccessEnabled,
    IReadOnlyCollection<ServiceCenterEscalationAssignmentRequest> Escalations);

public sealed record UserRoleAssignmentResponse(
    Guid RoleId,
    string RoleCode,
    string RoleName);

public sealed record RoleOptionResponse(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    bool IsSystemManaged,
    bool IsActive);

public sealed record ServiceCenterAccessUserSummaryResponse(
    Guid UserId,
    string Email,
    string FullName,
    bool IsAccessEnabled,
    IReadOnlyCollection<UserRoleAssignmentResponse> Roles,
    int EscalationAssignments,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

public sealed record ServiceCenterEscalationAssignmentResponse(Guid Id, string Category, string Priority, int Level, bool IsActive);

public sealed record ServiceCenterAccessDetailResponse(
    Guid UserId,
    string Email,
    string FullName,
    bool IsAccessEnabled,
    IReadOnlyCollection<UserRoleAssignmentResponse> Roles,
    IReadOnlyCollection<ServiceCenterEscalationAssignmentResponse> Escalations,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

public sealed record ServiceCenterAccessAuditResponse(
    Guid UserId,
    string FullName,
    string Email,
    bool IsAccessEnabled,
    string? UpdatedBy,
    DateTimeOffset? UpdatedAt);

public sealed record PlatformFeatureCatalogResponse(
    Guid FeatureId,
    string FeatureCode,
    string FeatureName,
    string Description,
    string Category,
    int DisplayOrder,
    bool IsActive,
    bool IsDeprecated,
    int AccessRoleCount);

public sealed record CreatePlatformFeatureRequest(
    string FeatureCode,
    string FeatureName,
    string Description,
    string Category,
    int DisplayOrder,
    bool IsActive);

public sealed record UpdatePlatformFeatureRequest(
    string FeatureName,
    string Description,
    string Category,
    int DisplayOrder,
    bool IsActive);

public sealed record AccessRoleCatalogResponse(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    string Description,
    bool IsSystem,
    bool IsActive,
    int FeatureCount,
    int GroupCount,
    IReadOnlyCollection<string> FeatureCodes,
    IReadOnlyCollection<string> FeatureNames);

public sealed record CreateAccessRoleRequest(
    string RoleCode,
    string RoleName,
    string Description,
    bool IsActive,
    IReadOnlyCollection<Guid> FeatureIds);

public sealed record UpdateAccessRoleRequest(
    string RoleName,
    string Description,
    bool IsActive,
    IReadOnlyCollection<Guid> FeatureIds);

public sealed record AccessGroupCatalogResponse(
    Guid GroupId,
    string GroupCode,
    string GroupName,
    string Description,
    bool IsSystem,
    bool IsActive,
    int RoleCount,
    int MemberCount,
    IReadOnlyCollection<string> RoleCodes,
    IReadOnlyCollection<string> RoleNames);

public sealed record CreateAccessGroupRequest(
    string GroupCode,
    string GroupName,
    string Description,
    bool IsActive,
    IReadOnlyCollection<Guid> RoleIds);

public sealed record UpdateAccessGroupRequest(
    string GroupName,
    string Description,
    bool IsActive,
    IReadOnlyCollection<Guid> RoleIds);

public sealed record PermissionCatalogResponse(
    string PermissionCode,
    string Title,
    string Description);

public sealed record SecurityRoleCatalogResponse(
    Guid RoleId,
    string RoleName,
    bool IsSystem,
    int PermissionCount,
    int AssignedUserCount,
    IReadOnlyCollection<string> PermissionCodes);

public sealed record CreateSecurityRoleRequest(
    string RoleName,
    IReadOnlyCollection<string> PermissionCodes);
