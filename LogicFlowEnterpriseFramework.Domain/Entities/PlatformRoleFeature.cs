namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class PlatformRoleFeature : BaseEntity
{
    public Guid PlatformAccessRoleId { get; set; }
    public PlatformAccessRole PlatformAccessRole { get; set; } = null!;
    public Guid PlatformFeatureId { get; set; }
    public PlatformFeature PlatformFeature { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
}
