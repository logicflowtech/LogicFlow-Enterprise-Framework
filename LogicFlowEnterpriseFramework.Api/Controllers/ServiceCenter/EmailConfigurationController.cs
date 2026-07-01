using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LogicFlowEnterpriseFramework.Api.Controllers.ServiceCenter;

[ApiController]
[Route("api/service-center/configuration/email")]
public sealed class EmailConfigurationController(IEmailConfigurationService emailConfigurationService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.ServiceCenterConfigRead)]
    public async Task<ActionResult<ApiResponse<EmailTransportConfigurationResponse>>> Get(CancellationToken cancellationToken)
    {
        var result = await emailConfigurationService.GetAsync(cancellationToken);
        return Ok(ApiResponse<EmailTransportConfigurationResponse>.Success(result));
    }

    [HttpPut]
    [HasPermission(Permissions.ServiceCenterConfigManage)]
    public async Task<ActionResult<ApiResponse<EmailTransportConfigurationResponse>>> Upsert(UpsertEmailTransportConfigurationRequest request, CancellationToken cancellationToken)
    {
        var result = await emailConfigurationService.UpsertAsync(request, cancellationToken);
        return Ok(ApiResponse<EmailTransportConfigurationResponse>.Success(result, "Email transport configuration saved."));
    }

    [HttpPost("test")]
    [HasPermission(Permissions.ServiceCenterConfigManage)]
    public async Task<ActionResult<ApiResponse<SendTestEmailResponse>>> SendTest(SendTestEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await emailConfigurationService.SendTestAsync(request, cancellationToken);
        return Ok(ApiResponse<SendTestEmailResponse>.Success(result, "Test email sent."));
    }
}
