using System.Reflection;
using LogicFlowEnterpriseFramework.Shared.Features;
using Microsoft.AspNetCore.Components;

namespace LogicFlowEnterpriseFramework.Blazor.Services;

public sealed class InMemoryPlatformFeatureCatalog : IPlatformFeatureCatalog
{
    private readonly Dictionary<string, PlatformFeatureDefinition> _featuresByCode;
    private readonly Dictionary<string, PlatformPageDefinition> _pagesByRoute;
    private readonly Dictionary<Type, PlatformPageDefinition> _pagesByType;

    public InMemoryPlatformFeatureCatalog(
        IEnumerable<PlatformFeatureDefinition> features,
        IEnumerable<PlatformMenuDefinition> menus,
        IEnumerable<PlatformPermissionDefinition> permissions,
        IEnumerable<PlatformPageDefinition> pages,
        IEnumerable<Assembly> featureAssemblies)
    {
        Features = features
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Menus = menus
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Permissions = permissions
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Pages = pages
            .OrderBy(x => x.Route, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        FeatureAssemblies = featureAssemblies
            .Distinct()
            .ToArray();

        _featuresByCode = Features.ToDictionary(x => x.FeatureCode, StringComparer.OrdinalIgnoreCase);
        _pagesByRoute = Pages.ToDictionary(x => NormalizeRoute(x.Route), StringComparer.OrdinalIgnoreCase);
        _pagesByType = Pages.ToDictionary(x => x.PageType);

        Validate();
    }

    public IReadOnlyCollection<PlatformFeatureDefinition> Features { get; }
    public IReadOnlyCollection<PlatformMenuDefinition> Menus { get; }
    public IReadOnlyCollection<PlatformPermissionDefinition> Permissions { get; }
    public IReadOnlyCollection<PlatformPageDefinition> Pages { get; }
    public IReadOnlyCollection<Assembly> FeatureAssemblies { get; }

    public PlatformFeatureDefinition? FindFeature(string featureCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureCode);
        return _featuresByCode.GetValueOrDefault(featureCode);
    }

    public PlatformPageDefinition? FindPage(Type pageType)
    {
        ArgumentNullException.ThrowIfNull(pageType);
        return _pagesByType.GetValueOrDefault(pageType);
    }

    public PlatformPageDefinition? FindPageByRoute(string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);
        return _pagesByRoute.GetValueOrDefault(NormalizeRoute(route));
    }

    private void Validate()
    {
        EnsureUnique("FeatureCode", Features.Select(x => x.FeatureCode));
        EnsureUnique("PermissionCode", Permissions.Select(x => x.PermissionCode));
        EnsureUnique("MenuCode", Menus.Select(x => x.MenuCode));
        EnsureUnique("Page route", Pages.Select(x => NormalizeRoute(x.Route)));
        EnsureUnique("Page type", Pages.Select(x => x.PageType.FullName ?? x.PageType.Name));

        var permissionCodes = Permissions
            .Select(x => x.PermissionCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var menu in Menus)
        {
            if (!_featuresByCode.ContainsKey(menu.FeatureCode))
            {
                throw new InvalidOperationException($"Menu '{menu.MenuCode}' references unknown feature '{menu.FeatureCode}'.");
            }

            if (!string.IsNullOrWhiteSpace(menu.RequiredPermissionCode) &&
                !permissionCodes.Contains(menu.RequiredPermissionCode))
            {
                throw new InvalidOperationException(
                    $"Menu '{menu.MenuCode}' requires permission '{menu.RequiredPermissionCode}', but that permission is not registered.");
            }

            if (!string.IsNullOrWhiteSpace(menu.ParentCode) &&
                !Menus.Any(x => string.Equals(x.MenuCode, menu.ParentCode, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Menu '{menu.MenuCode}' references missing parent menu '{menu.ParentCode}'.");
            }
        }

        foreach (var page in Pages)
        {
            if (!_featuresByCode.ContainsKey(page.FeatureCode))
            {
                throw new InvalidOperationException($"Page '{page.PageType.Name}' references unknown feature '{page.FeatureCode}'.");
            }

            if (!string.IsNullOrWhiteSpace(page.RequiredPermissionCode) &&
                !permissionCodes.Contains(page.RequiredPermissionCode))
            {
                throw new InvalidOperationException(
                    $"Page '{page.PageType.Name}' requires permission '{page.RequiredPermissionCode}', but that permission is not registered.");
            }

            var routes = page.PageType
                .GetCustomAttributes<RouteAttribute>(inherit: true)
                .Select(x => NormalizeRoute(x.Template))
                .ToArray();

            if (routes.Length == 0)
            {
                throw new InvalidOperationException($"Page '{page.PageType.FullName}' must declare an @page route.");
            }

            if (!routes.Contains(NormalizeRoute(page.Route), StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Page '{page.PageType.FullName}' route '{page.Route}' does not match the component route declaration.");
            }
        }

        var registeredPageTypes = Pages
            .Select(x => x.PageType)
            .ToHashSet();

        var discoveredPageTypes = FeatureAssemblies
            .SelectMany(x => x.ExportedTypes)
            .Where(IsRoutableComponent)
            .ToArray();

        var missingRegistrations = discoveredPageTypes
            .Where(x => !registeredPageTypes.Contains(x))
            .Select(x => x.FullName ?? x.Name)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missingRegistrations.Length > 0)
        {
            throw new InvalidOperationException(
                $"Every routable page must be registered with a feature. Missing registrations: {string.Join(", ", missingRegistrations)}");
        }

        foreach (var menu in Menus.Where(x => !string.IsNullOrWhiteSpace(x.Url)))
        {
            if (FindPageByRoute(menu.Url) is null)
            {
                throw new InvalidOperationException(
                    $"Menu '{menu.MenuCode}' points to '{menu.Url}', but no registered page uses that route.");
            }
        }
    }

    private static bool IsRoutableComponent(Type type)
    {
        return typeof(IComponent).IsAssignableFrom(type) &&
               !type.IsAbstract &&
               type.GetCustomAttributes<RouteAttribute>(inherit: true).Any();
    }

    private static void EnsureUnique(string label, IEnumerable<string> values)
    {
        var duplicates = values
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException($"{label} must be unique. Duplicates: {string.Join(", ", duplicates)}");
        }
    }

    private static string NormalizeRoute(string route)
    {
        var normalized = route.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return "/";
        }

        normalized = normalized.Trim('/');
        return normalized.Length == 0 ? "/" : $"/{normalized}";
    }
}
