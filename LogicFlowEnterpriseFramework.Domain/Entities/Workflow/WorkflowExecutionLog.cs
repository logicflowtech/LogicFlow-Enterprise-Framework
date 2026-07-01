namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowExecutionLog
{
    public long Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid? WorkflowInstanceNodeId { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? DataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
