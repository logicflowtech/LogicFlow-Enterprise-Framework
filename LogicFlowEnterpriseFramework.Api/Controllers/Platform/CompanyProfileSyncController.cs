using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/company-profiles/sync")]
public sealed class CompanyProfileSyncController(ICompanyProfileSyncService companyProfileSyncService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.ServiceCenterConfigRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SyncJobSummaryResponse>>>> GetCatalog(CancellationToken cancellationToken)
    {
        var status = await companyProfileSyncService.GetStatusAsync(cancellationToken);

        IReadOnlyList<SyncJobSummaryResponse> result =
        [
            new SyncJobSummaryResponse(
                "company-profiles",
                "Company Profile Sync",
                "Incremental sync of company records into the local application cache.",
                status.SourceObjectName,
                "[dbo].[CompanyProfiles]",
                status.ScheduleEnabled,
                status.ScheduleMinutes,
                status.BatchSize,
                status.UseLocalSynonym,
                status.SourceConnectionStringName,
                status.SourceConnectionConfigured,
                status.LocalRowCount,
                status.LastStartedAt,
                status.LastCompletedAt,
                status.LastRunSucceeded,
                status.LastProcessedRows,
                status.LastRunMessage,
                "/configuration/company-profiles")
        ];

        return Ok(ApiResponse<IReadOnlyList<SyncJobSummaryResponse>>.Success(result));
    }

    [HttpGet("status")]
    [HasPermission(Permissions.ServiceCenterConfigRead)]
    public async Task<ActionResult<ApiResponse<CompanyProfileSyncStatusResponse>>> GetStatus(CancellationToken cancellationToken)
    {
        var result = await companyProfileSyncService.GetStatusAsync(cancellationToken);
        return Ok(ApiResponse<CompanyProfileSyncStatusResponse>.Success(result));
    }

    [HttpPost("run")]
    [HasPermission(Permissions.ServiceCenterConfigManage)]
    public async Task<ActionResult<ApiResponse<CompanyProfileSyncStatusResponse>>> Run(CancellationToken cancellationToken)
    {
        try
        {
            var result = await companyProfileSyncService.RunSyncAsync(cancellationToken);
            return Ok(ApiResponse<CompanyProfileSyncStatusResponse>.Success(result, "Company profile sync completed."));
        }
        catch (Exception exception)
        {
            return StatusCode(500, ApiResponse<CompanyProfileSyncStatusResponse>.Failure(exception.Message));
        }
    }
}
