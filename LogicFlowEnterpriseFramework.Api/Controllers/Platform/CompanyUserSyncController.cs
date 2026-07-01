using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/company-users/sync")]
public sealed class CompanyUserSyncController(ICompanyUserSyncService companyUserSyncService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.ServiceCenterConfigRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SyncJobSummaryResponse>>>> GetCatalog(CancellationToken cancellationToken)
    {
        var summary = await companyUserSyncService.GetSummaryAsync(cancellationToken);
        IReadOnlyList<SyncJobSummaryResponse> result = [summary];
        return Ok(ApiResponse<IReadOnlyList<SyncJobSummaryResponse>>.Success(result));
    }

    [HttpGet("status")]
    [HasPermission(Permissions.ServiceCenterConfigRead)]
    public async Task<ActionResult<ApiResponse<CompanyUserSyncStatusResponse>>> GetStatus(CancellationToken cancellationToken)
    {
        var result = await companyUserSyncService.GetStatusAsync(cancellationToken);
        return Ok(ApiResponse<CompanyUserSyncStatusResponse>.Success(result));
    }

    [HttpPost("run")]
    [HasPermission(Permissions.ServiceCenterConfigManage)]
    public async Task<ActionResult<ApiResponse<CompanyUserSyncStatusResponse>>> Run([FromQuery] long? sourceCompanyId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await companyUserSyncService.RunSyncAsync(sourceCompanyId, cancellationToken);
            var message = sourceCompanyId.HasValue
                ? $"Company user sync completed for source company {sourceCompanyId.Value}."
                : "Company user sync completed.";
            return Ok(ApiResponse<CompanyUserSyncStatusResponse>.Success(result, message));
        }
        catch (Exception exception)
        {
            return StatusCode(500, ApiResponse<CompanyUserSyncStatusResponse>.Failure(exception.Message));
        }
    }
}
