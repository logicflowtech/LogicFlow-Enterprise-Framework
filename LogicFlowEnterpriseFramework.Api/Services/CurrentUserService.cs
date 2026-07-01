using System.Security.Claims;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;

namespace LogicFlowEnterpriseFramework.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public Guid? TenantId => Guid.TryParse(User?.FindFirstValue(AuthConstants.TenantClaimType), out var tenantId) ? tenantId : null;

    public string? UserName => User?.Identity?.Name;
}
