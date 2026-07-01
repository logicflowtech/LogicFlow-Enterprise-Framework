namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class ServiceCenterUserAccess : BaseEntity
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = default!;
    public bool IsAccessEnabled { get; set; } = true;
}
