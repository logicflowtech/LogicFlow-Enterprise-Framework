namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowOutbox
{
    public long Id { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? DeadLetteredAtUtc { get; set; }
    public string? ProcessorName { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? HeadersJson { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? LockId { get; set; }
    public DateTime? LockedAtUtc { get; set; }
}
