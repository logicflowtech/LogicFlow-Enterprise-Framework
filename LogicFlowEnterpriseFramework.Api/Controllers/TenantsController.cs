using LogicFlowEnterpriseFramework.Api.Security;
using LogicFlowEnterpriseFramework.Domain.Entities;
using LogicFlowEnterpriseFramework.Domain.Interfaces;
using LogicFlowEnterpriseFramework.Shared.Constants;
using LogicFlowEnterpriseFramework.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFlowEnterpriseFramework.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TenantsController(IRepository<Tenant> tenants) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.TenantsRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<Tenant>>>> Get(CancellationToken cancellationToken)
    {
        var result = await tenants.Query().AsNoTracking().ToListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<Tenant>>.Success(result));
    }
}
