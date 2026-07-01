using System.Text.Json;
using System.Xml;
using LogicFlowEnterpriseFramework.Api.Workflow.Contracts;
using LogicFlowEnterpriseFramework.Domain.Entities.Workflow;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogicFlowEnterpriseFramework.Api.Workflow.Runtime;

public sealed class WorkflowRuntimeService(ApplicationDbContext dbContext, ILogger<WorkflowRuntimeService> logger)
{
    private static readonly IReadOnlyList<WorkflowTaskActionResponse> EmptyTaskActions = [];

    public async Task<IReadOnlyList<WorkflowInstanceListItemResponse>> GetInstancesAsync(
        Guid? actorUserId,
        bool isWorkflowAdministrator,
        Guid? workflowDefinitionId,
        string? status,
        string? search,
        int take,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 200);

        var query =
            from instance in dbContext.WorkflowInstances.AsNoTracking()
            join definition in dbContext.WorkflowDefinitions.AsNoTracking() on instance.WorkflowDefinitionId equals definition.Id
            join version in dbContext.WorkflowVersions.AsNoTracking() on instance.WorkflowVersionId equals version.Id
            select new
            {
                Instance = instance,
                DefinitionName = definition.Name,
                VersionNumber = version.VersionNumber
            };

        if (workflowDefinitionId.HasValue)
        {
            query = query.Where(x => x.Instance.WorkflowDefinitionId == workflowDefinitionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(x => x.Instance.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.DefinitionName, term)
                || (x.Instance.Title != null && EF.Functions.Like(x.Instance.Title, term))
                || (x.Instance.BusinessKey != null && EF.Functions.Like(x.Instance.BusinessKey, term))
                || EF.Functions.Like(x.Instance.StartedBy, term)
                || (x.Instance.StartedByDisplayName != null && EF.Functions.Like(x.Instance.StartedByDisplayName, term)));
        }

        if (!isWorkflowAdministrator)
        {
            if (!actorUserId.HasValue)
            {
                return [];
            }

            var userId = actorUserId.Value;
            var roleIds = await GetUserRoleIdsAsync(userId, cancellationToken);
            var groupIds = await GetUserGroupIdsAsync(userId, cancellationToken);

            query = query.Where(x =>
                x.Instance.StartedByUserId == userId
                || dbContext.WorkflowTasks.Any(task =>
                    task.WorkflowInstanceId == x.Instance.Id
                    && (task.AssignedToUserId == userId
                        || task.ClaimedByUserId == userId
                        || task.CompletedByUserId == userId
                        || (task.AssignedToGroupId.HasValue && groupIds.Contains(task.AssignedToGroupId.Value))
                        || (task.AssignedToRoleId.HasValue && roleIds.Contains(task.AssignedToRoleId.Value)))));
        }

        return await query
            .OrderByDescending(x => x.Instance.StartedAtUtc)
            .Take(take)
            .Select(x => new WorkflowInstanceListItemResponse(
                x.Instance.Id,
                x.Instance.WorkflowDefinitionId,
                x.DefinitionName,
                x.Instance.WorkflowVersionId,
                x.VersionNumber,
                x.Instance.BusinessKey,
                x.Instance.Title,
                x.Instance.Status,
                dbContext.WorkflowInstanceNodes.AsNoTracking()
                    .Where(node => node.WorkflowInstanceId == x.Instance.Id)
                    .OrderByDescending(node => node.ActivatedAtUtc ?? DateTime.MinValue)
                    .ThenByDescending(node => node.SequenceNo)
                    .Select(node => node.NodeId)
                    .FirstOrDefault(),
                x.Instance.StartedByUserId,
                x.Instance.StartedBy,
                x.Instance.StartedByDisplayName,
                x.Instance.StartedAtUtc,
                x.Instance.CompletedAtUtc,
                x.Instance.CancelledAtUtc,
                x.Instance.FailedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowInstanceResponse> StartAsync(
        Guid workflowDefinitionId,
        StartWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var version = await dbContext.WorkflowVersions
            .Where(x => x.WorkflowDefinitionId == workflowDefinitionId
                && x.Status == "Published"
                && (x.EffectiveFromUtc == null || x.EffectiveFromUtc <= now)
                && (x.EffectiveToUtc == null || x.EffectiveToUtc > now))
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (version is null)
        {
            throw new WorkflowRuntimeException("No effective published workflow version was found.");
        }

        var document = WorkflowDefinitionDocument.Parse(version.DefinitionJson);
        var startNode = document.GetStartNode();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            TenantId = GetTenantId(request.StartedByUserId),
            WorkflowDefinitionId = workflowDefinitionId,
            WorkflowVersionId = version.Id,
            BusinessKey = string.IsNullOrWhiteSpace(request.BusinessKey) ? null : request.BusinessKey.Trim(),
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            Status = "Running",
            CurrentNodeCount = 0,
            StartedBy = request.StartedBy.Trim(),
            StartedByUserId = request.StartedByUserId,
            StartedByDisplayName = string.IsNullOrWhiteSpace(request.StartedByDisplayName) ? null : request.StartedByDisplayName.Trim(),
            StartedAtUtc = now,
            LastHeartbeatUtc = now
        };

        dbContext.WorkflowInstances.Add(instance);
        await dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(instance.Id, null, "WorkflowStarted", null, startNode.Id, request.StartedByUserId, request.StartedBy, request.StartedByDisplayName, "Workflow instance started.", null, now);

        if (request.Variables is not null)
        {
            foreach (var variable in request.Variables)
            {
                dbContext.WorkflowVariables.Add(new WorkflowVariable
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = instance.Id,
                    VariableName = variable.Key,
                    ValueJson = JsonSerializer.Serialize(variable.Value),
                    ValueType = ResolveVariableDataType(variable.Value),
                    CreatedAtUtc = now
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await AdvanceAsync(instance, document, startNode.Id, request.StartedByUserId, request.StartedBy, request.StartedByDisplayName, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await BuildInstanceResponseAsync(instance, cancellationToken);
    }

    public async Task<WorkflowInstanceResponse?> GetInstanceAsync(Guid id, CancellationToken cancellationToken)
    {
        var instance = await dbContext.WorkflowInstances.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return instance is null ? null : await BuildInstanceResponseAsync(instance, cancellationToken);
    }

    public async Task<int> ProcessDueTimersAsync(int batchSize, CancellationToken cancellationToken)
    {
        batchSize = Math.Clamp(batchSize, 1, 200);
        var now = DateTime.UtcNow;
        var processed = 0;

        var timers = await dbContext.WorkflowTimers
            .Where(x => x.Status == "Pending" && x.DueAtUtc <= now)
            .OrderBy(x => x.DueAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var timer in timers)
        {
            try
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                var instance = await dbContext.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == timer.WorkflowInstanceId, cancellationToken);
                var nodeExecution = await dbContext.WorkflowInstanceNodes.FirstOrDefaultAsync(x => x.Id == timer.WorkflowInstanceNodeId, cancellationToken);

                if (instance is null || nodeExecution is null)
                {
                    timer.Status = "Failed";
                    timer.ProcessedAtUtc = now;
                    dbContext.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
                    {
                        WorkflowInstanceId = timer.WorkflowInstanceId,
                        WorkflowInstanceNodeId = timer.WorkflowInstanceNodeId,
                        LogLevel = "Error",
                        EventType = "TimerProcessingFailed",
                        Message = "Workflow timer references a missing instance or node.",
                        CreatedAtUtc = now
                    });
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    continue;
                }

                if (timer.Status != "Pending")
                {
                    continue;
                }

                var version = await dbContext.WorkflowVersions.AsNoTracking().FirstAsync(x => x.Id == instance.WorkflowVersionId, cancellationToken);
                var document = WorkflowDefinitionDocument.Parse(version.DefinitionJson);

                timer.Status = "Processed";
                timer.ProcessedAtUtc = now;
                await MarkNodeCompletedAsync(nodeExecution.Id, now, cancellationToken);

                dbContext.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
                {
                    WorkflowInstanceId = instance.Id,
                    WorkflowInstanceNodeId = nodeExecution.Id,
                    LogLevel = "Info",
                    EventType = "TimerProcessed",
                    Message = $"Workflow timer '{timer.TimerType}' processed.",
                    DataJson = timer.PayloadJson,
                    CreatedAtUtc = now
                });

                AddAudit(instance.Id, null, "TimerProcessed", nodeExecution.NodeId, nodeExecution.NodeId, null, "system", "Workflow background processor", "Timer resumed workflow execution.", timer.PayloadJson, now);
                await AdvanceAsync(instance, document, nodeExecution.NodeId, null, "system", "Workflow background processor", now, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                processed++;
            }
            catch (Exception exception)
            {
                timer.Status = "Failed";
                timer.ProcessedAtUtc = now;
                dbContext.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
                {
                    WorkflowInstanceId = timer.WorkflowInstanceId,
                    WorkflowInstanceNodeId = timer.WorkflowInstanceNodeId,
                    LogLevel = "Error",
                    EventType = "TimerProcessingFailed",
                    Message = exception.Message,
                    DataJson = timer.PayloadJson,
                    CreatedAtUtc = now
                });
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogError(exception, "Workflow timer {TimerId} failed during background processing.", timer.Id);
            }
        }

        return processed;
    }

    public async Task<int> ProcessOutboxAsync(int batchSize, CancellationToken cancellationToken)
    {
        batchSize = Math.Clamp(batchSize, 1, 200);
        var now = DateTime.UtcNow;
        var processed = 0;
        var lockId = Guid.NewGuid();

        var messages = await dbContext.WorkflowOutbox
            .Where(x =>
                (x.Status == "Pending" || x.Status == "Failed")
                && (!x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc <= now))
            .OrderBy(x => x.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                message.Status = "Processing";
                message.LockId = lockId;
                message.LockedAtUtc = now;
                message.LastAttemptAtUtc = now;
            }
            catch
            {
            }
        }

        if (messages.Count == 0)
        {
            return 0;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                dbContext.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
                {
                    WorkflowInstanceId = Guid.TryParse(message.AggregateId, out var aggregateId) ? aggregateId : Guid.Empty,
                    WorkflowInstanceNodeId = null,
                    LogLevel = "Info",
                    EventType = "OutboxProcessed",
                    Message = $"Processed outbox event '{message.EventType}'.",
                    DataJson = message.PayloadJson,
                    CreatedAtUtc = DateTime.UtcNow
                });

                message.Status = "Processed";
                message.ProcessedAtUtc = DateTime.UtcNow;
                message.ErrorMessage = null;
                message.ErrorCode = null;
                message.LockId = null;
                message.LockedAtUtc = null;
                message.NextAttemptAtUtc = null;
                processed++;
            }
            catch (Exception exception)
            {
                message.RetryCount += 1;
                message.Status = message.RetryCount >= 5 ? "DeadLettered" : "Failed";
                message.DeadLetteredAtUtc = message.Status == "DeadLettered" ? DateTime.UtcNow : null;
                message.NextAttemptAtUtc = message.Status == "DeadLettered" ? null : DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, message.RetryCount)));
                message.ErrorCode = "OutboxProcessingFailed";
                message.ErrorMessage = exception.Message;
                message.LockId = null;
                message.LockedAtUtc = null;
                logger.LogError(exception, "Workflow outbox message {OutboxId} failed during processing.", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return processed;
    }

    public async Task<WorkflowInstanceDetailResponse?> GetInstanceDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var instance = await dbContext.WorkflowInstances.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (instance is null)
        {
            return null;
        }

        var version = await dbContext.WorkflowVersions.AsNoTracking().FirstAsync(x => x.Id == instance.WorkflowVersionId, cancellationToken);
        var taskEntities = await dbContext.WorkflowTasks.AsNoTracking()
            .Where(x => x.WorkflowInstanceId == id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var tasks = taskEntities.Select(x => ToTaskResponse(x, actorUserId: null, isWorkflowAdministrator: false)).ToList();
        var variables = await dbContext.WorkflowVariables.AsNoTracking()
            .Where(x => x.WorkflowInstanceId == id)
            .OrderBy(x => x.VariableName)
            .Select(x => new WorkflowVariableResponse(x.Id, x.WorkflowInstanceId, x.VariableName, x.ValueJson, x.ValueType, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        var auditLogs = await GetAuditLogsAsync(id, cancellationToken);
        var taskIds = tasks.Select(x => x.Id).ToList();
        var comments = await GetTaskCommentsAsync(taskIds, cancellationToken);
        var assignments = await GetTaskAssignmentsAsync(taskIds, cancellationToken);

        return new WorkflowInstanceDetailResponse(
            await BuildInstanceResponseAsync(instance, cancellationToken),
            ToVersionDetailResponse(version),
            tasks,
            variables,
            auditLogs,
            comments,
            assignments);
    }

    public async Task<IReadOnlyList<WorkflowAuditLogResponse>> GetAuditLogsAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkflowAuditLogs
            .AsNoTracking()
            .Where(x => x.WorkflowInstanceId == instanceId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new WorkflowAuditLogResponse(
                x.Id,
                x.WorkflowInstanceId,
                x.WorkflowTaskId,
                x.Action,
                x.FromNodeId,
                x.ToNodeId,
                x.ActorUserId,
                x.ActorId,
                x.ActorId,
                x.Summary,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowTaskResponse>> GetMyTasksAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roleIds = await GetUserRoleIdsAsync(userId, cancellationToken);
        var groupIds = await GetUserGroupIdsAsync(userId, cancellationToken);

        var tasks = await dbContext.WorkflowTasks
            .AsNoTracking()
            .Where(x =>
                (x.Status == "Claimed" && x.ClaimedByUserId == userId)
                || (x.Status == "Pending"
                    && (x.AssignedToUserId == userId
                        || (x.AssignedToGroupId != null && groupIds.Contains(x.AssignedToGroupId.Value))
                        || (x.AssignedToRoleId != null && roleIds.Contains(x.AssignedToRoleId.Value)))))
            .OrderBy(x => x.DueAtUtc)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return tasks.Select(x => ToTaskResponse(x, userId, isWorkflowAdministrator: false)).ToList();
    }

    public async Task<WorkflowTaskResponse?> GetTaskAsync(Guid taskId, Guid? actorUserId, bool isWorkflowAdministrator, CancellationToken cancellationToken)
    {
        var task = await dbContext.WorkflowTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        return task is null ? null : ToTaskResponse(task, actorUserId, isWorkflowAdministrator);
    }

    public async Task<WorkflowTaskDetailResponse?> GetTaskDetailAsync(Guid taskId, Guid? actorUserId, bool isWorkflowAdministrator, CancellationToken cancellationToken)
    {
        var task = await dbContext.WorkflowTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var comments = await GetTaskCommentsAsync([taskId], cancellationToken);
        var assignments = await GetTaskAssignmentsAsync([taskId], cancellationToken);
        return new WorkflowTaskDetailResponse(ToTaskResponse(task, actorUserId, isWorkflowAdministrator), comments, assignments);
    }

    public async Task<WorkflowTaskResponse> ClaimTaskAsync(Guid taskId, ClaimTaskRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var task = await dbContext.WorkflowTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new WorkflowRuntimeNotFoundException("Workflow task was not found.");

        if (task.Status != "Pending")
        {
            throw new WorkflowRuntimeException("Only pending tasks can be claimed.");
        }

        if (!await CanUserAccessTaskAsync(task, request.ClaimedByUserId, cancellationToken))
        {
            throw new WorkflowRuntimeForbiddenException("User is not allowed to claim this task.");
        }

        task.Status = "Claimed";
        task.ClaimedByUserId = request.ClaimedByUserId;
        task.ClaimedBy = request.ClaimedBy.Trim();
        task.ClaimedAtUtc = now;
        task.AvailableActionsJson = SerializeTaskActions(BuildAvailableActions(task, request.ClaimedByUserId, false));
        AddTaskAssignment(task.Id, "Claimed", task.AssignedToUserId, task.AssignedToGroupId, task.AssignedToRoleId, request.ClaimedByUserId, null, null, null, request.ClaimedBy, request.ClaimedByUserId, now);
        AddAudit(task.WorkflowInstanceId, task.Id, "TaskClaimed", task.NodeId, task.NodeId, request.ClaimedByUserId, request.ClaimedBy, request.ClaimedByDisplayName, "Task claimed.", null, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTaskResponse(task, request.ClaimedByUserId, false);
    }

    public async Task<WorkflowTaskResponse> UnclaimTaskAsync(Guid taskId, UnclaimTaskRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var task = await dbContext.WorkflowTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new WorkflowRuntimeNotFoundException("Workflow task was not found.");

        if (task.Status != "Claimed")
        {
            throw new WorkflowRuntimeException("Only claimed tasks can be unclaimed.");
        }

        if (task.ClaimedByUserId != request.UserId)
        {
            throw new WorkflowRuntimeForbiddenException("Only the claimant can unclaim this task.");
        }

        task.Status = "Pending";
        task.ClaimedByUserId = null;
        task.ClaimedBy = null;
        task.ClaimedAtUtc = null;
        task.AvailableActionsJson = SerializeTaskActions(BuildAvailableActions(task, request.UserId, false));
        AddTaskAssignment(task.Id, "Unclaimed", request.UserId, null, null, task.AssignedToUserId, task.AssignedToGroupId, task.AssignedToRoleId, null, request.UserName, request.UserId, now);
        AddAudit(task.WorkflowInstanceId, task.Id, "TaskUnclaimed", task.NodeId, task.NodeId, request.UserId, request.UserName, request.DisplayName, "Task unclaimed.", null, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTaskResponse(task, request.UserId, false);
    }

    public async Task<WorkflowTaskResponse> DelegateTaskAsync(Guid taskId, DelegateTaskRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var task = await dbContext.WorkflowTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new WorkflowRuntimeNotFoundException("Workflow task was not found.");

        if (task.Status != "Pending" && task.Status != "Claimed")
        {
            throw new WorkflowRuntimeException("Only pending or claimed tasks can be delegated.");
        }

        if (!await CanUserCompleteTaskAsync(task, request.RequestedByUserId, cancellationToken))
        {
            throw new WorkflowRuntimeForbiddenException("User is not allowed to delegate this task.");
        }

        var targetDisplayName = await dbContext.Users
            .Where(x => x.Id == request.TargetUserId)
            .Select(x => x.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetDisplayName is null)
        {
            throw new WorkflowRuntimeNotFoundException("Target user was not found.");
        }

        var fromUserId = task.AssignedToUserId;
        var fromGroupId = task.AssignedToGroupId;
        var fromRoleId = task.AssignedToRoleId;

        task.AssignedToUserId = request.TargetUserId;
        task.AssignedToGroupId = null;
        task.AssignedToRoleId = null;
        task.AssignmentType = "User";
        task.AssignedToDisplayName = targetDisplayName;
        task.Status = "Pending";
        task.ClaimedByUserId = null;
        task.ClaimedBy = null;
        task.ClaimedAtUtc = null;
        task.AvailableActionsJson = SerializeTaskActions(BuildAvailableActions(task, request.TargetUserId, false));

        AddTaskAssignment(task.Id, "Delegated", fromUserId, fromGroupId, fromRoleId, request.TargetUserId, null, null, request.Reason, request.RequestedBy, request.RequestedByUserId, now);
        AddTaskComment(task.Id, "AssignmentReason", request.Reason, "Internal", request.RequestedBy, request.RequestedByUserId, now);
        AddAudit(task.WorkflowInstanceId, task.Id, "TaskDelegated", task.NodeId, task.NodeId, request.RequestedByUserId, request.RequestedBy, request.RequestedByDisplayName, request.Reason ?? "Task delegated.", null, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTaskResponse(task, request.RequestedByUserId, false);
    }

    public async Task<WorkflowTaskResponse> ReassignTaskAsync(Guid taskId, ReassignTaskRequest request, bool isWorkflowAdministrator, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var task = await dbContext.WorkflowTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new WorkflowRuntimeNotFoundException("Workflow task was not found.");

        if (task.Status != "Pending" && task.Status != "Claimed")
        {
            throw new WorkflowRuntimeException("Only pending or claimed tasks can be reassigned.");
        }

        if (!isWorkflowAdministrator)
        {
            if (!request.RequestedByUserId.HasValue || !await CanUserCompleteTaskAsync(task, request.RequestedByUserId.Value, cancellationToken))
            {
                throw new WorkflowRuntimeForbiddenException("User is not allowed to reassign this task.");
            }
        }

        var targetCount = new[] { request.TargetUserId, request.TargetGroupId, request.TargetRoleId }.Count(x => x.HasValue);
        if (targetCount != 1)
        {
            throw new WorkflowRuntimeException("Reassignment requires exactly one target: user, group, or role.");
        }

        var fromUserId = task.AssignedToUserId;
        var fromGroupId = task.AssignedToGroupId;
        var fromRoleId = task.AssignedToRoleId;

        task.AssignedToUserId = request.TargetUserId;
        task.AssignedToGroupId = request.TargetGroupId;
        task.AssignedToRoleId = request.TargetRoleId;
        task.AssignmentType = request.TargetRoleId.HasValue ? "Role" : request.TargetGroupId.HasValue ? "Group" : "User";
        task.AssignedToDisplayName = await ResolveAssignmentDisplayNameAsync(request.TargetUserId, request.TargetGroupId, request.TargetRoleId, cancellationToken);
        task.Status = "Pending";
        task.ClaimedByUserId = null;
        task.ClaimedBy = null;
        task.ClaimedAtUtc = null;
        task.AvailableActionsJson = SerializeTaskActions(BuildAvailableActions(task, request.TargetUserId, isWorkflowAdministrator));

        AddTaskAssignment(task.Id, "Reassigned", fromUserId, fromGroupId, fromRoleId, request.TargetUserId, request.TargetGroupId, request.TargetRoleId, request.Reason, request.RequestedBy, request.RequestedByUserId, now);
        AddTaskComment(task.Id, "AssignmentReason", request.Reason, "Internal", request.RequestedBy, request.RequestedByUserId, now);
        AddAudit(task.WorkflowInstanceId, task.Id, "TaskReassigned", task.NodeId, task.NodeId, request.RequestedByUserId, request.RequestedBy, request.RequestedByDisplayName, request.Reason ?? "Task reassigned.", null, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTaskResponse(task, request.RequestedByUserId, isWorkflowAdministrator);
    }

    public async Task<WorkflowTaskCommentResponse> AddTaskCommentAsync(Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new WorkflowRuntimeException("Task comment is required.");
        }

        var task = await dbContext.WorkflowTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new WorkflowRuntimeNotFoundException("Workflow task was not found.");

        if (request.CreatedByUserId.HasValue && !await CanUserAccessTaskAsync(task, request.CreatedByUserId.Value, cancellationToken))
        {
            throw new WorkflowRuntimeForbiddenException("User is not allowed to comment on this task.");
        }

        var comment = AddTaskComment(taskId, "Comment", request.Comment, NormalizeVisibility(request.Visibility), request.CreatedBy, request.CreatedByUserId, now)
            ?? throw new WorkflowRuntimeException("Task comment is required.");
        AddAudit(task.WorkflowInstanceId, task.Id, "TaskCommentAdded", task.NodeId, task.NodeId, request.CreatedByUserId, request.CreatedBy, request.CreatedByDisplayName, request.Comment, null, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTaskCommentResponse(comment);
    }

    public async Task<WorkflowTaskResponse> ApproveTaskAsync(Guid taskId, CompleteTaskRequest request, CancellationToken cancellationToken) =>
        await CompleteTaskAsync(taskId, request, "Completed", "Approved", "TaskApproved", cancellationToken);

    public async Task<WorkflowTaskResponse> RejectTaskAsync(Guid taskId, CompleteTaskRequest request, CancellationToken cancellationToken) =>
        await CompleteTaskAsync(taskId, request, "Completed", "Rejected", "TaskRejected", cancellationToken);

    public async Task<WorkflowInstanceResponse> CancelInstanceAsync(Guid id, CancelWorkflowInstanceRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var instance = await dbContext.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new WorkflowRuntimeNotFoundException("Workflow instance was not found.");

        if (instance.Status != "Running" && instance.Status != "Waiting")
        {
            throw new WorkflowRuntimeException("Only running workflow instances can be cancelled.");
        }

        instance.Status = "Cancelled";
        instance.CancelledAtUtc = now;
        instance.LastHeartbeatUtc = now;

        var openTasks = await dbContext.WorkflowTasks
            .Where(x => x.WorkflowInstanceId == id && (x.Status == "Pending" || x.Status == "Claimed"))
            .ToListAsync(cancellationToken);

        foreach (var task in openTasks)
        {
            task.Status = "Cancelled";
            task.CompletedBy = request.CancelledBy;
            task.CompletedByUserId = request.CancelledByUserId;
            task.CompletedAtUtc = now;
            task.Outcome = "Cancelled";
            task.Comment = request.Reason;
        }

        await MarkOpenNodesCancelledAsync(id, now, cancellationToken);
        AddAudit(instance.Id, null, "WorkflowCancelled", await GetCurrentNodeIdAsync(instance.Id, cancellationToken), null, request.CancelledByUserId, request.CancelledBy, request.CancelledByDisplayName, request.Reason ?? "Workflow cancelled.", null, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildInstanceResponseAsync(instance, cancellationToken);
    }

    public async Task<bool> CanUserAccessInstanceAsync(Guid instanceId, Guid userId, CancellationToken cancellationToken)
    {
        var instance = await dbContext.WorkflowInstances.AsNoTracking().FirstOrDefaultAsync(x => x.Id == instanceId, cancellationToken);
        if (instance is null)
        {
            return false;
        }

        if (instance.StartedByUserId == userId)
        {
            return true;
        }

        var roleIds = await GetUserRoleIdsAsync(userId, cancellationToken);
        var groupIds = await GetUserGroupIdsAsync(userId, cancellationToken);
        return await dbContext.WorkflowTasks.AsNoTracking().AnyAsync(x =>
            x.WorkflowInstanceId == instanceId &&
            (x.AssignedToUserId == userId
             || x.ClaimedByUserId == userId
             || (x.AssignedToGroupId.HasValue && groupIds.Contains(x.AssignedToGroupId.Value))
             || (x.AssignedToRoleId.HasValue && roleIds.Contains(x.AssignedToRoleId.Value))
             || x.CompletedByUserId == userId), cancellationToken);
    }

    public async Task<bool> CanUserCancelInstanceAsync(Guid instanceId, Guid userId, CancellationToken cancellationToken) =>
        await dbContext.WorkflowInstances.AsNoTracking().AnyAsync(x => x.Id == instanceId && x.StartedByUserId == userId, cancellationToken);

    public async Task<bool> CanUserAccessTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken)
    {
        var task = await dbContext.WorkflowTasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        return task is not null && await CanUserAccessTaskAsync(task, userId, cancellationToken);
    }

    private async Task<WorkflowTaskResponse> CompleteTaskAsync(
        Guid taskId,
        CompleteTaskRequest request,
        string finalStatus,
        string outcome,
        string auditAction,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var task = await dbContext.WorkflowTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new WorkflowRuntimeNotFoundException("Workflow task was not found.");

        if (task.Status != "Pending" && task.Status != "Claimed")
        {
            throw new WorkflowRuntimeException("Only pending or claimed tasks can be completed.");
        }

        if (!request.CompletedByUserId.HasValue || !await CanUserCompleteTaskAsync(task, request.CompletedByUserId.Value, cancellationToken))
        {
            throw new WorkflowRuntimeForbiddenException("User is not allowed to complete this task.");
        }

        var instance = await dbContext.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == task.WorkflowInstanceId, cancellationToken)
            ?? throw new WorkflowRuntimeNotFoundException("Workflow instance was not found.");
        var version = await dbContext.WorkflowVersions.AsNoTracking().FirstAsync(x => x.Id == instance.WorkflowVersionId, cancellationToken);
        var document = WorkflowDefinitionDocument.Parse(version.DefinitionJson);

        task.Status = finalStatus;
        task.CompletedBy = request.CompletedBy;
        task.CompletedByUserId = request.CompletedByUserId;
        task.CompletedAtUtc = now;
        task.Outcome = outcome;
        task.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        task.SlaStatus = ResolveSlaStatus(now, task.DueAtUtc, task.EscalatedAtUtc);
        task.AvailableActionsJson = SerializeTaskActions(EmptyTaskActions);

        await MarkNodeCompletedAsync(task.WorkflowInstanceNodeId, now, cancellationToken);
        AddTaskComment(task.Id, "Decision", request.Comment, "Internal", request.CompletedBy, request.CompletedByUserId, now);
        AddAudit(instance.Id, task.Id, auditAction, task.NodeId, task.NodeId, request.CompletedByUserId, request.CompletedBy, request.CompletedByDisplayName, request.Comment, null, now);

        if (outcome == "Rejected")
        {
            instance.Status = "Cancelled";
            instance.CancelledAtUtc = now;
            instance.LastHeartbeatUtc = now;
            AddAudit(instance.Id, null, "WorkflowCancelled", task.NodeId, null, request.CompletedByUserId, request.CompletedBy, request.CompletedByDisplayName, "Workflow rejected.", null, now);
        }
        else
        {
            await AdvanceAsync(instance, document, task.NodeId, request.CompletedByUserId, request.CompletedBy, request.CompletedByDisplayName, now, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTaskResponse(task, request.CompletedByUserId, false);
    }

    private async Task AdvanceAsync(
        WorkflowInstance instance,
        WorkflowDefinitionDocument document,
        string fromNodeId,
        Guid? performedByUserId,
        string performedBy,
        string? performedByDisplayName,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var nextNode = document.GetSingleNextNode(fromNodeId);
        while (true)
        {
            if (nextNode is null)
            {
                throw new WorkflowDefinitionException($"Node '{fromNodeId}' has no outgoing edge.");
            }

            if (nextNode.Type is "approval" or "usertask")
            {
                await CreateApprovalTaskAsync(instance, nextNode, fromNodeId, performedByUserId, performedBy, performedByDisplayName, now, cancellationToken);
                return;
            }

            if (nextNode.Type == "condition")
            {
                await TrackNodeAsync(instance.Id, nextNode, "Completed", now, cancellationToken);
                var conditionResult = await EvaluateConditionAsync(instance.Id, nextNode, cancellationToken);
                AddAudit(instance.Id, null, "ConditionEvaluated", fromNodeId, nextNode.Id, performedByUserId, performedBy, performedByDisplayName, $"Condition '{nextNode.Expression}' evaluated to {conditionResult.ToString().ToLowerInvariant()}.", null, now);
                fromNodeId = nextNode.Id;
                nextNode = document.GetConditionNextNode(nextNode.Id, conditionResult);
                continue;
            }

            if (nextNode.Type is "timer" or "delay")
            {
                await CreateTimerAsync(instance, nextNode, fromNodeId, performedByUserId, performedBy, performedByDisplayName, now, cancellationToken);
                return;
            }

            if (nextNode.Type == "notification")
            {
                await TrackNodeAsync(instance.Id, nextNode, "Completed", now, cancellationToken);
                EnqueueOutboxMessage(
                    aggregateType: "WorkflowInstance",
                    aggregateId: instance.Id.ToString(),
                    eventType: "Workflow.NotificationRequested",
                    payloadJson: JsonSerializer.Serialize(new
                    {
                        workflowInstanceId = instance.Id,
                        workflowDefinitionId = instance.WorkflowDefinitionId,
                        nodeId = nextNode.Id,
                        nodeName = nextNode.Name,
                        notificationKey = nextNode.NotificationKey,
                        title = instance.Title,
                        businessKey = instance.BusinessKey
                    }),
                    occurredAtUtc: now);

                AddAudit(instance.Id, null, "NotificationQueued", fromNodeId, nextNode.Id, performedByUserId, performedBy, performedByDisplayName, "Notification queued for background delivery.", null, now);
                fromNodeId = nextNode.Id;
                nextNode = document.GetSingleNextNode(nextNode.Id);
                continue;
            }

            if (nextNode.Type is "servicetask")
            {
                await ExecuteServiceTaskAsync(instance, nextNode, fromNodeId, performedByUserId, performedBy, performedByDisplayName, now, cancellationToken);
                fromNodeId = nextNode.Id;
                nextNode = document.GetSingleNextNode(nextNode.Id);
                continue;
            }

            if (nextNode.Type == "end")
            {
                await TrackNodeAsync(instance.Id, nextNode, "Completed", now, cancellationToken);
                instance.Status = "Completed";
                instance.CompletedAtUtc = now;
                instance.CurrentNodeCount = 0;
                instance.LastHeartbeatUtc = now;
                AddAudit(instance.Id, null, "WorkflowCompleted", fromNodeId, nextNode.Id, performedByUserId, performedBy, performedByDisplayName, "Workflow completed.", null, now);
                return;
            }

            throw new WorkflowDefinitionException($"Node type '{nextNode.Type}' is not supported in runtime.");
        }
    }

    private async Task<bool> EvaluateConditionAsync(Guid workflowInstanceId, WorkflowNode node, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(node.Expression))
        {
            throw new WorkflowDefinitionException($"Condition node '{node.Id}' requires expression.");
        }

        var variables = await dbContext.WorkflowVariables.AsNoTracking()
            .Where(x => x.WorkflowInstanceId == workflowInstanceId)
            .ToDictionaryAsync(x => x.VariableName, x => ConditionExpressionEvaluator.ParseStoredValue(x.ValueJson, x.ValueType), StringComparer.OrdinalIgnoreCase, cancellationToken);

        return ConditionExpressionEvaluator.Evaluate(node.Expression, variables);
    }

    private async Task CreateApprovalTaskAsync(
        WorkflowInstance instance,
        WorkflowNode node,
        string fromNodeId,
        Guid? performedByUserId,
        string performedBy,
        string? performedByDisplayName,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (node.AssignedToUserId is null && node.AssignedToGroupId is null && node.AssignedToRoleId is null)
        {
            throw new WorkflowDefinitionException($"Approval node '{node.Id}' requires assignedToUserId, assignedToGroupId, or assignedToRoleId.");
        }

        string? assignedDisplayName = null;
        if (node.AssignedToUserId.HasValue)
        {
            assignedDisplayName = await dbContext.Users.Where(x => x.Id == node.AssignedToUserId.Value).Select(x => x.FullName).FirstOrDefaultAsync(cancellationToken);
        }

        if (assignedDisplayName is null && node.AssignedToRoleId.HasValue)
        {
            assignedDisplayName = await dbContext.PlatformAccessRoles.Where(x => x.Id == node.AssignedToRoleId.Value).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
        }

        if (assignedDisplayName is null && node.AssignedToGroupId.HasValue)
        {
            assignedDisplayName = await dbContext.PlatformAccessGroups.Where(x => x.Id == node.AssignedToGroupId.Value).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
        }

        var nodeExecution = new WorkflowInstanceNode
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            NodeId = node.Id,
            NodeType = node.Type,
            NodeName = node.Name,
            ExecutionStatus = "Waiting",
            ActivatedAtUtc = now
        };

        var task = new WorkflowTask
        {
            Id = Guid.NewGuid(),
            TenantId = instance.TenantId,
            WorkflowInstanceId = instance.Id,
            WorkflowInstanceNodeId = nodeExecution.Id,
            NodeId = node.Id,
            Title = string.IsNullOrWhiteSpace(node.Name) ? "Approval" : node.Name,
            Status = "Pending",
            TaskMode = ResolveTaskMode(node),
            Priority = ReadMetadataString(node.MetadataJson, "priority"),
            FormKey = ReadMetadataString(node.MetadataJson, "formKey"),
            ListViewKey = ReadMetadataString(node.MetadataJson, "listViewKey"),
            DetailViewKey = ReadMetadataString(node.MetadataJson, "detailViewKey"),
            AssignedToUserId = node.AssignedToUserId,
            AssignedToGroupId = node.AssignedToGroupId,
            AssignedToRoleId = node.AssignedToRoleId,
            AssignmentType = string.IsNullOrWhiteSpace(node.AssignmentType)
                ? node.AssignedToRoleId.HasValue ? "Role" : node.AssignedToGroupId.HasValue ? "Group" : "User"
                : node.AssignmentType,
            AssignedToDisplayName = assignedDisplayName,
            DueAtUtc = node.DueInHours.HasValue ? now.AddHours(node.DueInHours.Value) : null,
            ReminderAtUtc = node.DueInHours.HasValue && node.DueInHours.Value > 2 ? now.AddHours(Math.Max(1, node.DueInHours.Value - 2)) : null,
            EscalationAtUtc = node.DueInHours.HasValue ? now.AddHours(node.DueInHours.Value + 4) : null,
            EscalationPolicyKey = ReadMetadataString(node.MetadataJson, "escalationPolicyKey"),
            SlaStatus = ResolveSlaStatus(now, node.DueInHours.HasValue ? now.AddHours(node.DueInHours.Value) : null, null),
            ClaimRequired = !string.Equals(node.AssignmentType, "User", StringComparison.OrdinalIgnoreCase) && !node.AssignedToUserId.HasValue,
            DisplayMetadataJson = BuildTaskDisplayMetadata(instance, node, assignedDisplayName),
            CreatedAtUtc = now
        };
        task.AvailableActionsJson = SerializeTaskActions(BuildAvailableActions(task, null, false));

        instance.Status = "Waiting";
        instance.CurrentNodeCount = 1;
        instance.LastHeartbeatUtc = now;

        dbContext.WorkflowInstanceNodes.Add(nodeExecution);
        dbContext.WorkflowTasks.Add(task);
        AddTaskAssignment(task.Id, "Assigned", null, null, null, task.AssignedToUserId, task.AssignedToGroupId, task.AssignedToRoleId, null, performedBy, performedByUserId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        AddAudit(instance.Id, task.Id, "TaskCreated", fromNodeId, node.Id, performedByUserId, performedBy, performedByDisplayName, "Approval task created.", null, now);
    }

    private async Task CreateTimerAsync(
        WorkflowInstance instance,
        WorkflowNode node,
        string fromNodeId,
        Guid? performedByUserId,
        string performedBy,
        string? performedByDisplayName,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var dueAtUtc = ResolveTimerDueAtUtc(node, now);
        var nodeExecution = new WorkflowInstanceNode
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            NodeId = node.Id,
            NodeType = node.Type,
            NodeName = node.Name,
            ExecutionStatus = "Waiting",
            ActivatedAtUtc = now
        };

        dbContext.WorkflowInstanceNodes.Add(nodeExecution);
        dbContext.WorkflowTimers.Add(new WorkflowTimer
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            WorkflowInstanceNodeId = nodeExecution.Id,
            TimerType = string.IsNullOrWhiteSpace(node.TimerType) ? node.Type : node.TimerType,
            Status = "Pending",
            DueAtUtc = dueAtUtc,
            PayloadJson = JsonSerializer.Serialize(new
            {
                workflowInstanceId = instance.Id,
                nodeId = node.Id,
                nodeName = node.Name,
                dueInHours = node.DueInHours,
                timerType = node.TimerType,
                timerExpression = node.TimerExpression
            }),
            CreatedAtUtc = now
        });

        instance.Status = "Waiting";
        instance.CurrentNodeCount = 1;
        instance.LastHeartbeatUtc = now;

        dbContext.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
        {
            WorkflowInstanceId = instance.Id,
            WorkflowInstanceNodeId = nodeExecution.Id,
            LogLevel = "Info",
            EventType = "TimerScheduled",
            Message = $"Workflow {node.Type} node scheduled for {dueAtUtc:u}.",
            CreatedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        AddAudit(instance.Id, null, "TimerScheduled", fromNodeId, node.Id, performedByUserId, performedBy, performedByDisplayName, $"Timer scheduled for {dueAtUtc:u}.", null, now);
    }

    private async Task ExecuteServiceTaskAsync(
        WorkflowInstance instance,
        WorkflowNode node,
        string fromNodeId,
        Guid? performedByUserId,
        string performedBy,
        string? performedByDisplayName,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await TrackNodeAsync(instance.Id, node, "Completed", now, cancellationToken);

        var processMode = ResolveProcessMode(node);
        var payloadJson = JsonSerializer.Serialize(new
        {
            workflowInstanceId = instance.Id,
            workflowDefinitionId = instance.WorkflowDefinitionId,
            nodeId = node.Id,
            nodeName = node.Name,
            processMode,
            serviceKey = node.ServiceKey,
            externalApiEndpointId = node.ExternalApiEndpointId,
            inputMapping = node.InputMapping,
            outputMapping = node.OutputMapping,
            retryPolicy = node.RetryPolicy
        });

        switch (processMode)
        {
            case "dataUpdate":
                var updatedValue = await UpsertWorkflowVariableAsync(instance.Id, node.TargetVariable ?? $"service.{node.Id}", ResolveServiceTaskValue(node), now, cancellationToken);
                AddAudit(instance.Id, null, "ServiceTaskDataUpdated", fromNodeId, node.Id, performedByUserId, performedBy, performedByDisplayName, $"Process task updated variable '{updatedValue.VariableName}'.", updatedValue.ValueJson, now);
                break;

            case "externalApi":
                EnqueueOutboxMessage(
                    aggregateType: "WorkflowInstance",
                    aggregateId: instance.Id.ToString(),
                    eventType: "Workflow.ProcessTaskRequested",
                    payloadJson: payloadJson,
                    occurredAtUtc: now);
                AddAudit(instance.Id, null, "ServiceTaskQueued", fromNodeId, node.Id, performedByUserId, performedBy, performedByDisplayName, "Process task queued for external API execution.", payloadJson, now);
                break;

            default:
                EnqueueOutboxMessage(
                    aggregateType: "WorkflowInstance",
                    aggregateId: instance.Id.ToString(),
                    eventType: "Workflow.ProcessTaskRequested",
                    payloadJson: payloadJson,
                    occurredAtUtc: now);
                AddAudit(instance.Id, null, "ServiceTaskQueued", fromNodeId, node.Id, performedByUserId, performedBy, performedByDisplayName, "Process task queued for background execution.", payloadJson, now);
                break;
        }
    }

    private static DateTime ResolveTimerDueAtUtc(WorkflowNode node, DateTime now)
    {
        if (node.DueInHours is > 0)
        {
            return now.AddHours(node.DueInHours.Value);
        }

        if (!string.IsNullOrWhiteSpace(node.TimerExpression))
        {
            if (DateTime.TryParse(node.TimerExpression, out var absoluteDate))
            {
                return absoluteDate.Kind == DateTimeKind.Utc
                    ? absoluteDate
                    : absoluteDate.ToUniversalTime();
            }

            if (TimeSpan.TryParse(node.TimerExpression, out var timeSpan))
            {
                return now.Add(timeSpan);
            }

            try
            {
                return now.Add(XmlConvert.ToTimeSpan(node.TimerExpression));
            }
            catch (FormatException)
            {
            }
        }

        return now.AddHours(1);
    }

    private async Task<WorkflowVariable> UpsertWorkflowVariableAsync(Guid workflowInstanceId, string variableName, object? value, DateTime now, CancellationToken cancellationToken)
    {
        var normalizedName = variableName.Trim();
        var serializedValue = JsonSerializer.Serialize(value);
        var dataType = ResolveVariableDataType(value);

        var existing = await dbContext.WorkflowVariables
            .FirstOrDefaultAsync(x => x.WorkflowInstanceId == workflowInstanceId && x.VariableName == normalizedName, cancellationToken);

        if (existing is null)
        {
            existing = new WorkflowVariable
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                VariableName = normalizedName,
                ValueJson = serializedValue,
                ValueType = dataType,
                CreatedAtUtc = now
            };

            dbContext.WorkflowVariables.Add(existing);
        }
        else
        {
            existing.ValueJson = serializedValue;
            existing.ValueType = dataType;
            existing.UpdatedAtUtc = now;
        }

        return existing;
    }

    private async Task TrackNodeAsync(Guid workflowInstanceId, WorkflowNode node, string status, DateTime now, CancellationToken cancellationToken)
    {
        dbContext.WorkflowInstanceNodes.Add(new WorkflowInstanceNode
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            NodeId = node.Id,
            NodeType = node.Type,
            NodeName = node.Name,
            ExecutionStatus = status,
            ActivatedAtUtc = now,
            CompletedAtUtc = status == "Completed" ? now : null
        });

        var instance = await dbContext.WorkflowInstances.FirstAsync(x => x.Id == workflowInstanceId, cancellationToken);
        instance.LastHeartbeatUtc = now;
        instance.CurrentNodeCount = status == "Completed" ? 0 : 1;
    }

    private async Task MarkNodeCompletedAsync(Guid workflowInstanceNodeId, DateTime now, CancellationToken cancellationToken)
    {
        var node = await dbContext.WorkflowInstanceNodes.FirstOrDefaultAsync(x => x.Id == workflowInstanceNodeId, cancellationToken);
        if (node is null)
        {
            return;
        }

        node.ExecutionStatus = "Completed";
        node.CompletedAtUtc = now;
    }

    private async Task MarkOpenNodesCancelledAsync(Guid workflowInstanceId, DateTime now, CancellationToken cancellationToken)
    {
        var nodes = await dbContext.WorkflowInstanceNodes
            .Where(x => x.WorkflowInstanceId == workflowInstanceId && (x.ExecutionStatus == "Active" || x.ExecutionStatus == "Waiting" || x.ExecutionStatus == "Pending"))
            .ToListAsync(cancellationToken);

        foreach (var node in nodes)
        {
            node.ExecutionStatus = "Cancelled";
            node.CompletedAtUtc = now;
        }
    }

    private void AddAudit(Guid workflowInstanceId, Guid? workflowTaskId, string action, string? fromNodeId, string? toNodeId, Guid? actorUserId, string? actorId, string? actorDisplayName, string? message, string? dataJson, DateTime createdAtUtc)
    {
        dbContext.WorkflowAuditLogs.Add(new WorkflowAuditLog
        {
            WorkflowInstanceId = workflowInstanceId,
            WorkflowTaskId = workflowTaskId,
            ActorType = actorUserId.HasValue ? "User" : "System",
            ActorId = actorId,
            ActorUserId = actorUserId,
            Action = action,
            Summary = message ?? actorDisplayName,
            FromNodeId = fromNodeId,
            ToNodeId = toNodeId,
            DataJson = dataJson,
            CreatedAtUtc = createdAtUtc
        });
    }

    private void EnqueueOutboxMessage(string aggregateType, string aggregateId, string eventType, string payloadJson, DateTime occurredAtUtc)
    {
        dbContext.WorkflowOutbox.Add(new WorkflowOutbox
        {
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            EventType = eventType,
            PayloadJson = payloadJson,
            OccurredAtUtc = occurredAtUtc,
            Status = "Pending",
            RetryCount = 0,
            NextAttemptAtUtc = occurredAtUtc
        });
    }

    private Guid? GetTenantId(Guid? userId)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        return dbContext.Users.Where(x => x.Id == userId.Value).Select(x => (Guid?)x.TenantId).FirstOrDefault();
    }

    private static string ResolveVariableDataType(object? value) => value switch
    {
        null => "Null",
        bool => "Boolean",
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => "Number",
        DateTime => "DateTime",
        JsonElement { ValueKind: JsonValueKind.Number } => "Number",
        JsonElement { ValueKind: JsonValueKind.True or JsonValueKind.False } => "Boolean",
        JsonElement { ValueKind: JsonValueKind.Object } => "Object",
        JsonElement { ValueKind: JsonValueKind.Array } => "Array",
        _ => "String"
    };

    private async Task<WorkflowInstanceResponse> BuildInstanceResponseAsync(WorkflowInstance instance, CancellationToken cancellationToken) =>
        new(instance.Id, instance.WorkflowDefinitionId, instance.WorkflowVersionId, instance.BusinessKey, instance.Title, instance.Status, await GetCurrentNodeIdAsync(instance.Id, cancellationToken), instance.StartedByUserId, instance.StartedBy, instance.StartedByDisplayName, instance.StartedAtUtc, instance.CompletedAtUtc, instance.CancelledAtUtc, instance.FailedAtUtc);

    private async Task<string?> GetCurrentNodeIdAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkflowInstanceNodes.AsNoTracking()
            .Where(x => x.WorkflowInstanceId == instanceId)
            .OrderByDescending(x => x.ActivatedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(x => x.SequenceNo)
            .Select(x => x.NodeId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static WorkflowVersionDetailResponse ToVersionDetailResponse(WorkflowVersion version) =>
        new(version.Id, version.WorkflowDefinitionId, version.VersionNumber, version.Status, version.EffectiveFromUtc, version.EffectiveToUtc, version.PublishedBy, version.PublishedAtUtc, version.PublishMessage, version.DefinitionJson);

    private static WorkflowTaskResponse ToTaskResponse(WorkflowTask task, Guid? actorUserId, bool isWorkflowAdministrator)
    {
        var availableActions = DeserializeTaskActions(task.AvailableActionsJson);
        if (availableActions.Count == 0)
        {
            availableActions = BuildAvailableActions(task, actorUserId, isWorkflowAdministrator);
        }

        return new(
            task.Id,
            task.WorkflowInstanceId,
            task.NodeId,
            task.Title,
            task.Status,
            task.TaskMode,
            task.Priority,
            task.EntityType,
            task.EntityId,
            task.AssignedToUserId,
            task.AssignedToGroupId,
            task.AssignedToRoleId,
            task.AssignmentType,
            task.AssignedToDisplayName,
            task.ClaimRequired,
            task.QueueKey,
            task.FormKey,
            task.ListViewKey,
            task.DetailViewKey,
            task.DisplayMetadataJson,
            task.DueAtUtc,
            task.ReminderAtUtc,
            task.EscalationAtUtc,
            task.EscalationPolicyKey,
            task.SlaStatus,
            task.EscalatedAtUtc,
            task.CreatedAtUtc,
            task.ClaimedBy,
            task.ClaimedByUserId,
            task.ClaimedAtUtc,
            task.CompletedBy,
            task.CompletedByUserId,
            task.CompletedAtUtc,
            task.Outcome,
            availableActions,
            task.Comment);
    }

    private static string ResolveTaskMode(WorkflowNode node)
    {
        if (string.Equals(node.Type, "approval", StringComparison.OrdinalIgnoreCase))
        {
            return "approval";
        }

        return ReadMetadataString(node.MetadataJson, "taskMode") ?? "manualAction";
    }

    private static string ResolveProcessMode(WorkflowNode node)
    {
        if (!string.IsNullOrWhiteSpace(node.ProcessMode))
        {
            return node.ProcessMode;
        }

        if (!string.IsNullOrWhiteSpace(node.ExternalApiEndpointId))
        {
            return "externalApi";
        }

        if (!string.IsNullOrWhiteSpace(node.TargetVariable) || !string.IsNullOrWhiteSpace(node.ValueExpression))
        {
            return "dataUpdate";
        }

        return "service";
    }

    private static object? ResolveServiceTaskValue(WorkflowNode node)
    {
        if (string.IsNullOrWhiteSpace(node.ValueExpression))
        {
            return null;
        }

        var text = node.ValueExpression.Trim();

        if (bool.TryParse(text, out var booleanValue))
        {
            return booleanValue;
        }

        if (decimal.TryParse(text, out var decimalValue))
        {
            return decimalValue;
        }

        if ((text.StartsWith("{") && text.EndsWith("}")) || (text.StartsWith("[") && text.EndsWith("]")))
        {
            try
            {
                return JsonSerializer.Deserialize<JsonElement>(text);
            }
            catch (JsonException)
            {
            }
        }

        return text;
    }

    private static string? ReadMetadataString(string? metadataJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string BuildTaskDisplayMetadata(WorkflowInstance instance, WorkflowNode node, string? assignedDisplayName)
    {
        return JsonSerializer.Serialize(new
        {
            workflowTitle = instance.Title,
            workflowBusinessKey = instance.BusinessKey,
            nodeName = node.Name,
            assignment = assignedDisplayName
        });
    }

    private static string? ResolveSlaStatus(DateTime now, DateTime? dueAtUtc, DateTime? escalatedAtUtc)
    {
        if (escalatedAtUtc.HasValue)
        {
            return "Escalated";
        }

        if (!dueAtUtc.HasValue)
        {
            return null;
        }

        if (dueAtUtc <= now)
        {
            return "Overdue";
        }

        if (dueAtUtc <= now.AddHours(2))
        {
            return "DueSoon";
        }

        return "OnTrack";
    }

    private async Task<string?> ResolveAssignmentDisplayNameAsync(Guid? userId, Guid? groupId, Guid? roleId, CancellationToken cancellationToken)
    {
        if (userId.HasValue)
        {
            return await dbContext.Users.Where(x => x.Id == userId.Value).Select(x => x.FullName).FirstOrDefaultAsync(cancellationToken);
        }

        if (roleId.HasValue)
        {
            return await dbContext.PlatformAccessRoles.Where(x => x.Id == roleId.Value).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
        }

        if (groupId.HasValue)
        {
            return await dbContext.PlatformAccessGroups.Where(x => x.Id == groupId.Value).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private WorkflowTaskComment? AddTaskComment(Guid workflowTaskId, string commentType, string? body, string visibility, string createdBy, Guid? createdByUserId, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var comment = new WorkflowTaskComment
        {
            Id = Guid.NewGuid(),
            WorkflowTaskId = workflowTaskId,
            CommentType = commentType,
            Body = body.Trim(),
            Visibility = visibility,
            CreatedBy = createdBy,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = createdAtUtc
        };

        dbContext.WorkflowTaskComments.Add(comment);
        return comment;
    }

    private void AddTaskAssignment(Guid workflowTaskId, string actionType, Guid? fromUserId, Guid? fromGroupId, Guid? fromRoleId, Guid? toUserId, Guid? toGroupId, Guid? toRoleId, string? reason, string performedBy, Guid? performedByUserId, DateTime createdAtUtc)
    {
        dbContext.WorkflowTaskAssignments.Add(new WorkflowTaskAssignment
        {
            Id = Guid.NewGuid(),
            WorkflowTaskId = workflowTaskId,
            ActionType = actionType,
            FromUserId = fromUserId,
            FromGroupId = fromGroupId,
            FromRoleId = fromRoleId,
            ToUserId = toUserId,
            ToGroupId = toGroupId,
            ToRoleId = toRoleId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            PerformedBy = performedBy,
            PerformedByUserId = performedByUserId,
            CreatedAtUtc = createdAtUtc
        });
    }

    private static string NormalizeVisibility(string? visibility)
    {
        return visibility is "Participant" or "Watcher" ? visibility : "Internal";
    }

    private async Task<IReadOnlyList<WorkflowTaskCommentResponse>> GetTaskCommentsAsync(IReadOnlyList<Guid> taskIds, CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
        {
            return [];
        }

        return await dbContext.WorkflowTaskComments.AsNoTracking()
            .Where(x => taskIds.Contains(x.WorkflowTaskId))
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => ToTaskCommentResponse(x))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<WorkflowTaskAssignmentResponse>> GetTaskAssignmentsAsync(IReadOnlyList<Guid> taskIds, CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
        {
            return [];
        }

        return await dbContext.WorkflowTaskAssignments.AsNoTracking()
            .Where(x => taskIds.Contains(x.WorkflowTaskId))
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new WorkflowTaskAssignmentResponse(x.Id, x.WorkflowTaskId, x.ActionType, x.FromUserId, x.FromGroupId, x.FromRoleId, x.ToUserId, x.ToGroupId, x.ToRoleId, x.Reason, x.PerformedBy, x.PerformedByUserId, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static WorkflowTaskCommentResponse ToTaskCommentResponse(WorkflowTaskComment comment) =>
        new(comment.Id, comment.WorkflowTaskId, comment.CommentType, comment.Body, comment.Visibility, comment.CreatedBy, comment.CreatedByUserId, comment.CreatedAtUtc);

    private static List<WorkflowTaskActionResponse> DeserializeTaskActions(string? actionsJson)
    {
        if (string.IsNullOrWhiteSpace(actionsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<WorkflowTaskActionResponse>>(actionsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string SerializeTaskActions(IReadOnlyList<WorkflowTaskActionResponse> actions) => JsonSerializer.Serialize(actions);

    private static List<WorkflowTaskActionResponse> BuildAvailableActions(WorkflowTask task, Guid? actorUserId, bool isWorkflowAdministrator)
    {
        if (task.Status is "Completed" or "Cancelled")
        {
            return [];
        }

        var actions = new List<WorkflowTaskActionResponse>();
        var canOperate = isWorkflowAdministrator || !actorUserId.HasValue || task.AssignedToUserId == actorUserId || task.ClaimedByUserId == actorUserId || task.AssignedToUserId is null;

        if (task.Status == "Pending")
        {
            if (canOperate)
            {
                actions.Add(new WorkflowTaskActionResponse("claim", "Claim", "secondary", false));
                actions.Add(new WorkflowTaskActionResponse("delegate", "Delegate", "secondary", false));
                actions.Add(new WorkflowTaskActionResponse("reassign", "Reassign", "secondary", false));
                actions.Add(new WorkflowTaskActionResponse("comment", "Comment", "secondary", true));
                actions.Add(new WorkflowTaskActionResponse("approve", task.TaskMode == "approval" ? "Approve" : "Complete", "primary", false));
                actions.Add(new WorkflowTaskActionResponse("reject", task.TaskMode == "approval" ? "Reject" : "Send Back", "danger", true));
            }
        }
        else if (task.Status == "Claimed")
        {
            if (canOperate)
            {
                actions.Add(new WorkflowTaskActionResponse("unclaim", "Unclaim", "secondary", false));
                actions.Add(new WorkflowTaskActionResponse("delegate", "Delegate", "secondary", false));
                actions.Add(new WorkflowTaskActionResponse("reassign", "Reassign", "secondary", false));
                actions.Add(new WorkflowTaskActionResponse("comment", "Comment", "secondary", true));
                actions.Add(new WorkflowTaskActionResponse("approve", task.TaskMode == "approval" ? "Approve" : "Complete", "primary", false));
                actions.Add(new WorkflowTaskActionResponse("reject", task.TaskMode == "approval" ? "Reject" : "Send Back", "danger", true));
            }
        }

        return actions;
    }

    private async Task<bool> CanUserCompleteTaskAsync(WorkflowTask task, Guid userId, CancellationToken cancellationToken)
    {
        if (task.Status == "Claimed")
        {
            return task.ClaimedByUserId == userId;
        }

        return await CanUserAccessTaskAsync(task, userId, cancellationToken);
    }

    private async Task<bool> CanUserAccessTaskAsync(WorkflowTask task, Guid userId, CancellationToken cancellationToken)
    {
        if (task.AssignedToUserId.HasValue)
        {
            return task.AssignedToUserId == userId;
        }

        if (task.AssignedToGroupId.HasValue)
        {
            return await dbContext.UserAccessGroupAssignments
                .AnyAsync(x => x.ApplicationUserId == userId && x.PlatformAccessGroupId == task.AssignedToGroupId.Value && x.IsEnabled && x.PlatformAccessGroup.IsActive, cancellationToken);
        }

        if (task.AssignedToRoleId.HasValue)
        {
            return (await GetUserRoleIdsAsync(userId, cancellationToken)).Contains(task.AssignedToRoleId.Value);
        }

        return false;
    }

    private async Task<List<Guid>> GetUserRoleIdsAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.UserAccessGroupAssignments
            .Where(x => x.ApplicationUserId == userId && x.IsEnabled && x.PlatformAccessGroup.IsActive)
            .SelectMany(x => x.PlatformAccessGroup.GroupRoles
                .Where(roleLink => roleLink.IsEnabled && roleLink.PlatformAccessRole.IsActive)
                .Select(roleLink => roleLink.PlatformAccessRoleId))
            .Distinct()
            .ToListAsync(cancellationToken);

    private async Task<List<Guid>> GetUserGroupIdsAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.UserAccessGroupAssignments
            .Where(x => x.ApplicationUserId == userId && x.IsEnabled && x.PlatformAccessGroup.IsActive)
            .Select(x => x.PlatformAccessGroupId)
            .Distinct()
            .ToListAsync(cancellationToken);
}

public class WorkflowRuntimeException(string message) : Exception(message);
public sealed class WorkflowRuntimeNotFoundException(string message) : WorkflowRuntimeException(message);
public sealed class WorkflowRuntimeForbiddenException(string message) : WorkflowRuntimeException(message);
