using LogicFlowEnterpriseFramework.Shared.Features;

namespace LogicFlowEnterpriseFramework.Blazor.Services;

public interface IPlatformNavigationAccessService
{
    bool HasPermission(string? permissionCode);
    bool CanAccess(PlatformMenuDefinition menu);
    bool CanAccess(PlatformPageDefinition page);
}
