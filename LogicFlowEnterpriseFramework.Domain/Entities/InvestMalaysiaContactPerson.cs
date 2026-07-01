namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class InvestMalaysiaContactPerson
{
    public long LegacyContactPersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? UserDesignationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TelephoneNo { get; set; } = string.Empty;
    public string FaxNo { get; set; } = string.Empty;
    public long? LegacyCompanyId { get; set; }
    public long? TempContactPersonId { get; set; }
    public bool Status { get; set; }
    public int? ContactPersonApprovalStatus { get; set; }
    public int? LegacyUserId { get; set; }
    public int? CreatedByUserId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? SourceCreatedDateTime { get; set; }
    public int? ModifiedByUserId { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public DateTime? SourceModifiedDateTime { get; set; }
    public int? TitleId { get; set; }
    public string TitleName { get; set; } = string.Empty;
    public string OtherDesignationName { get; set; } = string.Empty;
    public DateTime LastSyncedAt { get; set; }
}
