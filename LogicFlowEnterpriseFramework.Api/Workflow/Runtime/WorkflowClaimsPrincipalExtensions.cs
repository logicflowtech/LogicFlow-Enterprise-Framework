using System.Security.Claims;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;

namespace LogicFlowEnterpriseFramework.Api.Workflow.Runtime;

public static class WorkflowClaimsPrincipalExtensions
{
    public static WorkflowActor GetWorkflowActor(this ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : null;
        var userName = principal.Identity?.Name;

        return new WorkflowActor(
            userId,
            string.IsNullOrWhiteSpace(userName) ? "unknown" : userName,
            principal.FindFirstValue(ClaimTypes.Email) ?? userName);
    }

    public static bool IsWorkflowAdministrator(this ClaimsPrincipal principal)
    {
        return principal.HasClaim(AuthConstants.PermissionClaimType, Permissions.WorkflowAdmin);
    }
}

public sealed record WorkflowActor(Guid? UserId, string UserName, string? DisplayName);
