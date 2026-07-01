namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class InvestMalaysiaGroupRole
{
    public int LegacyGroupId { get; set; }
    public InvestMalaysiaGroup Group { get; set; } = null!;
    public int LegacyRoleId { get; set; }
    public InvestMalaysiaRole Role { get; set; } = null!;
    public DateTime LastSyncedAt { get; set; }
}
