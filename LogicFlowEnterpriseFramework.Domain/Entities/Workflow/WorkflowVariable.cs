namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowVariable
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string VariableName { get; set; } = string.Empty;
    public string? ValueJson { get; set; }
    public string ValueType { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
