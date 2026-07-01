using Microsoft.AspNetCore.Authorization;

namespace LogicFlowEnterpriseFramework.Api.Security;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
