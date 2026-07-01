namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowTimer
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid WorkflowInstanceNodeId { get; set; }
    public string TimerType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime DueAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
