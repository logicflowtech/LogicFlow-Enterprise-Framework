namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string Email { get; set; } = "admin@logicflow.local";
    public string FullName { get; set; } = "System Administrator";
    public string InitialPassword { get; set; } = string.Empty;
}
