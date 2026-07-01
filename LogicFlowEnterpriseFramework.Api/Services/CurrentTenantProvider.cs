using LogicFlowEnterpriseFramework.Application.Interfaces;

namespace LogicFlowEnterpriseFramework.Api.Services;

public sealed class CurrentTenantProvider(ICurrentUserService currentUserService) : ITenantProvider
{
    public Guid? TenantId => currentUserService.TenantId;
}
