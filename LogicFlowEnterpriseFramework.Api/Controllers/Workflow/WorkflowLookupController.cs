using LogicFlowEnterpriseFramework.Api.Workflow.Contracts;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Workflow;

[ApiController]
[Route("api/workflow/lookups")]
[Authorize]
public sealed class WorkflowLookupController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowUserLookupResponse>>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new WorkflowUserLookupResponse(x.Id, x.UserName ?? string.Empty, x.FullName, x.Email, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<WorkflowUserLookupResponse>>.Success(users));
    }

    [HttpGet("groups")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowGroupLookupResponse>>>> GetGroups(CancellationToken cancellationToken)
    {
        var groups = await dbContext.PlatformAccessGroups.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new WorkflowGroupLookupResponse(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<WorkflowGroupLookupResponse>>.Success(groups));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowRoleLookupResponse>>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await dbContext.PlatformAccessRoles.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new WorkflowRoleLookupResponse(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<WorkflowRoleLookupResponse>>.Success(roles));
    }
}
