namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class InvestMalaysiaUserRole
{
    public int LegacyUserId { get; set; }
    public InvestMalaysiaUser User { get; set; } = null!;
    public int LegacyRoleId { get; set; }
    public InvestMalaysiaRole Role { get; set; } = null!;
    public DateTime LastSyncedAt { get; set; }
}
