namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class PlatformAccessRole : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public ICollection<PlatformRoleFeature> RoleFeatures { get; set; } = new List<PlatformRoleFeature>();
    public ICollection<GroupAccessRoleAssignment> GroupAssignments { get; set; } = new List<GroupAccessRoleAssignment>();
}
