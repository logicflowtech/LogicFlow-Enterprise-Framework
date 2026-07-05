using LogicFlowEnterpriseFramework.Shared.Constants;
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

        if (IsSuperAdmin())
        {
            return true;
        }

        if (session.User?.Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        var roles = session.User?.Roles ?? [];
        if (roles.Contains("Applicant", StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(permissionCode, Permissions.ApplicantDashboardRead, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(permissionCode, Permissions.ApplicantTasksRead, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(permissionCode, Permissions.ApplicantApplicationsRead, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(permissionCode, Permissions.ApplicantCompanyProfileRead, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (roles.Contains("System Administrator", StringComparer.OrdinalIgnoreCase) ||
            roles.Contains("System Admin", StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(permissionCode, Permissions.SystemAdminSettingsRead, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(permissionCode, Permissions.SystemAdminSettingsManage, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSuperAdmin()
    {
        return session.User?.Roles.Contains(AuthConstants.AdminRole, StringComparer.OrdinalIgnoreCase) == true ||
               string.Equals(session.User?.Email, "admin@logicflow.local", StringComparison.OrdinalIgnoreCase);
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
