using LogicFlowEnterpriseFramework.Api.Workflow.Contracts;
using LogicFlowEnterpriseFramework.Api.Workflow.Runtime;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Workflow;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController(WorkflowRuntimeService runtimeService) : ControllerBase
{
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessTaskAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var actor = User.GetWorkflowActor();
        var task = await runtimeService.GetTaskAsync(id, actor.UserId, User.IsWorkflowAdministrator(), cancellationToken);
        return task is null ? NotFound(ApiResponse<WorkflowTaskResponse>.Failure("Workflow task not found.")) : Ok(ApiResponse<WorkflowTaskResponse>.Success(task));
    }

    [Authorize]
    [HttpGet("{id:guid}/detail")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskDetailResponse>>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessTaskAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var actor = User.GetWorkflowActor();
        var detail = await runtimeService.GetTaskDetailAsync(id, actor.UserId, User.IsWorkflowAdministrator(), cancellationToken);
        return detail is null ? NotFound(ApiResponse<WorkflowTaskDetailResponse>.Failure("Workflow task not found.")) : Ok(ApiResponse<WorkflowTaskDetailResponse>.Success(detail));
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowTaskResponse>>>> GetMyTasks(CancellationToken cancellationToken)
    {
        var actor = User.GetWorkflowActor();
        if (!actor.UserId.HasValue)
        {
            return Unauthorized(ApiResponse<IReadOnlyList<WorkflowTaskResponse>>.Failure("Authenticated user id is missing."));
        }

        var tasks = await runtimeService.GetMyTasksAsync(actor.UserId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WorkflowTaskResponse>>.Success(tasks));
    }

    [Authorize]
    [HttpPost("{id:guid}/claim")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskResponse>>> Claim(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var actor = User.GetWorkflowActor();
            if (!actor.UserId.HasValue)
            {
                return Unauthorized(ApiResponse<WorkflowTaskResponse>.Failure("Authenticated user id is missing."));
            }

            var task = await runtimeService.ClaimTaskAsync(id, new ClaimTaskRequest(actor.UserId.Value, actor.UserName, actor.DisplayName), cancellationToken);
            return Ok(ApiResponse<WorkflowTaskResponse>.Success(task));
        }
        catch (WorkflowRuntimeNotFoundException exception)
        {
            return NotFound(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeForbiddenException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeException exception)
        {
            return BadRequest(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
    }

    [Authorize]
    [HttpPost("{id:guid}/unclaim")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskResponse>>> Unclaim(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var actor = User.GetWorkflowActor();
            if (!actor.UserId.HasValue)
            {
                return Unauthorized(ApiResponse<WorkflowTaskResponse>.Failure("Authenticated user id is missing."));
            }

            var task = await runtimeService.UnclaimTaskAsync(id, new UnclaimTaskRequest(actor.UserId.Value, actor.UserName, actor.DisplayName), cancellationToken);
            return Ok(ApiResponse<WorkflowTaskResponse>.Success(task));
        }
        catch (WorkflowRuntimeNotFoundException exception)
        {
            return NotFound(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeForbiddenException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeException exception)
        {
            return BadRequest(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
    }

    [Authorize]
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskResponse>>> Approve(Guid id, CompleteTaskActionRequest request, CancellationToken cancellationToken)
    {
        return await CompleteAsync(id, request, runtimeService.ApproveTaskAsync, cancellationToken);
    }

    [Authorize]
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskResponse>>> Reject(Guid id, CompleteTaskActionRequest request, CancellationToken cancellationToken)
    {
        return await CompleteAsync(id, request, runtimeService.RejectTaskAsync, cancellationToken);
    }

    [Authorize]
    [HttpPost("{id:guid}/delegate")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskResponse>>> Delegate(Guid id, DelegateTaskActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actor = User.GetWorkflowActor();
            if (!actor.UserId.HasValue)
            {
                return Unauthorized(ApiResponse<WorkflowTaskResponse>.Failure("Authenticated user id is missing."));
            }

            var task = await runtimeService.DelegateTaskAsync(
                id,
                new DelegateTaskRequest(actor.UserId.Value, actor.UserName, actor.DisplayName, request.TargetUserId, request.Reason),
                cancellationToken);
            return Ok(ApiResponse<WorkflowTaskResponse>.Success(task));
        }
        catch (WorkflowRuntimeNotFoundException exception)
        {
            return NotFound(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeForbiddenException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeException exception)
        {
            return BadRequest(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
    }

    [Authorize]
    [HttpPost("{id:guid}/reassign")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskResponse>>> Reassign(Guid id, ReassignTaskActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actor = User.GetWorkflowActor();
            var task = await runtimeService.ReassignTaskAsync(
                id,
                new ReassignTaskRequest(actor.UserId, actor.UserName, actor.DisplayName, request.TargetUserId, request.TargetGroupId, request.TargetRoleId, request.Reason),
                User.IsWorkflowAdministrator(),
                cancellationToken);
            return Ok(ApiResponse<WorkflowTaskResponse>.Success(task));
        }
        catch (WorkflowRuntimeNotFoundException exception)
        {
            return NotFound(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeForbiddenException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeException exception)
        {
            return BadRequest(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
    }

    [Authorize]
    [HttpPost("{id:guid}/comment")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskCommentResponse>>> Comment(Guid id, AddTaskCommentActionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actor = User.GetWorkflowActor();
            var comment = await runtimeService.AddTaskCommentAsync(
                id,
                new AddTaskCommentRequest(actor.UserId, actor.UserName, actor.DisplayName, request.Comment, request.Visibility),
                cancellationToken);
            return Ok(ApiResponse<WorkflowTaskCommentResponse>.Success(comment));
        }
        catch (WorkflowRuntimeNotFoundException exception)
        {
            return NotFound(ApiResponse<WorkflowTaskCommentResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeForbiddenException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<WorkflowTaskCommentResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeException exception)
        {
            return BadRequest(ApiResponse<WorkflowTaskCommentResponse>.Failure(exception.Message));
        }
    }

    private async Task<ActionResult<ApiResponse<WorkflowTaskResponse>>> CompleteAsync(
        Guid id,
        CompleteTaskActionRequest request,
        Func<Guid, CompleteTaskRequest, CancellationToken, Task<WorkflowTaskResponse>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = User.GetWorkflowActor();
            var task = await action(id, new CompleteTaskRequest(actor.UserId, actor.UserName, actor.DisplayName, request.Comment), cancellationToken);
            return Ok(ApiResponse<WorkflowTaskResponse>.Success(task));
        }
        catch (WorkflowRuntimeNotFoundException exception)
        {
            return NotFound(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeForbiddenException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowDefinitionException exception)
        {
            return BadRequest(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
        catch (WorkflowRuntimeException exception)
        {
            return BadRequest(ApiResponse<WorkflowTaskResponse>.Failure(exception.Message));
        }
    }

    private async Task<bool> CanAccessTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        if (User.IsWorkflowAdministrator())
        {
            return true;
        }

        var actor = User.GetWorkflowActor();
        return actor.UserId.HasValue && await runtimeService.CanUserAccessTaskAsync(taskId, actor.UserId.Value, cancellationToken);
    }
}
