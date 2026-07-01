namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface ITenantProvider
{
    Guid? TenantId { get; }
}
