namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowVersion
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public int VersionNumber { get; set; }
    public string DefinitionJson { get; set; } = string.Empty;
    public string? CompiledDefinitionJson { get; set; }
    public string Status { get; set; } = "Published";
    public DateTime? EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public string? PublishedBy { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public string? PublishMessage { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
