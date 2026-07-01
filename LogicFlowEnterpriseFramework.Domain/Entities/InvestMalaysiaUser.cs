namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class InvestMalaysiaUser
{
    public int LegacyUserId { get; set; }
    public int LegacyTenantId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MobilePhone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public DateTime LastSyncedAt { get; set; }
    public ICollection<InvestMalaysiaGroupUser> GroupAssignments { get; set; } = new List<InvestMalaysiaGroupUser>();
    public ICollection<InvestMalaysiaUserRole> DirectRoleAssignments { get; set; } = new List<InvestMalaysiaUserRole>();
}
