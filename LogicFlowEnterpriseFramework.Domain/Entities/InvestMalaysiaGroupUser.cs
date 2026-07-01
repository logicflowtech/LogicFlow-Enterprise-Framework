namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class InvestMalaysiaGroupUser
{
    public int LegacyGroupId { get; set; }
    public InvestMalaysiaGroup Group { get; set; } = null!;
    public int LegacyUserId { get; set; }
    public InvestMalaysiaUser User { get; set; } = null!;
    public DateTime LastSyncedAt { get; set; }
}
