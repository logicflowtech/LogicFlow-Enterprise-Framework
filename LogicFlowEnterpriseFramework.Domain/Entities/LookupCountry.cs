namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class LookupCountry
{
    public Guid Id { get; set; }
    public long? MigratedId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
