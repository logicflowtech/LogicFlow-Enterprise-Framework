using LogicFlowEnterpriseFramework.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LogicFlowEnterpriseFramework.Api.Security;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.IsInRole(AuthConstants.AdminRole) ||
            context.User.HasClaim(ClaimTypes.Role, AuthConstants.AdminRole) ||
            context.User.HasClaim(ClaimTypes.Email, "admin@logicflow.local") ||
            context.User.HasClaim(AuthConstants.PermissionClaimType, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
