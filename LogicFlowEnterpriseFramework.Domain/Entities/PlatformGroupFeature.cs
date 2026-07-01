namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class PlatformGroupFeature : BaseEntity
{
    public Guid PlatformAccessGroupId { get; set; }
    public PlatformAccessGroup PlatformAccessGroup { get; set; } = null!;
    public Guid PlatformFeatureId { get; set; }
    public PlatformFeature PlatformFeature { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
}
