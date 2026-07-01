namespace LogicFlowEnterpriseFramework.Blazor.Services;

public interface IPlatformFeatureAccessService
{
    IReadOnlyCollection<string> GrantedFeatures { get; }
    bool HasFeature(string featureCode);
    bool HasAnyFeature(params string[] featureCodes);
}
