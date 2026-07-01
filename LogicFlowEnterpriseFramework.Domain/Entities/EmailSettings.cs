namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class EmailSettings : BaseEntity
{
    public string Provider { get; set; } = "Smtp";
    public string? Host { get; set; }
    public int? Port { get; set; } = 587;
    public string? UserName { get; set; }
    public string? EncryptedPassword { get; set; }
    public bool EnableSsl { get; set; } = true;
    public string? DefaultFromAddress { get; set; }
    public string? DefaultReplyToAddress { get; set; }
}
