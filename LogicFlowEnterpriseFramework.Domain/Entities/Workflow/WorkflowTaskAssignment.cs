namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowTaskAssignment
{
    public Guid Id { get; set; }
    public Guid WorkflowTaskId { get; set; }
    public string ActionType { get; set; } = "Assigned";
    public Guid? FromUserId { get; set; }
    public Guid? FromGroupId { get; set; }
    public Guid? FromRoleId { get; set; }
    public Guid? ToUserId { get; set; }
    public Guid? ToGroupId { get; set; }
    public Guid? ToRoleId { get; set; }
    public string? Reason { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public Guid? PerformedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
