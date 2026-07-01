using FluentValidation;
using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Validators;

public sealed class UpsertEmailTransportConfigurationRequestValidator : AbstractValidator<UpsertEmailTransportConfigurationRequest>
{
    public UpsertEmailTransportConfigurationRequestValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Host).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535).When(x => x.Port.HasValue);
        RuleFor(x => x.UserName).MaximumLength(256);
        RuleFor(x => x.Password).MaximumLength(512);
        RuleFor(x => x.DefaultFromAddress).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.DefaultReplyToAddress).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.DefaultReplyToAddress));
        RuleFor(x => x.DefaultReplyToAddress).MaximumLength(256);
    }
}

public sealed class SendTestEmailRequestValidator : AbstractValidator<SendTestEmailRequest>
{
    public SendTestEmailRequestValidator()
    {
        RuleFor(x => x.RecipientEmail).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
