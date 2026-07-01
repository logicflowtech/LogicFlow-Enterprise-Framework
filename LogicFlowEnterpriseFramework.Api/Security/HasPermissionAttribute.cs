using Microsoft.AspNetCore.Authorization;

namespace LogicFlowEnterpriseFramework.Api.Security;

public sealed class HasPermissionAttribute(string permission)
    : AuthorizeAttribute($"{PermissionPolicyProvider.PolicyPrefix}{permission}");
