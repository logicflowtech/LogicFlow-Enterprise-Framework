namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class UserAccessGroupAssignment : BaseEntity
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = null!;
    public Guid PlatformAccessGroupId { get; set; }
    public PlatformAccessGroup PlatformAccessGroup { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
}
