namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class InvestMalaysiaRole
{
    public int LegacyRoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public ICollection<InvestMalaysiaGroupRole> GroupAssignments { get; set; } = new List<InvestMalaysiaGroupRole>();
    public ICollection<InvestMalaysiaUserRole> UserAssignments { get; set; } = new List<InvestMalaysiaUserRole>();
}
