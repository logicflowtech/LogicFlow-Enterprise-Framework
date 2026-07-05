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
        menuRegistry.Register(new PlatformMenuDefinition("workspace.dashboard", FeatureCode, "Dashboard", "/dashboard", "dashboard", 0, RequiredPermissionCode: Permissions.ApplicantDashboardRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.tasks", FeatureCode, "My Tasks", "/tasks", "tasks", 10, RequiredPermissionCode: Permissions.ApplicantTasksRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications", FeatureCode, "Applications", "/applications", "folder", 20, RequiredPermissionCode: Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.spm", FeatureCode, "Confirmation Letter for Exemption (SPM)", "/applications/spm", "reference", 21, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.dte", FeatureCode, "Import Duty and/or Sales Tax Exemption", "/applications/dte", "reference", 22, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.ml", FeatureCode, "Manufacturing Licence (e-ML)", "/applications/ml", "reference", 23, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.incentive", FeatureCode, "Incentive (e-Incentive)", "/applications/incentive", "reference", 24, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.grant", FeatureCode, "Grant (e-Grant)", "/applications/grant", "reference", 25, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.ica10", FeatureCode, "Exempted From Manufacturing Licence (ICA10)", "/applications/ica10", "reference", 26, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.domestic-sales", FeatureCode, "Domestic Sales", "/applications/domestic-sales", "reference", 27, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.permit", FeatureCode, "Permit (PDA 2)", "/applications/permit", "reference", 28, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.rnd", FeatureCode, "R&D/ILS/DIILS Status", "/applications/rnd", "reference", 29, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.rero", FeatureCode, "RE/RO", "/applications/rero", "reference", 30, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.ep", FeatureCode, "Expatriate Post (EP)", "/applications/ep", "reference", 31, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.applications.post-approval-ml", FeatureCode, "Post Approval (ML)", "/applications/post-approval-ml", "reference", 32, "workspace.applications", Permissions.ApplicantApplicationsRead));
        menuRegistry.Register(new PlatformMenuDefinition("workspace.settings", FeatureCode, "Settings", "/configuration", "settings", 40, RequiredPermissionCode: Permissions.SystemAdminSettingsRead));
    }

    public void RegisterPermissions(IPermissionRegistry permissionRegistry)
    {
        permissionRegistry.Register(new PlatformPermissionDefinition(
            FeatureCode,
            Permissions.CompanyProfilesRead,
            "Company Profiles Read",
            "View the synchronized company profile directory."));
        permissionRegistry.Register(new PlatformPermissionDefinition(
            FeatureCode,
            Permissions.ApplicantDashboardRead,
            "Applicant Dashboard Read",
            "Access the applicant dashboard.",
            10));
        permissionRegistry.Register(new PlatformPermissionDefinition(
            FeatureCode,
            Permissions.ApplicantTasksRead,
            "Applicant Tasks Read",
            "Access applicant task features.",
            20));
        permissionRegistry.Register(new PlatformPermissionDefinition(
            FeatureCode,
            Permissions.ApplicantApplicationsRead,
            "Applicant Applications Read",
            "Access applicant applications.",
            30));
        permissionRegistry.Register(new PlatformPermissionDefinition(
            FeatureCode,
            Permissions.ApplicantCompanyProfileRead,
            "Applicant Company Profile Read",
            "Access applicant company profile pages.",
            40));
        permissionRegistry.Register(new PlatformPermissionDefinition(
            FeatureCode,
            Permissions.SystemAdminSettingsRead,
            "System Admin Settings Read",
            "Access settings pages.",
            50));
        permissionRegistry.Register(new PlatformPermissionDefinition(
            FeatureCode,
            Permissions.SystemAdminSettingsManage,
            "System Admin Settings Manage",
            "Manage settings pages.",
            60));
    }

    public void RegisterPages(IPageRegistry pageRegistry)
    {
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(Dashboard), "/dashboard", "Dashboard", "workspace.dashboard"));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(ApplicantDashboard), "/dashboard/applicant", "Applicant Dashboard", "workspace.dashboard", Permissions.ApplicantDashboardRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(ScreeningOfficerDashboard), "/dashboard/screening-officer", "Screening Officer Dashboard", null));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(Tasks), "/tasks", "My Tasks", "workspace.tasks", Permissions.ApplicantTasksRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(Applications), "/applications/{applicationCode}", "Applications", "workspace.applications", Permissions.ApplicantApplicationsRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(ApplicantApplicationSubmission), "/applications/new/{applicationCode}", "New Application Submission", null, Permissions.ApplicantApplicationsRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(CompanyProfiles), "/company-profiles", "Company Profiles", "workspace.company-profiles", Permissions.CompanyProfilesRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(CompanyProfileDetail), "/company-profiles/{id:guid}", "Company Profile Details", null, Permissions.CompanyProfilesRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(IrpmCompanyProfile), "/irpm/company-profile", "Company Profile", null, Permissions.ApplicantCompanyProfileRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(IrpmFinancialDetails), "/irpm/financial-details", "IRPM Financial Details", null, Permissions.CompanyProfilesRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(IrpmModulePlaceholder), "/irpm/{moduleSlug}", "IRPM Module Placeholder", null, Permissions.CompanyProfilesRead));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(ApplicationFormSkeletonPreview), "/application-skeleton", "Application Form Skeleton", null));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(Profile), "/profile", "Profile", null));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(StyleGuide), "/style-guide", "Style Guide", null));
    }
}
