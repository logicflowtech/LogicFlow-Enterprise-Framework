namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class EscalationAssignment : BaseEntity
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = default!;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsActive { get; set; } = true;
}
