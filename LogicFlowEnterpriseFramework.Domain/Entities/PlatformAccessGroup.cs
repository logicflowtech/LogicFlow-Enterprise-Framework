namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class PlatformAccessGroup : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public ICollection<PlatformGroupFeature> GroupFeatures { get; set; } = new List<PlatformGroupFeature>();
    public ICollection<GroupAccessRoleAssignment> GroupRoles { get; set; } = new List<GroupAccessRoleAssignment>();
    public ICollection<UserAccessGroupAssignment> UserAssignments { get; set; } = new List<UserAccessGroupAssignment>();
}
