using LogicFlowEnterpriseFramework.Blazor.Components.Pages;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Features;
using Microsoft.Extensions.DependencyInjection;

namespace LogicFlowEnterpriseFramework.Blazor.Features.Administration;

public sealed class AdministrationFeature : IPlatformFeature
{
    public string FeatureCode => "ADMINISTRATION";
    public string Title => "Administration";
    public string Description => "Access, configuration, and shared operational administration.";
    public string Icon => "settings";
    public string BasePath => "/access-management";
    public int SortOrder => 20;
    public bool IsEnabled => true;

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void RegisterMenus(IMenuRegistry menuRegistry)
    {
        menuRegistry.Register(new PlatformMenuDefinition(
            "administration.access-users",
            FeatureCode,
            "Users",
            "/access-management",
            "users",
            40,
            "workspace.settings",
            RequiredPermissionCode: Permissions.ServiceCenterAccessRead));

        menuRegistry.Register(new PlatformMenuDefinition(
            "administration.access-features",
            FeatureCode,
            "Features",
            "/access-management/features",
            "reference",
            41,
            "workspace.settings",
            RequiredPermissionCode: Permissions.ServiceCenterAccessRead));

        menuRegistry.Register(new PlatformMenuDefinition(
            "administration.access-groups",
            FeatureCode,
            "Groups",
            "/access-management/groups",
            "folder",
            42,
            "workspace.settings",
            RequiredPermissionCode: Permissions.ServiceCenterAccessRead));

        menuRegistry.Register(new PlatformMenuDefinition(
            "administration.access-roles",
            FeatureCode,
            "Roles",
            "/access-management/roles",
            "settings",
            43,
            "workspace.settings",
            RequiredPermissionCode: Permissions.ServiceCenterAccessRead));

    }

    public void RegisterPermissions(IPermissionRegistry permissionRegistry)
    {
        permissionRegistry.Register(new PlatformPermissionDefinition(
            FeatureCode,
            Permissions.ServiceCenterAccessRead,
            "Access Read",
            "View users, roles, teams, and queues."));
        permissionRegistry.Register(new PlatformPermissionDefinition(
            FeatureCode,
            Permissions.ServiceCenterAccessManage,
            "Access Manage",
            "Create users and update access assignments.",
            10));
    }

    public void RegisterPages(IPageRegistry pageRegistry)
    {
        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(AccessManagement),
            "/access-management",
            "Access Users",
            "administration.access-users",
            Permissions.ServiceCenterAccessRead));

        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(AccessManagementFeatures),
            "/access-management/features",
            "Access Features",
            "administration.access-features",
            Permissions.ServiceCenterAccessRead));

        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(AccessManagementGroups),
            "/access-management/groups",
            "Access Groups",
            "administration.access-groups",
            Permissions.ServiceCenterAccessRead));

        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(AccessManagementRoles),
            "/access-management/roles",
            "Access Roles",
            "administration.access-roles",
            Permissions.ServiceCenterAccessRead));

        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(Configuration),
            "/configuration",
            "Settings",
            "administration.configuration",
            Permissions.SystemAdminSettingsRead));

        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(EmailConfiguration),
            "/configuration/email",
            "Email Transport",
            "administration.configuration",
            Permissions.SystemAdminSettingsRead));

        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(ConfigurationSyncs),
            "/configuration/syncs",
            "Data Syncs",
            "administration.configuration",
            Permissions.SystemAdminSettingsRead));

        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(CompanyProfileSync),
            "/configuration/company-profiles",
            "Company Profile Sync",
            "administration.configuration",
            Permissions.SystemAdminSettingsRead));

        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(CompanyUserSync),
            "/configuration/company-users",
            "Company User Sync",
            "administration.configuration",
            Permissions.SystemAdminSettingsRead));

        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(CompanyRelatedDataSync),
            "/configuration/company-related-data",
            "Company Related Data Sync",
            "administration.configuration",
            Permissions.SystemAdminSettingsRead));

        pageRegistry.Register(new PlatformPageDefinition(
            FeatureCode,
            typeof(InvestMalaysiaGroupMappings),
            "/configuration/invest-malaysia-groups",
            "InvestMalaysia Group Mappings",
            "administration.configuration",
            Permissions.SystemAdminSettingsRead));
    }
}
