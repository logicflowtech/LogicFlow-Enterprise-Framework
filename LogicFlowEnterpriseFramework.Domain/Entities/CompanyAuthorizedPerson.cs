namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class CompanyAuthorizedPerson : BaseEntity
{
    public Guid CompanyProfileId { get; set; }
    public CompanyProfile CompanyProfile { get; set; } = null!;
    public long MigratedId { get; set; }
    public string? FullName { get; set; }
    public string? Designation { get; set; }
    public int? LegacyIdentityTypeId { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Email { get; set; }
    public string? TelephoneNo { get; set; }
    public int? LegacyUserId { get; set; }
    public bool? IsDigiCertPaid { get; set; }
    public bool? IsCertified { get; set; }
    public bool? IsPinVerified { get; set; }
    public bool IsDeletedInSource { get; set; }
    public int? LegacyTitleId { get; set; }
    public long? LegacyCitizenshipId { get; set; }
    public bool? CanEdit { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
