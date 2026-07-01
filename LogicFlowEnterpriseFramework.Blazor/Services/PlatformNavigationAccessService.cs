using LogicFlowEnterpriseFramework.Shared.Features;

namespace LogicFlowEnterpriseFramework.Blazor.Services;

public sealed class PlatformNavigationAccessService(AuthSession session) : IPlatformNavigationAccessService
{
    public bool HasPermission(string? permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return true;
        }

        if (!session.IsAuthenticated)
        {
            return false;
        }

        return session.User?.Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase) == true;
    }

    public bool CanAccess(PlatformMenuDefinition menu)
    {
        return HasPermission(menu.RequiredPermissionCode);
    }

    public bool CanAccess(PlatformPageDefinition page)
    {
        if (page.AllowAnonymous)
        {
            return true;
        }

        return HasPermission(page.RequiredPermissionCode);
    }
}
