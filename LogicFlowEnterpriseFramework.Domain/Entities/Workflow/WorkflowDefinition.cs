namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowDefinition
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string WorkflowCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public int LatestVersionNumber { get; set; }
    public Guid? CurrentDraftId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
