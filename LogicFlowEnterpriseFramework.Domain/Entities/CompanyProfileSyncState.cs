namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class CompanyProfileSyncState
{
    public string SourceName { get; set; } = string.Empty;
    public DateTime? LastSourceModifiedDateTime { get; set; }
    public long? LastSourceCompanyId { get; set; }
    public DateTime? LastStartedAt { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public bool? LastRunSucceeded { get; set; }
    public int LastProcessedRows { get; set; }
    public string? LastRunMessage { get; set; }
}
