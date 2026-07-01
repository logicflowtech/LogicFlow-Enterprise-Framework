namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowDraft
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public string DraftJson { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public string ValidationStatus { get; set; } = "Pending";
    public string? ValidationErrorsJson { get; set; }
    public string? DesignerMetadataJson { get; set; }
    public string? LockedBy { get; set; }
    public Guid? LockedByUserId { get; set; }
    public DateTime? LockedAtUtc { get; set; }
    public DateTime? LastAutosavedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
