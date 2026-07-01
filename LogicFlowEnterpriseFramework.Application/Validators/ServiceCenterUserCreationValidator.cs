using FluentValidation;
using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Validators;

public sealed class CreateServiceCenterUserRequestValidator : AbstractValidator<CreateServiceCenterUserRequest>
{
    public CreateServiceCenterUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleForEach(x => x.RoleIds).NotEqual(Guid.Empty);
    }
}
