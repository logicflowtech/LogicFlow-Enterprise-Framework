using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/company-related-data/sync")]
public sealed class CompanyRelatedDataSyncController(ICompanyRelatedDataSyncService companyRelatedDataSyncService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.ServiceCenterConfigRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SyncJobSummaryResponse>>>> GetCatalog(CancellationToken cancellationToken)
    {
        var summary = await companyRelatedDataSyncService.GetSummaryAsync(cancellationToken);
        IReadOnlyList<SyncJobSummaryResponse> result = [summary];
        return Ok(ApiResponse<IReadOnlyList<SyncJobSummaryResponse>>.Success(result));
    }

    [HttpGet("status")]
    [HasPermission(Permissions.ServiceCenterConfigRead)]
    public async Task<ActionResult<ApiResponse<CompanyRelatedDataSyncStatusResponse>>> GetStatus(CancellationToken cancellationToken)
    {
        var result = await companyRelatedDataSyncService.GetStatusAsync(cancellationToken);
        return Ok(ApiResponse<CompanyRelatedDataSyncStatusResponse>.Success(result));
    }

    [HttpPost("run")]
    [HasPermission(Permissions.ServiceCenterConfigManage)]
    public async Task<ActionResult<ApiResponse<CompanyRelatedDataSyncStatusResponse>>> Run([FromQuery] long? sourceCompanyId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await companyRelatedDataSyncService.RunSyncAsync(sourceCompanyId, cancellationToken);
            var message = sourceCompanyId.HasValue
                ? $"Company related data sync completed for source company {sourceCompanyId.Value}."
                : "Company related data sync completed.";
            return Ok(ApiResponse<CompanyRelatedDataSyncStatusResponse>.Success(result, message));
        }
        catch (Exception exception)
        {
            return StatusCode(500, ApiResponse<CompanyRelatedDataSyncStatusResponse>.Failure(exception.Message));
        }
    }
}
