namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class UserProfile
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = default!;
    public string? Nric { get; set; }
    public string? PassportNumber { get; set; }
    public int? LegacyIdentificationTypeId { get; set; }
    public string? CustomDesignationName { get; set; }
    public string? FaxNumber { get; set; }
    public Guid? AddressId { get; set; }
    public long? LegacyAddressId { get; set; }
    public int? LegacyDesignationId { get; set; }
    public int? LegacyTitleId { get; set; }
    public string? TitleDisplayName { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
}
