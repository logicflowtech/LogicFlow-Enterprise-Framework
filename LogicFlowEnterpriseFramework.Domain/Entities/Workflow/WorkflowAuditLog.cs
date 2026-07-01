namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowAuditLog
{
    public long Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid? WorkflowTaskId { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? FromNodeId { get; set; }
    public string? ToNodeId { get; set; }
    public string? DataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
