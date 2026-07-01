using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace LogicFlowEnterpriseFramework.Shared.Features;

public sealed record PlatformFeatureDefinition(
    string FeatureCode,
    string Title,
    string Description,
    string Icon,
    string BasePath,
    bool IsEnabled,
    int SortOrder);

public sealed record PlatformMenuDefinition(
    string MenuCode,
    string FeatureCode,
    string Title,
    string Url,
    string Icon,
    int SortOrder,
    string? ParentCode = null,
    string? RequiredPermissionCode = null,
    bool IsVisible = true);

public sealed record PlatformPermissionDefinition(
    string FeatureCode,
    string PermissionCode,
    string Title,
    string Description,
    int SortOrder = 0);

public sealed record PlatformPageDefinition(
    string FeatureCode,
    Type PageType,
    string Route,
    string Title,
    string? MenuCode = null,
    string? RequiredPermissionCode = null,
    bool AllowAnonymous = false);

public interface IPlatformFeatureCatalog
{
    IReadOnlyCollection<PlatformFeatureDefinition> Features { get; }
    IReadOnlyCollection<PlatformMenuDefinition> Menus { get; }
    IReadOnlyCollection<PlatformPermissionDefinition> Permissions { get; }
    IReadOnlyCollection<PlatformPageDefinition> Pages { get; }
    IReadOnlyCollection<Assembly> FeatureAssemblies { get; }
    PlatformFeatureDefinition? FindFeature(string featureCode);
    PlatformPageDefinition? FindPage(Type pageType);
    PlatformPageDefinition? FindPageByRoute(string route);
}

public interface IPlatformFeature
{
    string FeatureCode { get; }
    string Title { get; }
    string Description { get; }
    string Icon { get; }
    string BasePath { get; }
    int SortOrder { get; }
    bool IsEnabled { get; }
    void RegisterServices(IServiceCollection services);
    void RegisterMenus(IMenuRegistry menuRegistry);
    void RegisterPermissions(IPermissionRegistry permissionRegistry);
    void RegisterPages(IPageRegistry pageRegistry);
}

public interface IPlatformFeatureCollectionBuilder
{
    IPlatformFeatureCollectionBuilder AddFeature<TFeature>() where TFeature : class, IPlatformFeature, new();
}

public interface IMenuRegistry
{
    void Register(PlatformMenuDefinition menu);
}

public interface IPermissionRegistry
{
    void Register(PlatformPermissionDefinition permission);
}

public interface IPageRegistry
{
    void Register(PlatformPageDefinition page);
}
