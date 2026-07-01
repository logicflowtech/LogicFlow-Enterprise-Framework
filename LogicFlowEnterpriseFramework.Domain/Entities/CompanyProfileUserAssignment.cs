namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class CompanyProfileUserAssignment : BaseEntity
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = null!;
    public Guid CompanyProfileId { get; set; }
    public CompanyProfile CompanyProfile { get; set; } = null!;
    public long? LegacyContactPersonId { get; set; }
    public bool IsActive { get; set; } = true;
}
