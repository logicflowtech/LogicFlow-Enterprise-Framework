using LogicFlowEnterpriseFramework.Blazor.Components.Pages;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Features;
using Microsoft.Extensions.DependencyInjection;

namespace LogicFlowEnterpriseFramework.Blazor.Features.Workspace;

public sealed class WorkspaceFeature : IPlatformFeature
{
    public string FeatureCode => "WORKSPACE";
    public string Title => "Workspace";
    public string Description => "Core enterprise workspace pages.";
    public string Icon => "dashboard";
    public string BasePath => "/dashboard";
    public int SortOrder => 10;
    public bool IsEnabled => true;

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void RegisterMenus(IMenuRegistry menuRegistry)
    {
        menuRegistry.Register(new PlatformMenuDefinition("workspace.dashboard", FeatureCode, "Dashboard", "/dashboard", "dashboard", 0));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.tasks", FeatureCode, "Tasks", "/tasks", "tasks", 10));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.application", FeatureCode, "Application", string.Empty, "folder", 20));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.company-profiles", FeatureCode, "Company Profiles", "/company-profiles", "folder", 20, "workspace.application", Permissions.CompanyProfilesRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.irpm-company-profile", FeatureCode, "IRPM Company Profile", "/irpm/company-profile", "folder", 23, "workspace.application", Permissions.CompanyProfilesRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.application-skeleton", FeatureCode, "Application Skeleton", "/application-skeleton", "reference", 22, "workspace.application"));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.style-guide", FeatureCode, "Style Guide", "/style-guide", "reference", 21, "workspace.application"));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.settings", FeatureCode, "Settings", string.Empty, "settings", 30));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.profile", FeatureCode, "Profile", "/profile", "settings", 31, "workspace.settings"));
    }

    public void RegisterPermissions(IPermissionRegistry permissionRegistry)
    {
        permissionRegistry.Register(new PlatformPermissionDefinition(
            FeatureCode,
            Permissions.CompanyProfilesRead,
            "Company Profiles Read",
            "View the synchronized company profile directory."));
    }

    public void RegisterPages(IPageRegistry pageRegistry)
    {
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(Dashboard), "/dashboard", "Dashboard", "workspace.dashboard"));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(Tasks), "/tasks", "Tasks", "workspace.tasks"));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(CompanyProfiles), "/company-profiles", "Company Profiles", "workspace.company-profiles", Permissions.CompanyProfilesRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(CompanyProfileDetail), "/company-profiles/{id:guid}", "Company Profile Details", null, Permissions.CompanyProfilesRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(IrpmCompanyProfile), "/irpm/company-profile", "IRPM Company Profile", "workspace.irpm-company-profile", Permissions.CompanyProfilesRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(IrpmFinancialDetails), "/irpm/financial-details", "IRPM Financial Details", null, Permissions.CompanyProfilesRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(IrpmModulePlaceholder), "/irpm/{moduleSlug}", "IRPM Module Placeholder", null, Permissions.CompanyProfilesRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(ApplicationFormSkeletonPreview), "/application-skeleton", "Application Form Skeleton", "workspace.application-skeleton"));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(Profile), "/profile", "Profile", "workspace.profile"));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(StyleGuide), "/style-guide", "Style Guide", "workspace.style-guide"));
    }
}
