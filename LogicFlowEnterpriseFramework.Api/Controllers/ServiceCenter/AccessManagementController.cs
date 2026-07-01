using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LogicFlowEnterpriseFramework.Api.Controllers.ServiceCenter;

[ApiController]
[Route("api/service-center/access")]
public sealed class AccessManagementController(IAccessManagementService accessManagementService) : ControllerBase
{
    [HttpGet("roles")]
    [HasPermission(Permissions.ServiceCenterAccessRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleOptionResponse>>>> GetRoles(CancellationToken cancellationToken)
    {
        var result = await accessManagementService.GetRolesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoleOptionResponse>>.Success(result));
    }

    [HttpGet("catalog/features")]
    [HasPermission(Permissions.ServiceCenterAccessRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlatformFeatureCatalogResponse>>>> GetFeatureCatalog(CancellationToken cancellationToken)
    {
        var result = await accessManagementService.GetFeatureCatalogAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PlatformFeatureCatalogResponse>>.Success(result));
    }

    [HttpPost("catalog/features")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<PlatformFeatureCatalogResponse>>> CreateFeature(CreatePlatformFeatureRequest request, CancellationToken cancellationToken)
    {
        var result = await accessManagementService.CreateFeatureAsync(request, cancellationToken);
        return Ok(ApiResponse<PlatformFeatureCatalogResponse>.Success(result, "Platform feature created."));
    }

    [HttpPut("catalog/features/{featureId:guid}")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<PlatformFeatureCatalogResponse>>> UpdateFeature(Guid featureId, UpdatePlatformFeatureRequest request, CancellationToken cancellationToken)
    {
        var result = await accessManagementService.UpdateFeatureAsync(featureId, request, cancellationToken);
        return Ok(ApiResponse<PlatformFeatureCatalogResponse>.Success(result, "Platform feature updated."));
    }

    [HttpDelete("catalog/features/{featureId:guid}")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteFeature(Guid featureId, CancellationToken cancellationToken)
    {
        await accessManagementService.DeleteFeatureAsync(featureId, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Platform feature deleted."));
    }

    [HttpGet("catalog/access-roles")]
    [HasPermission(Permissions.ServiceCenterAccessRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccessRoleCatalogResponse>>>> GetAccessRolesCatalog(CancellationToken cancellationToken)
    {
        var result = await accessManagementService.GetAccessRolesCatalogAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccessRoleCatalogResponse>>.Success(result));
    }

    [HttpPost("catalog/access-roles")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<AccessRoleCatalogResponse>>> CreateAccessRole(CreateAccessRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await accessManagementService.CreateAccessRoleAsync(request, cancellationToken);
        return Ok(ApiResponse<AccessRoleCatalogResponse>.Success(result, "Access role created."));
    }

    [HttpPut("catalog/access-roles/{roleId:guid}")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<AccessRoleCatalogResponse>>> UpdateAccessRole(Guid roleId, UpdateAccessRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await accessManagementService.UpdateAccessRoleAsync(roleId, request, cancellationToken);
        return Ok(ApiResponse<AccessRoleCatalogResponse>.Success(result, "Access role updated."));
    }

    [HttpDelete("catalog/access-roles/{roleId:guid}")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteAccessRole(Guid roleId, CancellationToken cancellationToken)
    {
        await accessManagementService.DeleteAccessRoleAsync(roleId, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Access role deleted."));
    }

    [HttpGet("catalog/groups")]
    [HasPermission(Permissions.ServiceCenterAccessRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccessGroupCatalogResponse>>>> GetAccessGroupsCatalog(CancellationToken cancellationToken)
    {
        var result = await accessManagementService.GetAccessGroupsCatalogAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AccessGroupCatalogResponse>>.Success(result));
    }

    [HttpPost("catalog/groups")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<AccessGroupCatalogResponse>>> CreateAccessGroup(CreateAccessGroupRequest request, CancellationToken cancellationToken)
    {
        var result = await accessManagementService.CreateAccessGroupAsync(request, cancellationToken);
        return Ok(ApiResponse<AccessGroupCatalogResponse>.Success(result, "Access group created."));
    }

    [HttpPut("catalog/groups/{groupId:guid}")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<AccessGroupCatalogResponse>>> UpdateAccessGroup(Guid groupId, UpdateAccessGroupRequest request, CancellationToken cancellationToken)
    {
        var result = await accessManagementService.UpdateAccessGroupAsync(groupId, request, cancellationToken);
        return Ok(ApiResponse<AccessGroupCatalogResponse>.Success(result, "Access group updated."));
    }

    [HttpDelete("catalog/groups/{groupId:guid}")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteAccessGroup(Guid groupId, CancellationToken cancellationToken)
    {
        await accessManagementService.DeleteAccessGroupAsync(groupId, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Access group deleted."));
    }

    [HttpGet("catalog/permissions")]
    [HasPermission(Permissions.ServiceCenterAccessRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionCatalogResponse>>>> GetPermissionCatalog(CancellationToken cancellationToken)
    {
        var result = await accessManagementService.GetPermissionCatalogAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PermissionCatalogResponse>>.Success(result));
    }

    [HttpGet("catalog/security-roles")]
    [HasPermission(Permissions.ServiceCenterAccessRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SecurityRoleCatalogResponse>>>> GetSecurityRoles(CancellationToken cancellationToken)
    {
        var result = await accessManagementService.GetSecurityRolesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SecurityRoleCatalogResponse>>.Success(result));
    }

    [HttpPost("catalog/security-roles")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<SecurityRoleCatalogResponse>>> CreateSecurityRole(CreateSecurityRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await accessManagementService.CreateSecurityRoleAsync(request, cancellationToken);
        return Ok(ApiResponse<SecurityRoleCatalogResponse>.Success(result, "Security role created."));
    }

    [HttpDelete("catalog/security-roles/{roleId:guid}")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteSecurityRole(Guid roleId, CancellationToken cancellationToken)
    {
        await accessManagementService.DeleteSecurityRoleAsync(roleId, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Security role deleted."));
    }

    [HttpPost("users")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<ServiceCenterAccessDetailResponse>>> CreateUser(CreateServiceCenterUserRequest request, CancellationToken cancellationToken)
    {
        var result = await accessManagementService.CreateUserAsync(request, cancellationToken);
        return Ok(ApiResponse<ServiceCenterAccessDetailResponse>.Success(result, "Platform user created and granted Service Center access."));
    }

    [HttpDelete("users/{userId:guid}")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        await accessManagementService.DeleteUserAsync(userId, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "Platform user deleted."));
    }

    [HttpGet("users")]
    [HasPermission(Permissions.ServiceCenterAccessRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceCenterAccessUserSummaryResponse>>>> GetUsers(CancellationToken cancellationToken)
    {
        var result = await accessManagementService.GetUsersAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServiceCenterAccessUserSummaryResponse>>.Success(result));
    }

    [HttpGet("users/{userId:guid}")]
    [HasPermission(Permissions.ServiceCenterAccessRead)]
    public async Task<ActionResult<ApiResponse<ServiceCenterAccessDetailResponse>>> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await accessManagementService.GetUserAsync(userId, cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<object>.Failure("Service Center user was not found."));
        }

        return Ok(ApiResponse<ServiceCenterAccessDetailResponse>.Success(result));
    }

    [HttpPut("users/{userId:guid}")]
    [HasPermission(Permissions.ServiceCenterAccessManage)]
    public async Task<ActionResult<ApiResponse<ServiceCenterAccessDetailResponse>>> UpsertUser(Guid userId, ServiceCenterAccessUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await accessManagementService.UpsertUserAccessAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<ServiceCenterAccessDetailResponse>.Success(result, "Service Center access updated."));
    }

    [HttpGet("audit")]
    [HasPermission(Permissions.ServiceCenterAccessRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceCenterAccessAuditResponse>>>> GetAudit(CancellationToken cancellationToken)
    {
        var result = await accessManagementService.GetAuditAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServiceCenterAccessAuditResponse>>.Success(result));
    }
}
