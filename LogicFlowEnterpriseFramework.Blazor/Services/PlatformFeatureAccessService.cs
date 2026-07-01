namespace LogicFlowEnterpriseFramework.Blazor.Services;

public sealed class PlatformFeatureAccessService(AuthSession session) : IPlatformFeatureAccessService
{
    public IReadOnlyCollection<string> GrantedFeatures => session.User?.FeatureCodes ?? [];

    public bool HasFeature(string featureCode)
    {
        if (string.IsNullOrWhiteSpace(featureCode) || !session.IsAuthenticated)
        {
            return false;
        }

        return GrantedFeatures.Contains(featureCode, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasAnyFeature(params string[] featureCodes)
    {
        if (!session.IsAuthenticated || featureCodes.Length == 0)
        {
            return false;
        }

        return featureCodes.Any(HasFeature);
    }
}
