namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowEventSubscription
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid WorkflowInstanceNodeId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? CorrelationKey { get; set; }
    public string Status { get; set; } = "Waiting";
    public string? PayloadSchemaJson { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? SatisfiedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
