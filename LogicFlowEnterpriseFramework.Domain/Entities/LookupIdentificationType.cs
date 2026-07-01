namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class LookupIdentificationType
{
    public Guid Id { get; set; }
    public int? MigratedId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
