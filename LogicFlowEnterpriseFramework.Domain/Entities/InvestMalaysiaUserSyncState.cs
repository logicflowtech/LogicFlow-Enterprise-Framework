namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class InvestMalaysiaUserSyncState
{
    public string SourceName { get; set; } = string.Empty;
    public DateTime? LastStartedAt { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public bool? LastRunSucceeded { get; set; }
    public int LastProcessedRows { get; set; }
    public string? LastRunMessage { get; set; }
}
