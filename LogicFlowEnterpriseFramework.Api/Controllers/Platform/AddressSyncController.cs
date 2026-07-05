using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/addresses/sync")]
public sealed class AddressSyncController(IAddressSyncService addressSyncService) : ControllerBase
{
    [HttpPost("run")]
    [HasPermission(Permissions.SystemAdminSettingsManage)]
    public async Task<ActionResult<ApiResponse<AddressSyncResponse>>> Run(CancellationToken cancellationToken)
    {
        try
        {
            var result = await addressSyncService.RunSyncAsync(cancellationToken);
            return Ok(ApiResponse<AddressSyncResponse>.Success(result, "Address sync completed."));
        }
        catch (Exception exception)
        {
            return StatusCode(500, ApiResponse<AddressSyncResponse>.Failure(exception.Message));
        }
    }
}
