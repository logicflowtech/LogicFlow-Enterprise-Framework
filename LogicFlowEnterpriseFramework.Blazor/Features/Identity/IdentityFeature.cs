using LogicFlowEnterpriseFramework.Blazor.Components.Pages;
using LogicFlowEnterpriseFramework.Shared.Features;
using Microsoft.Extensions.DependencyInjection;

namespace LogicFlowEnterpriseFramework.Blazor.Features.Identity;

public sealed class IdentityFeature : IPlatformFeature
{
    public string FeatureCode => "IDENTITY";
    public string Title => "Identity";
    public string Description => "Authentication and entry pages.";
    public string Icon => "reference";
    public string BasePath => "/login";
    public int SortOrder => 0;
    public bool IsEnabled => true;

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void RegisterMenus(IMenuRegistry menuRegistry)
    {
    }

    public void RegisterPermissions(IPermissionRegistry permissionRegistry)
    {
    }

    public void RegisterPages(IPageRegistry pageRegistry)
    {
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(Home), "/", "Root Redirect", AllowAnonymous: true));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(Login), "/login", "Login", AllowAnonymous: true));
        pageRegistry.Register(new PlatformPageDefinition(FeatureCode, typeof(Error), "/error", "Error", AllowAnonymous: true));
    }
}
