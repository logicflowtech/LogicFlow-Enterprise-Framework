namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowInstanceNode
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string? NodeName { get; set; }
    public string? BranchKey { get; set; }
    public string? JoinGroupKey { get; set; }
    public string ExecutionStatus { get; set; } = "Pending";
    public int SequenceNo { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? TokenJson { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
