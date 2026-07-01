using FluentValidation;
using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Validators;

public sealed class ServiceCenterAccessUpsertRequestValidator : AbstractValidator<ServiceCenterAccessUpsertRequest>
{
    public ServiceCenterAccessUpsertRequestValidator()
    {
        RuleForEach(x => x.RoleIds).NotEqual(Guid.Empty);
        RuleForEach(x => x.Escalations).ChildRules(escalation =>
        {
            escalation.RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            escalation.RuleFor(x => x.Priority).NotEmpty().MaximumLength(50);
            escalation.RuleFor(x => x.Level).GreaterThanOrEqualTo(1);
        });
    }
}
