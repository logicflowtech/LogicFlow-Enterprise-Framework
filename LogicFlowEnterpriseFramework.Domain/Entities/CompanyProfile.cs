namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class CompanyProfile
{
    public Guid Id { get; set; }
    public long? MigratedId { get; set; }
    public string? CompanyName { get; set; }
    public string? RegistrationNo { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public DateTime? DateOfIncorporation { get; set; }
    public string? TelephoneNo { get; set; }
    public string? FaxNo { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? IncomeTaxNo { get; set; }
    public string? EpfNo { get; set; }
    public string? SocsoNo { get; set; }
    public int? UserId { get; set; }
    public long? CompanySignatureId { get; set; }
    public int? CompanyType { get; set; }
    public bool? IsCompanyCertified { get; set; }
    public int? CompanyApprovalStatus { get; set; }
    public bool? IsPaid { get; set; }
    public bool? IsCompanyLocal { get; set; }
    public int? CreatedBySourceUserId { get; set; }
    public DateTime? SourceCreatedDateTime { get; set; }
    public int? ModifiedBySourceUserId { get; set; }
    public DateTime? SourceModifiedDateTime { get; set; }
    public Guid? AddressId { get; set; }
    public long? LegacyAddressId { get; set; }
    public string? BackgroundDescription1 { get; set; }
    public string? NewSsmCompanyRegNo { get; set; }
    public int? CompanyStatusId { get; set; }
    public int? TotalEmployment { get; set; }
    public int? AnnualClosingDateDay { get; set; }
    public int? AnnualClosingDateMonth { get; set; }
    public string? AprNo { get; set; }
    public string? NonCode { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public ICollection<CompanyProfileUserAssignment> UserAssignments { get; set; } = new List<CompanyProfileUserAssignment>();
    public ICollection<CompanyAuthorizedPerson> AuthorizedPersons { get; set; } = new List<CompanyAuthorizedPerson>();
    public ICollection<CompanyBoardDirector> BoardDirectors { get; set; } = new List<CompanyBoardDirector>();
    public ICollection<CompanyAttachmentDocument> AttachmentDocuments { get; set; } = new List<CompanyAttachmentDocument>();
    public ICollection<CompanyProfileFinancialDetail> FinancialDetails { get; set; } = new List<CompanyProfileFinancialDetail>();
    public ICollection<CompanyProfileAuthorizedCapital> AuthorizedCapitalRecords { get; set; } = new List<CompanyProfileAuthorizedCapital>();
}
