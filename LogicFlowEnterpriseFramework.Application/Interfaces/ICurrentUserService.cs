namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    string? UserName { get; }
}
