using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface IEmailConfigurationService
{
    Task<EmailTransportConfigurationResponse> GetAsync(CancellationToken cancellationToken = default);
    Task<EmailTransportConfigurationResponse> UpsertAsync(UpsertEmailTransportConfigurationRequest request, CancellationToken cancellationToken = default);
    Task<SendTestEmailResponse> SendTestAsync(SendTestEmailRequest request, CancellationToken cancellationToken = default);
}
