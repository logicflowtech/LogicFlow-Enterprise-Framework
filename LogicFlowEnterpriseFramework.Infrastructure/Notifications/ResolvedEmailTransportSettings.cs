namespace LogicFlowEnterpriseFramework.Infrastructure.Notifications;

public sealed record ResolvedEmailTransportSettings(
    string Provider,
    string? Host,
    int? Port,
    bool EnableSsl,
    string? UserName,
    string? Password,
    string? DefaultFromAddress,
    string? DefaultReplyToAddress,
    bool HasPassword,
    bool IsConfigured);
