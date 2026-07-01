namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class InvestMalaysiaGroupMapping : BaseEntity
{
    public string InvestMalaysiaGroupName { get; set; } = string.Empty;
    public string NormalizedInvestMalaysiaGroupName { get; set; } = string.Empty;
    public Guid PlatformAccessGroupId { get; set; }
    public PlatformAccessGroup PlatformAccessGroup { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
