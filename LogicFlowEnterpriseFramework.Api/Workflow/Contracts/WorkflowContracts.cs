namespace LogicFlowEnterpriseFramework.Api.Workflow.Contracts;

public sealed record CreateWorkflowDefinitionRequest(
    string Name,
    string? Description,
    string? DraftDefinitionJson);

public sealed record UpdateWorkflowDraftRequest(
    string? Name,
    string? Description,
    string DraftDefinitionJson,
    string? DefinitionRowVersion,
    string? DraftRowVersion);

public sealed record PublishWorkflowDefinitionRequest(
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    string? PublishMessage,
    string? DefinitionRowVersion,
    string? DraftRowVersion);

public sealed record ValidateWorkflowDefinitionRequest(
    string DefinitionJson);

public sealed record WorkflowValidationResponse(
    bool IsValid,
    IReadOnlyList<string> Errors);

public sealed record WorkflowDefinitionResponse(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    string? DraftDefinitionJson,
    string DefinitionRowVersion,
    string? DraftRowVersion,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record WorkflowVersionResponse(
    Guid Id,
    Guid WorkflowDefinitionId,
    int VersionNumber,
    string Status,
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    string? PublishedBy,
    DateTime? PublishedAtUtc,
    string? PublishMessage);

public sealed record WorkflowVersionDetailResponse(
    Guid Id,
    Guid WorkflowDefinitionId,
    int VersionNumber,
    string Status,
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    string? PublishedBy,
    DateTime? PublishedAtUtc,
    string? PublishMessage,
    string DefinitionJson);

public sealed record StartWorkflowActionRequest(
    string? BusinessKey,
    string? Title,
    IReadOnlyDictionary<string, object?>? Variables);

public sealed record StartWorkflowRequest(
    string? BusinessKey,
    string? Title,
    Guid? StartedByUserId,
    string StartedBy,
    string? StartedByDisplayName,
    IReadOnlyDictionary<string, object?>? Variables);

public sealed record CompleteTaskRequest(
    Guid? CompletedByUserId,
    string CompletedBy,
    string? CompletedByDisplayName,
    string? Comment);

public sealed record CompleteTaskActionRequest(string? Comment);

public sealed record DelegateTaskActionRequest(
    Guid TargetUserId,
    string? Reason);

public sealed record ReassignTaskActionRequest(
    Guid? TargetUserId,
    Guid? TargetGroupId,
    Guid? TargetRoleId,
    string? Reason);

public sealed record AddTaskCommentActionRequest(
    string Comment,
    string? Visibility);

public sealed record ClaimTaskRequest(
    Guid ClaimedByUserId,
    string ClaimedBy,
    string? ClaimedByDisplayName);

public sealed record UnclaimTaskRequest(
    Guid UserId,
    string UserName,
    string? DisplayName);

public sealed record DelegateTaskRequest(
    Guid RequestedByUserId,
    string RequestedBy,
    string? RequestedByDisplayName,
    Guid TargetUserId,
    string? Reason);

public sealed record ReassignTaskRequest(
    Guid? RequestedByUserId,
    string RequestedBy,
    string? RequestedByDisplayName,
    Guid? TargetUserId,
    Guid? TargetGroupId,
    Guid? TargetRoleId,
    string? Reason);

public sealed record AddTaskCommentRequest(
    Guid? CreatedByUserId,
    string CreatedBy,
    string? CreatedByDisplayName,
    string Comment,
    string? Visibility);

public sealed record CancelWorkflowInstanceActionRequest(string? Reason);

public sealed record CancelWorkflowInstanceRequest(
    Guid? CancelledByUserId,
    string CancelledBy,
    string? CancelledByDisplayName,
    string? Reason);

public sealed record WorkflowInstanceResponse(
    Guid Id,
    Guid WorkflowDefinitionId,
    Guid WorkflowVersionId,
    string? BusinessKey,
    string? Title,
    string Status,
    string? CurrentNodeId,
    Guid? StartedByUserId,
    string StartedBy,
    string? StartedByDisplayName,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc,
    DateTime? FailedAtUtc);

public sealed record WorkflowInstanceListItemResponse(
    Guid Id,
    Guid WorkflowDefinitionId,
    string WorkflowDefinitionName,
    Guid WorkflowVersionId,
    int WorkflowVersionNumber,
    string? BusinessKey,
    string? Title,
    string Status,
    string? CurrentNodeId,
    Guid? StartedByUserId,
    string StartedBy,
    string? StartedByDisplayName,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc,
    DateTime? FailedAtUtc);

public sealed record WorkflowTaskResponse(
    Guid Id,
    Guid WorkflowInstanceId,
    string NodeId,
    string TaskName,
    string Status,
    string TaskMode,
    string? Priority,
    string? EntityType,
    string? EntityId,
    Guid? AssignedToUserId,
    Guid? AssignedToGroupId,
    Guid? AssignedToRoleId,
    string AssignmentType,
    string? AssignedToDisplayName,
    bool ClaimRequired,
    string? QueueKey,
    string? FormKey,
    string? ListViewKey,
    string? DetailViewKey,
    string? DisplayMetadataJson,
    DateTime? DueAtUtc,
    DateTime? ReminderAtUtc,
    DateTime? EscalationAtUtc,
    string? EscalationPolicyKey,
    string? SlaStatus,
    DateTime? EscalatedAtUtc,
    DateTime CreatedAtUtc,
    string? ClaimedBy,
    Guid? ClaimedByUserId,
    DateTime? ClaimedAtUtc,
    string? CompletedBy,
    Guid? CompletedByUserId,
    DateTime? CompletedAtUtc,
    string? CompletionAction,
    IReadOnlyList<WorkflowTaskActionResponse> AvailableActions,
    string? Comment);

public sealed record WorkflowTaskActionResponse(
    string Code,
    string Label,
    string Style,
    bool RequiresComment);

public sealed record WorkflowTaskCommentResponse(
    Guid Id,
    Guid WorkflowTaskId,
    string CommentType,
    string Body,
    string Visibility,
    string CreatedBy,
    Guid? CreatedByUserId,
    DateTime CreatedAtUtc);

public sealed record WorkflowTaskAssignmentResponse(
    Guid Id,
    Guid WorkflowTaskId,
    string ActionType,
    Guid? FromUserId,
    Guid? FromGroupId,
    Guid? FromRoleId,
    Guid? ToUserId,
    Guid? ToGroupId,
    Guid? ToRoleId,
    string? Reason,
    string PerformedBy,
    Guid? PerformedByUserId,
    DateTime CreatedAtUtc);

public sealed record WorkflowVariableResponse(
    Guid Id,
    Guid WorkflowInstanceId,
    string Name,
    string? Value,
    string DataType,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record WorkflowAuditLogResponse(
    long Id,
    Guid WorkflowInstanceId,
    Guid? WorkflowTaskId,
    string Action,
    string? FromNodeId,
    string? ToNodeId,
    Guid? PerformedByUserId,
    string? PerformedBy,
    string? PerformedByDisplayName,
    string? Message,
    DateTime CreatedAtUtc);

public sealed record WorkflowInstanceDetailResponse(
    WorkflowInstanceResponse Instance,
    WorkflowVersionDetailResponse Version,
    IReadOnlyList<WorkflowTaskResponse> Tasks,
    IReadOnlyList<WorkflowVariableResponse> Variables,
    IReadOnlyList<WorkflowAuditLogResponse> AuditLogs,
    IReadOnlyList<WorkflowTaskCommentResponse> TaskComments,
    IReadOnlyList<WorkflowTaskAssignmentResponse> TaskAssignments);

public sealed record WorkflowTaskDetailResponse(
    WorkflowTaskResponse Task,
    IReadOnlyList<WorkflowTaskCommentResponse> Comments,
    IReadOnlyList<WorkflowTaskAssignmentResponse> Assignments);

public sealed record WorkflowUserLookupResponse(
    Guid Id,
    string UserName,
    string DisplayName,
    string? Email,
    bool IsActive);

public sealed record WorkflowGroupLookupResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);

public sealed record WorkflowRoleLookupResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);
