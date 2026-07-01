namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class GroupAccessRoleAssignment : BaseEntity
{
    public Guid PlatformAccessGroupId { get; set; }
    public PlatformAccessGroup PlatformAccessGroup { get; set; } = null!;
    public Guid PlatformAccessRoleId { get; set; }
    public PlatformAccessRole PlatformAccessRole { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
}
