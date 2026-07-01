namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowTask
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid WorkflowInstanceNodeId { get; set; }
    public string? TaskCode { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending";
    public string TaskMode { get; set; } = "approval";
    public string? Priority { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? FormKey { get; set; }
    public string? ListViewKey { get; set; }
    public string? DetailViewKey { get; set; }
    public string? AvailableActionsJson { get; set; }
    public string? DisplayMetadataJson { get; set; }
    public string? TaskTagsJson { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? AssignedToGroupId { get; set; }
    public Guid? AssignedToRoleId { get; set; }
    public string AssignmentType { get; set; } = "User";
    public string? AssignedToDisplayName { get; set; }
    public bool ClaimRequired { get; set; }
    public string? QueueKey { get; set; }
    public string? ClaimedBy { get; set; }
    public Guid? ClaimedByUserId { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public string? CompletedBy { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? ReminderAtUtc { get; set; }
    public DateTime? EscalationAtUtc { get; set; }
    public string? EscalationPolicyKey { get; set; }
    public string? SlaStatus { get; set; }
    public DateTime? EscalatedAtUtc { get; set; }
    public string? Outcome { get; set; }
    public string? InputPayloadJson { get; set; }
    public string? OutputPayloadJson { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
