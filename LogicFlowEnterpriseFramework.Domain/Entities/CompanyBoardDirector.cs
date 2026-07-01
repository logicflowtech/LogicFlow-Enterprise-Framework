namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class CompanyBoardDirector : BaseEntity
{
    public Guid CompanyProfileId { get; set; }
    public CompanyProfile CompanyProfile { get; set; } = null!;
    public long MigratedId { get; set; }
    public string? Name { get; set; }
    public long? LegacyNationalityId { get; set; }
    public decimal? SharePercentage { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
