namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowInstance
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public Guid WorkflowVersionId { get; set; }
    public Guid? ParentWorkflowInstanceId { get; set; }
    public Guid? RootWorkflowInstanceId { get; set; }
    public string? BusinessKey { get; set; }
    public string? CorrelationId { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; } = "Running";
    public int CurrentNodeCount { get; set; }
    public string StartedBy { get; set; } = string.Empty;
    public Guid? StartedByUserId { get; set; }
    public string? StartedByDisplayName { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? LastHeartbeatUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
