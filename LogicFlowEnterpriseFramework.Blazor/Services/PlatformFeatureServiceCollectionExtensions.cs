using LogicFlowEnterpriseFramework.Shared.Features;

namespace LogicFlowEnterpriseFramework.Blazor.Services;

public static class PlatformFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformFeatures(
        this IServiceCollection services,
        Action<IPlatformFeatureCollectionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new PlatformFeatureCollectionBuilder(services);
        configure(builder);

        var catalog = builder.Build();
        services.AddSingleton<IPlatformFeatureCatalog>(catalog);
        services.AddScoped<IPlatformNavigationAccessService, PlatformNavigationAccessService>();
        services.AddScoped<IPlatformFeatureAccessService, PlatformFeatureAccessService>();

        return services;
    }

    private sealed class PlatformFeatureCollectionBuilder(IServiceCollection services) : IPlatformFeatureCollectionBuilder
    {
        private readonly List<IPlatformFeature> _features = [];

        public IPlatformFeatureCollectionBuilder AddFeature<TFeature>() where TFeature : class, IPlatformFeature, new()
        {
            var feature = new TFeature();
            feature.RegisterServices(services);
            _features.Add(feature);
            return this;
        }

        public InMemoryPlatformFeatureCatalog Build()
        {
            var menuRegistry = new BufferingMenuRegistry();
            var permissionRegistry = new BufferingPermissionRegistry();
            var pageRegistry = new BufferingPageRegistry();
            var featureDefinitions = new List<PlatformFeatureDefinition>(_features.Count);

            foreach (var feature in _features)
            {
                featureDefinitions.Add(new PlatformFeatureDefinition(
                    feature.FeatureCode,
                    feature.Title,
                    feature.Description,
                    feature.Icon,
                    feature.BasePath,
                    feature.IsEnabled,
                    feature.SortOrder));

                feature.RegisterPermissions(permissionRegistry);
                feature.RegisterMenus(menuRegistry);
                feature.RegisterPages(pageRegistry);
            }

            return new InMemoryPlatformFeatureCatalog(
                featureDefinitions,
                menuRegistry.Items,
                permissionRegistry.Items,
                pageRegistry.Items,
                _features.Select(x => x.GetType().Assembly));
        }
    }

    private sealed class BufferingMenuRegistry : IMenuRegistry
    {
        public List<PlatformMenuDefinition> Items { get; } = [];

        public void Register(PlatformMenuDefinition menu)
        {
            ArgumentNullException.ThrowIfNull(menu);
            Items.Add(menu);
        }
    }

    private sealed class BufferingPermissionRegistry : IPermissionRegistry
    {
        public List<PlatformPermissionDefinition> Items { get; } = [];

        public void Register(PlatformPermissionDefinition permission)
        {
            ArgumentNullException.ThrowIfNull(permission);
            Items.Add(permission);
        }
    }

    private sealed class BufferingPageRegistry : IPageRegistry
    {
        public List<PlatformPageDefinition> Items { get; } = [];

        public void Register(PlatformPageDefinition page)
        {
            ArgumentNullException.ThrowIfNull(page);
            Items.Add(page);
        }
    }
}
