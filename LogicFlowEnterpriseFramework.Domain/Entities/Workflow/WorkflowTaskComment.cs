namespace LogicFlowEnterpriseFramework.Domain.Entities.Workflow;

public sealed class WorkflowTaskComment
{
    public Guid Id { get; set; }
    public Guid WorkflowTaskId { get; set; }
    public string CommentType { get; set; } = "Comment";
    public string Body { get; set; } = string.Empty;
    public string Visibility { get; set; } = "Internal";
    public string CreatedBy { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
