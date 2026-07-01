namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class PlatformFeature : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeprecated { get; set; }
    public ICollection<PlatformGroupFeature> GroupFeatures { get; set; } = new List<PlatformGroupFeature>();
    public ICollection<PlatformRoleFeature> RoleFeatures { get; set; } = new List<PlatformRoleFeature>();
}
