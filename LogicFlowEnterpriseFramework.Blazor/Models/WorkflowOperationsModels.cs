using System.Text.Json.Serialization;

namespace LogicFlowEnterpriseFramework.Blazor.Models;

public sealed class WorkflowDefinitionSummaryModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("draftDefinitionJson")]
    public string? DraftDefinitionJson { get; set; }

    [JsonPropertyName("definitionRowVersion")]
    public string DefinitionRowVersion { get; set; } = string.Empty;

    [JsonPropertyName("draftRowVersion")]
    public string? DraftRowVersion { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class CreateWorkflowDefinitionRequestModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("draftDefinitionJson")]
    public string? DraftDefinitionJson { get; set; }
}

public sealed class PublishWorkflowDefinitionRequestModel
{
    [JsonPropertyName("effectiveFromUtc")]
    public DateTime? EffectiveFromUtc { get; set; }

    [JsonPropertyName("effectiveToUtc")]
    public DateTime? EffectiveToUtc { get; set; }

    [JsonPropertyName("publishMessage")]
    public string? PublishMessage { get; set; }

    [JsonPropertyName("definitionRowVersion")]
    public string? DefinitionRowVersion { get; set; }

    [JsonPropertyName("draftRowVersion")]
    public string? DraftRowVersion { get; set; }
}

public sealed class StartWorkflowActionRequestModel
{
    [JsonPropertyName("businessKey")]
    public string? BusinessKey { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("variables")]
    public Dictionary<string, object?>? Variables { get; set; }
}

public sealed class WorkflowInstanceListItemModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("workflowDefinitionId")]
    public Guid WorkflowDefinitionId { get; set; }

    [JsonPropertyName("workflowDefinitionName")]
    public string WorkflowDefinitionName { get; set; } = string.Empty;

    [JsonPropertyName("workflowVersionId")]
    public Guid WorkflowVersionId { get; set; }

    [JsonPropertyName("workflowVersionNumber")]
    public int WorkflowVersionNumber { get; set; }

    [JsonPropertyName("businessKey")]
    public string? BusinessKey { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("currentNodeId")]
    public string? CurrentNodeId { get; set; }

    [JsonPropertyName("startedByUserId")]
    public Guid? StartedByUserId { get; set; }

    [JsonPropertyName("startedBy")]
    public string StartedBy { get; set; } = string.Empty;

    [JsonPropertyName("startedByDisplayName")]
    public string? StartedByDisplayName { get; set; }

    [JsonPropertyName("startedAtUtc")]
    public DateTime StartedAtUtc { get; set; }

    [JsonPropertyName("completedAtUtc")]
    public DateTime? CompletedAtUtc { get; set; }

    [JsonPropertyName("cancelledAtUtc")]
    public DateTime? CancelledAtUtc { get; set; }

    [JsonPropertyName("failedAtUtc")]
    public DateTime? FailedAtUtc { get; set; }
}

public sealed class WorkflowInstanceModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("workflowDefinitionId")]
    public Guid WorkflowDefinitionId { get; set; }

    [JsonPropertyName("workflowVersionId")]
    public Guid WorkflowVersionId { get; set; }

    [JsonPropertyName("businessKey")]
    public string? BusinessKey { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("currentNodeId")]
    public string? CurrentNodeId { get; set; }

    [JsonPropertyName("startedByUserId")]
    public Guid? StartedByUserId { get; set; }

    [JsonPropertyName("startedBy")]
    public string StartedBy { get; set; } = string.Empty;

    [JsonPropertyName("startedByDisplayName")]
    public string? StartedByDisplayName { get; set; }

    [JsonPropertyName("startedAtUtc")]
    public DateTime StartedAtUtc { get; set; }

    [JsonPropertyName("completedAtUtc")]
    public DateTime? CompletedAtUtc { get; set; }

    [JsonPropertyName("cancelledAtUtc")]
    public DateTime? CancelledAtUtc { get; set; }

    [JsonPropertyName("failedAtUtc")]
    public DateTime? FailedAtUtc { get; set; }
}

public sealed class WorkflowTaskModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("workflowInstanceId")]
    public Guid WorkflowInstanceId { get; set; }

    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    [JsonPropertyName("taskName")]
    public string TaskName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("taskMode")]
    public string TaskMode { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("entityType")]
    public string? EntityType { get; set; }

    [JsonPropertyName("entityId")]
    public string? EntityId { get; set; }

    [JsonPropertyName("assignedToUserId")]
    public Guid? AssignedToUserId { get; set; }

    [JsonPropertyName("assignedToGroupId")]
    public Guid? AssignedToGroupId { get; set; }

    [JsonPropertyName("assignedToRoleId")]
    public Guid? AssignedToRoleId { get; set; }

    [JsonPropertyName("assignmentType")]
    public string AssignmentType { get; set; } = string.Empty;

    [JsonPropertyName("assignedToDisplayName")]
    public string? AssignedToDisplayName { get; set; }

    [JsonPropertyName("claimRequired")]
    public bool ClaimRequired { get; set; }

    [JsonPropertyName("queueKey")]
    public string? QueueKey { get; set; }

    [JsonPropertyName("formKey")]
    public string? FormKey { get; set; }

    [JsonPropertyName("listViewKey")]
    public string? ListViewKey { get; set; }

    [JsonPropertyName("detailViewKey")]
    public string? DetailViewKey { get; set; }

    [JsonPropertyName("displayMetadataJson")]
    public string? DisplayMetadataJson { get; set; }

    [JsonPropertyName("dueAtUtc")]
    public DateTime? DueAtUtc { get; set; }

    [JsonPropertyName("reminderAtUtc")]
    public DateTime? ReminderAtUtc { get; set; }

    [JsonPropertyName("escalationAtUtc")]
    public DateTime? EscalationAtUtc { get; set; }

    [JsonPropertyName("escalationPolicyKey")]
    public string? EscalationPolicyKey { get; set; }

    [JsonPropertyName("slaStatus")]
    public string? SlaStatus { get; set; }

    [JsonPropertyName("escalatedAtUtc")]
    public DateTime? EscalatedAtUtc { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("claimedBy")]
    public string? ClaimedBy { get; set; }

    [JsonPropertyName("claimedByUserId")]
    public Guid? ClaimedByUserId { get; set; }

    [JsonPropertyName("claimedAtUtc")]
    public DateTime? ClaimedAtUtc { get; set; }

    [JsonPropertyName("completedBy")]
    public string? CompletedBy { get; set; }

    [JsonPropertyName("completedByUserId")]
    public Guid? CompletedByUserId { get; set; }

    [JsonPropertyName("completedAtUtc")]
    public DateTime? CompletedAtUtc { get; set; }

    [JsonPropertyName("completionAction")]
    public string? CompletionAction { get; set; }

    [JsonPropertyName("availableActions")]
    public List<WorkflowTaskActionModel> AvailableActions { get; set; } = [];

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public sealed class WorkflowTaskActionModel
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("style")]
    public string Style { get; set; } = string.Empty;

    [JsonPropertyName("requiresComment")]
    public bool RequiresComment { get; set; }
}

public sealed class WorkflowTaskCommentModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("workflowTaskId")]
    public Guid WorkflowTaskId { get; set; }

    [JsonPropertyName("commentType")]
    public string CommentType { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = string.Empty;

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonPropertyName("createdByUserId")]
    public Guid? CreatedByUserId { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class WorkflowTaskAssignmentModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("workflowTaskId")]
    public Guid WorkflowTaskId { get; set; }

    [JsonPropertyName("actionType")]
    public string ActionType { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("performedBy")]
    public string PerformedBy { get; set; } = string.Empty;

    [JsonPropertyName("performedByUserId")]
    public Guid? PerformedByUserId { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class WorkflowVariableModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("workflowInstanceId")]
    public Guid WorkflowInstanceId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class WorkflowAuditLogModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("workflowInstanceId")]
    public Guid WorkflowInstanceId { get; set; }

    [JsonPropertyName("workflowTaskId")]
    public Guid? WorkflowTaskId { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("fromNodeId")]
    public string? FromNodeId { get; set; }

    [JsonPropertyName("toNodeId")]
    public string? ToNodeId { get; set; }

    [JsonPropertyName("performedByUserId")]
    public Guid? PerformedByUserId { get; set; }

    [JsonPropertyName("performedBy")]
    public string? PerformedBy { get; set; }

    [JsonPropertyName("performedByDisplayName")]
    public string? PerformedByDisplayName { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class WorkflowVersionDetailModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("workflowDefinitionId")]
    public Guid WorkflowDefinitionId { get; set; }

    [JsonPropertyName("versionNumber")]
    public int VersionNumber { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("effectiveFromUtc")]
    public DateTime? EffectiveFromUtc { get; set; }

    [JsonPropertyName("effectiveToUtc")]
    public DateTime? EffectiveToUtc { get; set; }

    [JsonPropertyName("publishedBy")]
    public string? PublishedBy { get; set; }

    [JsonPropertyName("publishedAtUtc")]
    public DateTime? PublishedAtUtc { get; set; }

    [JsonPropertyName("publishMessage")]
    public string? PublishMessage { get; set; }

    [JsonPropertyName("definitionJson")]
    public string DefinitionJson { get; set; } = string.Empty;
}

public sealed class WorkflowInstanceDetailModel
{
    [JsonPropertyName("instance")]
    public WorkflowInstanceModel Instance { get; set; } = new();

    [JsonPropertyName("version")]
    public WorkflowVersionDetailModel Version { get; set; } = new();

    [JsonPropertyName("tasks")]
    public List<WorkflowTaskModel> Tasks { get; set; } = [];

    [JsonPropertyName("variables")]
    public List<WorkflowVariableModel> Variables { get; set; } = [];

    [JsonPropertyName("auditLogs")]
    public List<WorkflowAuditLogModel> AuditLogs { get; set; } = [];

    [JsonPropertyName("taskComments")]
    public List<WorkflowTaskCommentModel> TaskComments { get; set; } = [];

    [JsonPropertyName("taskAssignments")]
    public List<WorkflowTaskAssignmentModel> TaskAssignments { get; set; } = [];
}

public sealed class WorkflowTaskDetailModel
{
    [JsonPropertyName("task")]
    public WorkflowTaskModel Task { get; set; } = new();

    [JsonPropertyName("comments")]
    public List<WorkflowTaskCommentModel> Comments { get; set; } = [];

    [JsonPropertyName("assignments")]
    public List<WorkflowTaskAssignmentModel> Assignments { get; set; } = [];
}

public sealed class CompleteTaskActionRequestModel
{
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public sealed class DelegateTaskActionRequestModel
{
    [JsonPropertyName("targetUserId")]
    public Guid TargetUserId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public sealed class ReassignTaskActionRequestModel
{
    [JsonPropertyName("targetUserId")]
    public Guid? TargetUserId { get; set; }

    [JsonPropertyName("targetGroupId")]
    public Guid? TargetGroupId { get; set; }

    [JsonPropertyName("targetRoleId")]
    public Guid? TargetRoleId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public sealed class AddTaskCommentActionRequestModel
{
    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }
}

public sealed class WorkflowUserLookupModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

public sealed class CancelWorkflowInstanceActionRequestModel
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
