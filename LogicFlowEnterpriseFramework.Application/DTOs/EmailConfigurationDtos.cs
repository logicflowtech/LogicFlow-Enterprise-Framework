namespace LogicFlowEnterpriseFramework.Application.DTOs;

public sealed record EmailTransportConfigurationResponse(
    string Provider,
    string? Host,
    int? Port,
    bool EnableSsl,
    string? UserName,
    bool HasPassword,
    string? DefaultFromAddress,
    string? DefaultReplyToAddress,
    bool IsConfigured,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

public sealed record UpsertEmailTransportConfigurationRequest(
    string Provider,
    string? Host,
    int? Port,
    bool EnableSsl,
    string? UserName,
    string? Password,
    bool ClearStoredPassword,
    string? DefaultFromAddress,
    string? DefaultReplyToAddress);

public sealed record SendTestEmailRequest(string RecipientEmail);

public sealed record SendTestEmailResponse(
    string RecipientEmail,
    string Subject,
    string SenderEmail,
    DateTimeOffset SentAt);
