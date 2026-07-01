using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LogicFlowEnterpriseFramework.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/invest-malaysia/access")]
public sealed class InvestMalaysiaAccessController(IInvestMalaysiaAccessService investMalaysiaAccessService) : ControllerBase
{
    [HttpGet("groups")]
    [HasPermission(Permissions.ServiceCenterConfigRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InvestMalaysiaGroupCatalogResponse>>>> GetGroups(CancellationToken cancellationToken)
    {
        var result = await investMalaysiaAccessService.GetGroupCatalogAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<InvestMalaysiaGroupCatalogResponse>>.Success(result));
    }

    [HttpPost("group-mappings")]
    [HasPermission(Permissions.ServiceCenterConfigManage)]
    public async Task<ActionResult<ApiResponse<InvestMalaysiaGroupCatalogResponse>>> CreateGroupMapping(CreateInvestMalaysiaGroupMappingRequest request, CancellationToken cancellationToken)
    {
        var result = await investMalaysiaAccessService.CreateGroupMappingAsync(request, cancellationToken);
        return Ok(ApiResponse<InvestMalaysiaGroupCatalogResponse>.Success(result, "InvestMalaysia group mapping created."));
    }

    [HttpPut("group-mappings/{mappingId:guid}")]
    [HasPermission(Permissions.ServiceCenterConfigManage)]
    public async Task<ActionResult<ApiResponse<InvestMalaysiaGroupCatalogResponse>>> UpdateGroupMapping(Guid mappingId, UpdateInvestMalaysiaGroupMappingRequest request, CancellationToken cancellationToken)
    {
        var result = await investMalaysiaAccessService.UpdateGroupMappingAsync(mappingId, request, cancellationToken);
        return Ok(ApiResponse<InvestMalaysiaGroupCatalogResponse>.Success(result, "InvestMalaysia group mapping updated."));
    }

    [HttpDelete("group-mappings/{mappingId:guid}")]
    [HasPermission(Permissions.ServiceCenterConfigManage)]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteGroupMapping(Guid mappingId, CancellationToken cancellationToken)
    {
        await investMalaysiaAccessService.DeleteGroupMappingAsync(mappingId, cancellationToken);
        return Ok(ApiResponse<object?>.Success(null, "InvestMalaysia group mapping deleted."));
    }
}
