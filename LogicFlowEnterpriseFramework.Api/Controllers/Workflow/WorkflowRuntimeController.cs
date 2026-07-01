using LogicFlowEnterpriseFramework.Api.Workflow.Contracts;
using LogicFlowEnterpriseFramework.Api.Workflow.Runtime;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Workflow;

[ApiController]
public sealed class WorkflowRuntimeController(WorkflowRuntimeService runtimeService) : ControllerBase
{
    [Authorize]
    [HttpGet("api/workflow-instances")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowInstanceListItemResponse>>>> GetInstances(
        [FromQuery] Guid? workflowDefinitionId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var actor = User.GetWorkflowActor();
        var instances = await runtimeService.GetInstancesAsync(
            actor.UserId,
            User.IsWorkflowAdministrator(),
            workflowDefinitionId,
            status,
            search,
            take,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<WorkflowInstanceListItemResponse>>.Success(instances));
    }

    [Authorize]
    [HttpPost("api/workflows/{workflowDefinitionId:guid}/start")]
    public async Task<ActionResult<ApiResponse<WorkflowInstanceResponse>>> Start(Guid workflowDefinitionId, StartWorkflowActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actor = User.GetWorkflowActor();
            var instance = await runtimeService.StartAsync(
                workflowDefinitionId,
                new StartWorkflowRequest(request.BusinessKey, request.Title, actor.UserId, actor.UserName, actor.DisplayName, request.Variables),
                cancellationToken);
            return CreatedAtAction(nameof(GetInstance), new { id = instance.Id }, ApiResponse<WorkflowInstanceResponse>.Success(instance));
        }
        catch (WorkflowDefinitionException exception)
        {
            return BadRequest(ApiResponse<WorkflowInstanceResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeException exception)
        {
            return BadRequest(ApiResponse<WorkflowInstanceResponse>.Failure(exception.Message));
        }
    }

    [Authorize]
    [HttpGet("api/workflow-instances/{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkflowInstanceResponse>>> GetInstance(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessInstanceAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var instance = await runtimeService.GetInstanceAsync(id, cancellationToken);
        return instance is null ? NotFound(ApiResponse<WorkflowInstanceResponse>.Failure("Workflow instance not found.")) : Ok(ApiResponse<WorkflowInstanceResponse>.Success(instance));
    }

    [Authorize]
    [HttpGet("api/workflow-instances/{id:guid}/detail")]
    public async Task<ActionResult<ApiResponse<WorkflowInstanceDetailResponse>>> GetInstanceDetail(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessInstanceAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var detail = await runtimeService.GetInstanceDetailAsync(id, cancellationToken);
        return detail is null ? NotFound(ApiResponse<WorkflowInstanceDetailResponse>.Failure("Workflow instance not found.")) : Ok(ApiResponse<WorkflowInstanceDetailResponse>.Success(detail));
    }

    [Authorize]
    [HttpPost("api/workflow-instances/{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<WorkflowInstanceResponse>>> CancelInstance(Guid id, CancelWorkflowInstanceActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actor = User.GetWorkflowActor();
            if (!User.IsWorkflowAdministrator() && (!actor.UserId.HasValue || !await runtimeService.CanUserCancelInstanceAsync(id, actor.UserId.Value, cancellationToken)))
            {
                return Forbid();
            }

            var response = await runtimeService.CancelInstanceAsync(id, new CancelWorkflowInstanceRequest(actor.UserId, actor.UserName, actor.DisplayName, request.Reason), cancellationToken);
            return Ok(ApiResponse<WorkflowInstanceResponse>.Success(response));
        }
        catch (WorkflowRuntimeNotFoundException exception)
        {
            return NotFound(ApiResponse<WorkflowInstanceResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeException exception)
        {
            return BadRequest(ApiResponse<WorkflowInstanceResponse>.Failure(exception.Message));
        }
    }

    [Authorize]
    [HttpGet("api/workflow-instances/{id:guid}/audit-logs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowAuditLogResponse>>>> GetAuditLogs(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessInstanceAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var logs = await runtimeService.GetAuditLogsAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WorkflowAuditLogResponse>>.Success(logs));
    }

    private async Task<bool> CanAccessInstanceAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        if (User.IsWorkflowAdministrator())
        {
            return true;
        }

        var actor = User.GetWorkflowActor();
        return actor.UserId.HasValue && await runtimeService.CanUserAccessInstanceAsync(instanceId, actor.UserId.Value, cancellationToken);
    }
}
