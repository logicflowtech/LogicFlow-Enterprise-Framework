namespace LogicFlowEnterpriseFramework.Infrastructure.Notifications;

public sealed class EmailTransportOptions
{
    public const string SectionName = "EmailTransport";

    public string Provider { get; set; } = "Smtp";
    public string? Host { get; set; }
    public int? Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? DefaultFromAddress { get; set; }
    public string? DefaultReplyToAddress { get; set; }
}
