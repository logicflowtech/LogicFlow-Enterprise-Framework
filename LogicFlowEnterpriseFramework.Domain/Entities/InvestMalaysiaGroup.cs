namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class InvestMalaysiaGroup
{
    public int LegacyGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public ICollection<InvestMalaysiaGroupUser> UserAssignments { get; set; } = new List<InvestMalaysiaGroupUser>();
    public ICollection<InvestMalaysiaGroupRole> RoleAssignments { get; set; } = new List<InvestMalaysiaGroupRole>();
}
