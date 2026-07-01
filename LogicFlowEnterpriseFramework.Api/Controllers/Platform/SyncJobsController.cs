using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/sync-jobs")]
public sealed class SyncJobsController(ISyncCatalogService syncCatalogService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.ServiceCenterConfigRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SyncJobSummaryResponse>>>> GetCatalog(CancellationToken cancellationToken)
    {
        var result = await syncCatalogService.GetCatalogAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SyncJobSummaryResponse>>.Success(result));
    }

    [HttpPost("{syncKey}/run")]
    [HasPermission(Permissions.ServiceCenterConfigManage)]
    public async Task<ActionResult<ApiResponse<SyncJobSummaryResponse>>> Run(string syncKey, CancellationToken cancellationToken)
    {
        try
        {
            var result = await syncCatalogService.RunAsync(syncKey, cancellationToken);
            return Ok(ApiResponse<SyncJobSummaryResponse>.Success(result, $"{result.Name} completed."));
        }
        catch (Exception exception)
        {
            return StatusCode(500, ApiResponse<SyncJobSummaryResponse>.Failure(exception.Message));
        }
    }
}
