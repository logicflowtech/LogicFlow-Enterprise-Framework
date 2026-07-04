using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LogicFlowEnterpriseFramework.Blazor.Models;

public sealed class ServiceCenterCreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public List<Guid> RoleIds { get; set; } = [];
    public bool IsAccessEnabled { get; set; } = true;
    public List<ServiceCenterEscalationAssignmentRequestModel> Escalations { get; set; } = [];
}

public sealed class ServiceCenterAccessUpdateRequest
{
    public List<Guid> RoleIds { get; set; } = [];
    public bool IsAccessEnabled { get; set; } = true;
    public List<ServiceCenterEscalationAssignmentRequestModel> Escalations { get; set; } = [];
}

public sealed class RoleOption
{
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("roleCode")]
    public string RoleCode { get; set; } = string.Empty;

    [JsonPropertyName("roleName")]
    public string RoleName { get; set; } = string.Empty;

    [JsonPropertyName("isSystemManaged")]
    public bool IsSystemManaged { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

public sealed class CreatePlatformFeatureRequestModel
{
    [Required]
    [StringLength(150)]
    public string FeatureCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FeatureName { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdatePlatformFeatureRequestModel
{
    [Required]
    [StringLength(200)]
    public string FeatureName { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PlatformFeatureCatalogItemModel
{
    [JsonPropertyName("featureId")]
    public Guid FeatureId { get; set; }

    [JsonPropertyName("featureCode")]
    public string FeatureCode { get; set; } = string.Empty;

    [JsonPropertyName("featureName")]
    public string FeatureName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isDeprecated")]
    public bool IsDeprecated { get; set; }

    [JsonPropertyName("accessRoleCount")]
    public int AccessRoleCount { get; set; }
}

public sealed class CreateAccessRoleRequestModel
{
    [Required]
    [StringLength(150)]
    public string RoleCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public List<Guid> FeatureIds { get; set; } = [];
}

public sealed class UpdateAccessRoleRequestModel
{
    [Required]
    [StringLength(200)]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public List<Guid> FeatureIds { get; set; } = [];
}

public sealed class AccessRoleCatalogItemModel
{
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("roleCode")]
    public string RoleCode { get; set; } = string.Empty;

    [JsonPropertyName("roleName")]
    public string RoleName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("isSystem")]
    public bool IsSystem { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("featureCount")]
    public int FeatureCount { get; set; }

    [JsonPropertyName("groupCount")]
    public int GroupCount { get; set; }

    [JsonPropertyName("featureCodes")]
    public List<string> FeatureCodes { get; set; } = [];

    [JsonPropertyName("featureNames")]
    public List<string> FeatureNames { get; set; } = [];
}

public sealed class CreateAccessGroupRequestModel
{
    [Required]
    [StringLength(150)]
    public string GroupCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string GroupName { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public List<Guid> RoleIds { get; set; } = [];
}

public sealed class UpdateAccessGroupRequestModel
{
    [Required]
    [StringLength(200)]
    public string GroupName { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public List<Guid> RoleIds { get; set; } = [];
}

public sealed class AccessGroupCatalogItemModel
{
    [JsonPropertyName("groupId")]
    public Guid GroupId { get; set; }

    [JsonPropertyName("groupCode")]
    public string GroupCode { get; set; } = string.Empty;

    [JsonPropertyName("groupName")]
    public string GroupName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("isSystem")]
    public bool IsSystem { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("roleCount")]
    public int RoleCount { get; set; }

    [JsonPropertyName("memberCount")]
    public int MemberCount { get; set; }

    [JsonPropertyName("roleCodes")]
    public List<string> RoleCodes { get; set; } = [];

    [JsonPropertyName("roleNames")]
    public List<string> RoleNames { get; set; } = [];
}

public sealed class InvestMalaysiaGroupCatalogItemModel
{
    [JsonPropertyName("mappingId")]
    public Guid? MappingId { get; set; }

    [JsonPropertyName("legacyGroupId")]
    public int? LegacyGroupId { get; set; }

    [JsonPropertyName("investMalaysiaGroupName")]
    public string InvestMalaysiaGroupName { get; set; } = string.Empty;

    [JsonPropertyName("userCount")]
    public int UserCount { get; set; }

    [JsonPropertyName("roleCount")]
    public int RoleCount { get; set; }

    [JsonPropertyName("isDiscoveredFromSync")]
    public bool IsDiscoveredFromSync { get; set; }

    [JsonPropertyName("platformAccessGroupId")]
    public Guid? PlatformAccessGroupId { get; set; }

    [JsonPropertyName("platformAccessGroupCode")]
    public string? PlatformAccessGroupCode { get; set; }

    [JsonPropertyName("platformAccessGroupName")]
    public string? PlatformAccessGroupName { get; set; }

    [JsonPropertyName("isMapped")]
    public bool IsMapped { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
}

public sealed class CreateInvestMalaysiaGroupMappingRequestModel
{
    [Required]
    [StringLength(256)]
    public string InvestMalaysiaGroupName { get; set; } = string.Empty;

    [Required]
    public Guid? PlatformAccessGroupId { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class UpdateInvestMalaysiaGroupMappingRequestModel
{
    [Required]
    [StringLength(256)]
    public string InvestMalaysiaGroupName { get; set; } = string.Empty;

    [Required]
    public Guid? PlatformAccessGroupId { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class PermissionCatalogItemModel
{
    [JsonPropertyName("permissionCode")]
    public string PermissionCode { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public sealed class CreateSecurityRoleRequestModel
{
    [Required]
    [StringLength(256)]
    public string RoleName { get; set; } = string.Empty;

    public List<string> PermissionCodes { get; set; } = [];
}

public sealed class SecurityRoleCatalogItemModel
{
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("roleName")]
    public string RoleName { get; set; } = string.Empty;

    [JsonPropertyName("isSystem")]
    public bool IsSystem { get; set; }

    [JsonPropertyName("permissionCount")]
    public int PermissionCount { get; set; }

    [JsonPropertyName("assignedUserCount")]
    public int AssignedUserCount { get; set; }

    [JsonPropertyName("permissionCodes")]
    public List<string> PermissionCodes { get; set; } = [];
}

public sealed class UserRoleAssignment
{
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("roleCode")]
    public string RoleCode { get; set; } = string.Empty;

    [JsonPropertyName("roleName")]
    public string RoleName { get; set; } = string.Empty;
}

public sealed class ServiceCenterEscalationAssignmentRequestModel
{
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public sealed class ServiceCenterAccessUserSummary
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("isAccessEnabled")]
    public bool IsAccessEnabled { get; set; }

    [JsonPropertyName("roles")]
    public List<UserRoleAssignment> Roles { get; set; } = [];

    [JsonPropertyName("escalationAssignments")]
    public int EscalationAssignments { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
}

public sealed class ServiceCenterAccessDetail
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("isAccessEnabled")]
    public bool IsAccessEnabled { get; set; }

    [JsonPropertyName("roles")]
    public List<UserRoleAssignment> Roles { get; set; } = [];

    [JsonPropertyName("escalations")]
    public List<ServiceCenterEscalationAssignmentDetail> Escalations { get; set; } = [];

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
}

public sealed class ServiceCenterEscalationAssignmentDetail
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

public sealed class EmailTransportConfigurationModel
{
    [Required]
    [StringLength(50)]
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "Smtp";

    [Required]
    [StringLength(256)]
    [JsonPropertyName("host")]
    public string? Host { get; set; }

    [Range(1, 65535)]
    [JsonPropertyName("port")]
    public int? Port { get; set; } = 587;

    [JsonPropertyName("enableSsl")]
    public bool EnableSsl { get; set; } = true;

    [StringLength(256)]
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [StringLength(512)]
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("clearStoredPassword")]
    public bool ClearStoredPassword { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    [JsonPropertyName("defaultFromAddress")]
    public string? DefaultFromAddress { get; set; }

    [EmailAddress]
    [StringLength(256)]
    [JsonPropertyName("defaultReplyToAddress")]
    public string? DefaultReplyToAddress { get; set; }

    [JsonPropertyName("hasPassword")]
    public bool HasPassword { get; set; }

    [JsonPropertyName("isConfigured")]
    public bool IsConfigured { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
}

public sealed class TestEmailRequestModel
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [JsonPropertyName("recipientEmail")]
    public string RecipientEmail { get; set; } = string.Empty;
}

public sealed class TestEmailResponseModel
{
    [JsonPropertyName("recipientEmail")]
    public string RecipientEmail { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("senderEmail")]
    public string SenderEmail { get; set; } = string.Empty;

    [JsonPropertyName("sentAt")]
    public DateTimeOffset SentAt { get; set; }
}

public sealed class SyncJobSummaryModel
{
    [JsonPropertyName("syncKey")]
    public string SyncKey { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("sourceObjectName")]
    public string SourceObjectName { get; set; } = string.Empty;

    [JsonPropertyName("targetObjectName")]
    public string TargetObjectName { get; set; } = string.Empty;

    [JsonPropertyName("scheduleEnabled")]
    public bool ScheduleEnabled { get; set; }

    [JsonPropertyName("scheduleMinutes")]
    public int ScheduleMinutes { get; set; }

    [JsonPropertyName("batchSize")]
    public int BatchSize { get; set; }

    [JsonPropertyName("useLocalSynonym")]
    public bool UseLocalSynonym { get; set; }

    [JsonPropertyName("sourceConnectionStringName")]
    public string? SourceConnectionStringName { get; set; }

    [JsonPropertyName("sourceConnectionConfigured")]
    public bool SourceConnectionConfigured { get; set; }

    [JsonPropertyName("localRowCount")]
    public long LocalRowCount { get; set; }

    [JsonPropertyName("lastStartedAt")]
    public DateTimeOffset? LastStartedAt { get; set; }

    [JsonPropertyName("lastCompletedAt")]
    public DateTimeOffset? LastCompletedAt { get; set; }

    [JsonPropertyName("lastRunSucceeded")]
    public bool? LastRunSucceeded { get; set; }

    [JsonPropertyName("lastProcessedRows")]
    public int LastProcessedRows { get; set; }

    [JsonPropertyName("lastRunMessage")]
    public string? LastRunMessage { get; set; }

    [JsonPropertyName("detailPath")]
    public string DetailPath { get; set; } = string.Empty;
}

public sealed class CompanyProfileListItemModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("migratedId")]
    public long? MigratedId { get; set; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("registrationNo")]
    public string? RegistrationNo { get; set; }

    [JsonPropertyName("newSsmCompanyRegNo")]
    public string? NewSsmCompanyRegNo { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("telephoneNo")]
    public string? TelephoneNo { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    [JsonPropertyName("isCompanyLocal")]
    public bool? IsCompanyLocal { get; set; }

    [JsonPropertyName("companyApprovalStatus")]
    public int? CompanyApprovalStatus { get; set; }

    [JsonPropertyName("sourceModifiedDateTime")]
    public DateTime? SourceModifiedDateTime { get; set; }

    [JsonPropertyName("lastSyncedAt")]
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfileListModel
{
    [JsonPropertyName("items")]
    public List<CompanyProfileListItemModel> Items { get; set; } = [];

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("searchTerm")]
    public string? SearchTerm { get; set; }
}

public sealed class CompanyProfileDetailModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("migratedId")]
    public long? MigratedId { get; set; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("registrationNo")]
    public string? RegistrationNo { get; set; }

    [JsonPropertyName("registrationDate")]
    public DateTime? RegistrationDate { get; set; }

    [JsonPropertyName("dateOfIncorporation")]
    public DateTime? DateOfIncorporation { get; set; }

    [JsonPropertyName("telephoneNo")]
    public string? TelephoneNo { get; set; }

    [JsonPropertyName("faxNo")]
    public string? FaxNo { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("incomeTaxNo")]
    public string? IncomeTaxNo { get; set; }

    [JsonPropertyName("epfNo")]
    public string? EpfNo { get; set; }

    [JsonPropertyName("socsoNo")]
    public string? SocsoNo { get; set; }

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    [JsonPropertyName("companySignatureId")]
    public long? CompanySignatureId { get; set; }

    [JsonPropertyName("companyType")]
    public int? CompanyType { get; set; }

    [JsonPropertyName("isCompanyCertified")]
    public bool? IsCompanyCertified { get; set; }

    [JsonPropertyName("companyApprovalStatus")]
    public int? CompanyApprovalStatus { get; set; }

    [JsonPropertyName("isPaid")]
    public bool? IsPaid { get; set; }

    [JsonPropertyName("isCompanyLocal")]
    public bool? IsCompanyLocal { get; set; }

    [JsonPropertyName("createdBySourceUserId")]
    public int? CreatedBySourceUserId { get; set; }

    [JsonPropertyName("sourceCreatedDateTime")]
    public DateTime? SourceCreatedDateTime { get; set; }

    [JsonPropertyName("modifiedBySourceUserId")]
    public int? ModifiedBySourceUserId { get; set; }

    [JsonPropertyName("sourceModifiedDateTime")]
    public DateTime? SourceModifiedDateTime { get; set; }

    [JsonPropertyName("addressId")]
    public Guid? AddressId { get; set; }

    [JsonPropertyName("legacyAddressId")]
    public long? LegacyAddressId { get; set; }

    [JsonPropertyName("backgroundDescription1")]
    public string? BackgroundDescription1 { get; set; }

    [JsonPropertyName("newSsmCompanyRegNo")]
    public string? NewSsmCompanyRegNo { get; set; }

    [JsonPropertyName("companyStatusId")]
    public int? CompanyStatusId { get; set; }

    [JsonPropertyName("totalEmployment")]
    public int? TotalEmployment { get; set; }

    [JsonPropertyName("annualClosingDateDay")]
    public int? AnnualClosingDateDay { get; set; }

    [JsonPropertyName("annualClosingDateMonth")]
    public int? AnnualClosingDateMonth { get; set; }

    [JsonPropertyName("aprNo")]
    public string? AprNo { get; set; }

    [JsonPropertyName("nonCode")]
    public string? NonCode { get; set; }

    [JsonPropertyName("lastSyncedAt")]
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfileIrpmAddressModel
{
    [JsonPropertyName("addressId")]
    public Guid? AddressId { get; set; }

    [JsonPropertyName("legacyAddressId")]
    public long? LegacyAddressId { get; set; }

    [JsonPropertyName("addressLine1")]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("addressLine3")]
    public string? AddressLine3 { get; set; }

    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public sealed class CompanyProfileIrpmDirectorModel
{
    [JsonPropertyName("migratedId")]
    public long MigratedId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("designation")]
    public string? Designation { get; set; }

    [JsonPropertyName("nationality")]
    public string? Nationality { get; set; }

    [JsonPropertyName("sharePercentage")]
    public decimal? SharePercentage { get; set; }
}

public sealed class CompanyProfileIrpmContactModel
{
    [JsonPropertyName("applicationUserId")]
    public Guid ApplicationUserId { get; set; }

    [JsonPropertyName("legacyContactPersonId")]
    public long? LegacyContactPersonId { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("designation")]
    public string? Designation { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }
}

public sealed class CompanyProfileIrpmAuthorizedPersonModel
{
    [JsonPropertyName("migratedId")]
    public long MigratedId { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("designation")]
    public string? Designation { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("telephoneNo")]
    public string? TelephoneNo { get; set; }

    [JsonPropertyName("identityNumber")]
    public string? IdentityNumber { get; set; }

    [JsonPropertyName("isCertified")]
    public bool? IsCertified { get; set; }

    [JsonPropertyName("isPinVerified")]
    public bool? IsPinVerified { get; set; }

    [JsonPropertyName("isDigiCertPaid")]
    public bool? IsDigiCertPaid { get; set; }

    [JsonPropertyName("isDeletedInSource")]
    public bool IsDeletedInSource { get; set; }
}

public sealed class CompanyProfileIrpmDocumentModel
{
    [JsonPropertyName("migratedId")]
    public long MigratedId { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileType")]
    public string? FileType { get; set; }

    [JsonPropertyName("hasFileContent")]
    public bool HasFileContent { get; set; }

    [JsonPropertyName("sourceCreatedAt")]
    public DateTime? SourceCreatedAt { get; set; }

    [JsonPropertyName("lastSyncedAt")]
    public DateTime LastSyncedAt { get; set; }
}

public sealed class CompanyProfileIrpmModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("migratedId")]
    public long? MigratedId { get; set; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("registrationNo")]
    public string? RegistrationNo { get; set; }

    [JsonPropertyName("registrationDate")]
    public DateTime? RegistrationDate { get; set; }

    [JsonPropertyName("dateOfIncorporation")]
    public DateTime? DateOfIncorporation { get; set; }

    [JsonPropertyName("telephoneNo")]
    public string? TelephoneNo { get; set; }

    [JsonPropertyName("faxNo")]
    public string? FaxNo { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("incomeTaxNo")]
    public string? IncomeTaxNo { get; set; }

    [JsonPropertyName("epfNo")]
    public string? EpfNo { get; set; }

    [JsonPropertyName("socsoNo")]
    public string? SocsoNo { get; set; }

    [JsonPropertyName("companySignatureId")]
    public long? CompanySignatureId { get; set; }

    [JsonPropertyName("companyType")]
    public int? CompanyType { get; set; }

    [JsonPropertyName("isCompanyCertified")]
    public bool? IsCompanyCertified { get; set; }

    [JsonPropertyName("companyApprovalStatus")]
    public int? CompanyApprovalStatus { get; set; }

    [JsonPropertyName("isPaid")]
    public bool? IsPaid { get; set; }

    [JsonPropertyName("isCompanyLocal")]
    public bool? IsCompanyLocal { get; set; }

    [JsonPropertyName("sourceCreatedDateTime")]
    public DateTime? SourceCreatedDateTime { get; set; }

    [JsonPropertyName("sourceModifiedDateTime")]
    public DateTime? SourceModifiedDateTime { get; set; }

    [JsonPropertyName("backgroundDescription1")]
    public string? BackgroundDescription1 { get; set; }

    [JsonPropertyName("newSsmCompanyRegNo")]
    public string? NewSsmCompanyRegNo { get; set; }

    [JsonPropertyName("companyStatusId")]
    public int? CompanyStatusId { get; set; }

    [JsonPropertyName("totalEmployment")]
    public int? TotalEmployment { get; set; }

    [JsonPropertyName("annualClosingDateDay")]
    public int? AnnualClosingDateDay { get; set; }

    [JsonPropertyName("annualClosingDateMonth")]
    public int? AnnualClosingDateMonth { get; set; }

    [JsonPropertyName("aprNo")]
    public string? AprNo { get; set; }

    [JsonPropertyName("nonCode")]
    public string? NonCode { get; set; }

    [JsonPropertyName("lastSyncedAt")]
    public DateTime LastSyncedAt { get; set; }

    [JsonPropertyName("address")]
    public CompanyProfileIrpmAddressModel? Address { get; set; }

    [JsonPropertyName("directors")]
    public List<CompanyProfileIrpmDirectorModel> Directors { get; set; } = [];

    [JsonPropertyName("contactPersons")]
    public List<CompanyProfileIrpmContactModel> ContactPersons { get; set; } = [];

    [JsonPropertyName("authorizedPersons")]
    public List<CompanyProfileIrpmAuthorizedPersonModel> AuthorizedPersons { get; set; } = [];

    [JsonPropertyName("documents")]
    public List<CompanyProfileIrpmDocumentModel> Documents { get; set; } = [];
}

public sealed class CompanyProfileFinancialDetailsSummaryModel
{
    [JsonPropertyName("financialDetailsId")]
    public Guid FinancialDetailsId { get; set; }

    [JsonPropertyName("migratedId")]
    public long MigratedId { get; set; }

    [JsonPropertyName("legacyProjectId")]
    public long? LegacyProjectId { get; set; }

    [JsonPropertyName("financialYear")]
    public int? FinancialYear { get; set; }

    [JsonPropertyName("effectiveDate")]
    public DateTime? EffectiveDate { get; set; }

    [JsonPropertyName("projectStatusId")]
    public int? ProjectStatusId { get; set; }

    [JsonPropertyName("lastSyncedAt")]
    public DateTime LastSyncedAt { get; set; }
}

public sealed class ApplicantApplicationListItemModel
{
    [JsonPropertyName("companyId")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("registrationNo")]
    public string? RegistrationNo { get; set; }

    [JsonPropertyName("financialDetailsId")]
    public Guid FinancialDetailsId { get; set; }

    [JsonPropertyName("migratedId")]
    public long MigratedId { get; set; }

    [JsonPropertyName("legacyProjectId")]
    public long? LegacyProjectId { get; set; }

    [JsonPropertyName("financialYear")]
    public int? FinancialYear { get; set; }

    [JsonPropertyName("effectiveDate")]
    public DateTime? EffectiveDate { get; set; }

    [JsonPropertyName("projectStatusId")]
    public int? ProjectStatusId { get; set; }

    [JsonPropertyName("lastSyncedAt")]
    public DateTime LastSyncedAt { get; set; }
}

public sealed class ApplicantApplicationListModel
{
    [JsonPropertyName("items")]
    public List<ApplicantApplicationListItemModel> Items { get; set; } = [];

    [JsonPropertyName("totalCompanies")]
    public int TotalCompanies { get; set; }

    [JsonPropertyName("totalApplications")]
    public int TotalApplications { get; set; }

    [JsonPropertyName("latestEffectiveDate")]
    public DateTime? LatestEffectiveDate { get; set; }
}

public sealed class ApplicantApplicationTemplateFieldOptionModel
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }
}

public sealed class ApplicantApplicationTemplateFieldModel
{
    [JsonPropertyName("fieldId")]
    public Guid FieldId { get; set; }

    [JsonPropertyName("fieldCode")]
    public string FieldCode { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("fieldTypeCode")]
    public string FieldTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; }

    [JsonPropertyName("helpText")]
    public string? HelpText { get; set; }

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("options")]
    public List<ApplicantApplicationTemplateFieldOptionModel> Options { get; set; } = [];
}

public sealed class ApplicantApplicationTemplateFormModel
{
    [JsonPropertyName("formDefinitionVersionId")]
    public Guid FormDefinitionVersionId { get; set; }

    [JsonPropertyName("formCode")]
    public string FormCode { get; set; } = string.Empty;

    [JsonPropertyName("formName")]
    public string FormName { get; set; } = string.Empty;

    [JsonPropertyName("versionNumber")]
    public int VersionNumber { get; set; }

    [JsonPropertyName("fields")]
    public List<ApplicantApplicationTemplateFieldModel> Fields { get; set; } = [];
}

public sealed class ApplicantApplicationTemplateSectionModel
{
    [JsonPropertyName("sectionCode")]
    public string SectionCode { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("sectionTypeCode")]
    public string SectionTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("systemRouteKey")]
    public string? SystemRouteKey { get; set; }

    [JsonPropertyName("systemComponentKey")]
    public string? SystemComponentKey { get; set; }

    [JsonPropertyName("stepIcon")]
    public string? StepIcon { get; set; }

    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; }

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("validationMode")]
    public string ValidationMode { get; set; } = string.Empty;

    [JsonPropertyName("form")]
    public ApplicantApplicationTemplateFormModel? Form { get; set; }
}

public sealed class ApplicantApplicationTemplateModel
{
    [JsonPropertyName("applicationCode")]
    public string ApplicationCode { get; set; } = string.Empty;

    [JsonPropertyName("templateCode")]
    public string TemplateCode { get; set; } = string.Empty;

    [JsonPropertyName("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("sections")]
    public List<ApplicantApplicationTemplateSectionModel> Sections { get; set; } = [];
}

public sealed class ApplicantApplicationStartupOptionModel
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("isSelected")]
    public bool IsSelected { get; set; }

    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }
}

public sealed class ApplicantApplicationStartupOptionGroupModel
{
    [JsonPropertyName("groupCode")]
    public string GroupCode { get; set; } = string.Empty;

    [JsonPropertyName("groupLabel")]
    public string GroupLabel { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public List<ApplicantApplicationStartupOptionModel> Options { get; set; } = [];
}

public sealed class ApplicantApplicationStartupModel
{
    [JsonPropertyName("applicationCode")]
    public string ApplicationCode { get; set; } = string.Empty;

    [JsonPropertyName("templateCode")]
    public string TemplateCode { get; set; } = string.Empty;

    [JsonPropertyName("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("applicationOptions")]
    public List<ApplicantApplicationStartupOptionModel> ApplicationOptions { get; set; } = [];

    [JsonPropertyName("sectorOptions")]
    public List<ApplicantApplicationStartupOptionModel> SectorOptions { get; set; } = [];

    [JsonPropertyName("exemptionTypeOptions")]
    public List<ApplicantApplicationStartupOptionModel> ExemptionTypeOptions { get; set; } = [];

    [JsonPropertyName("applicationTypeGroups")]
    public List<ApplicantApplicationStartupOptionGroupModel> ApplicationTypeGroups { get; set; } = [];

    [JsonPropertyName("marketOptions")]
    public List<ApplicantApplicationStartupOptionModel> MarketOptions { get; set; } = [];

    [JsonPropertyName("mainIndustryOptions")]
    public List<ApplicantApplicationStartupOptionModel> MainIndustryOptions { get; set; } = [];

    [JsonPropertyName("mainIndustryOptionsBySector")]
    public Dictionary<string, List<ApplicantApplicationStartupOptionModel>> MainIndustryOptionsBySector { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ApplicantApplicationLaunchSnapshotModel
{
    public string ApplicationLabel { get; set; } = string.Empty;

    public string SectorLabel { get; set; } = string.Empty;

    public string ExemptionSummary { get; set; } = string.Empty;

    public string ApplicationTypeLabel { get; set; } = string.Empty;

    public string MarketSummary { get; set; } = string.Empty;

    public string MainIndustryLabel { get; set; } = string.Empty;
}

public sealed class CreateApplicantApplicationRequestModel
{
    [JsonPropertyName("applicationCode")]
    public string ApplicationCode { get; set; } = string.Empty;

    [JsonPropertyName("selectedApplicationValue")]
    public string SelectedApplicationValue { get; set; } = string.Empty;

    [JsonPropertyName("sectorValue")]
    public string? SectorValue { get; set; }

    [JsonPropertyName("exemptionTypeValues")]
    public List<string> ExemptionTypeValues { get; set; } = [];

    [JsonPropertyName("applicationTypeSelections")]
    public Dictionary<string, string> ApplicationTypeSelections { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("marketValues")]
    public List<string> MarketValues { get; set; } = [];

    [JsonPropertyName("mainIndustryValue")]
    public string? MainIndustryValue { get; set; }
}

public sealed class CreateApplicantApplicationResponseModel
{
    [JsonPropertyName("applicantApplicationId")]
    public Guid ApplicantApplicationId { get; set; }

    [JsonPropertyName("applicationNo")]
    public string ApplicationNo { get; set; } = string.Empty;

    [JsonPropertyName("applicationCode")]
    public string ApplicationCode { get; set; } = string.Empty;

    [JsonPropertyName("templateCode")]
    public string TemplateCode { get; set; } = string.Empty;

    [JsonPropertyName("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    [JsonPropertyName("currentSectionCode")]
    public string? CurrentSectionCode { get; set; }
}

public sealed class ApplicantApplicationCompanyOptionModel
{
    [JsonPropertyName("companyProfileId")]
    public Guid CompanyProfileId { get; set; }

    [JsonPropertyName("legacyCompanyId")]
    public long? LegacyCompanyId { get; set; }

    [JsonPropertyName("companyName")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("registrationNo")]
    public string? RegistrationNo { get; set; }

    [JsonPropertyName("isSelected")]
    public bool IsSelected { get; set; }
}

public sealed class ApplicantApplicationCompanyDirectorModel
{
    [JsonPropertyName("directorName")]
    public string? DirectorName { get; set; }

    [JsonPropertyName("nationality")]
    public string? Nationality { get; set; }

    [JsonPropertyName("sharesHeldPercent")]
    public decimal? SharesHeldPercent { get; set; }
}

public sealed class ApplicantApplicationCompanyContactPersonModel
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("designation")]
    public string? Designation { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phoneNo")]
    public string? PhoneNo { get; set; }
}

public sealed class ApplicantApplicationCompanyProfileModel
{
    [JsonPropertyName("applicationCompanyProfileId")]
    public Guid ApplicationCompanyProfileId { get; set; }

    [JsonPropertyName("companyProfileId")]
    public Guid CompanyProfileId { get; set; }

    [JsonPropertyName("legacyParticularOfCompanyId")]
    public long? LegacyParticularOfCompanyId { get; set; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("registrationNumber")]
    public string? RegistrationNumber { get; set; }

    [JsonPropertyName("registrationTypeLabel")]
    public string? RegistrationTypeLabel { get; set; }

    [JsonPropertyName("dateOfIncorporation")]
    public DateTime? DateOfIncorporation { get; set; }

    [JsonPropertyName("registrationDate")]
    public DateTime? RegistrationDate { get; set; }

    [JsonPropertyName("telephoneNumber")]
    public string? TelephoneNumber { get; set; }

    [JsonPropertyName("faxNumber")]
    public string? FaxNumber { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("incomeTaxNo")]
    public string? IncomeTaxNo { get; set; }

    [JsonPropertyName("epfNo")]
    public string? EpfNo { get; set; }

    [JsonPropertyName("socsoNo")]
    public string? SocsoNo { get; set; }

    [JsonPropertyName("totalEmployment")]
    public int? TotalEmployment { get; set; }

    [JsonPropertyName("companyBackground")]
    public string? CompanyBackground { get; set; }

    [JsonPropertyName("registeredAddress1")]
    public string? RegisteredAddress1 { get; set; }

    [JsonPropertyName("registeredAddress2")]
    public string? RegisteredAddress2 { get; set; }

    [JsonPropertyName("registeredAddress3")]
    public string? RegisteredAddress3 { get; set; }

    [JsonPropertyName("registeredCountryName")]
    public string? RegisteredCountryName { get; set; }

    [JsonPropertyName("registeredStateName")]
    public string? RegisteredStateName { get; set; }

    [JsonPropertyName("registeredCityName")]
    public string? RegisteredCityName { get; set; }

    [JsonPropertyName("registeredPostcode")]
    public string? RegisteredPostcode { get; set; }

    [JsonPropertyName("isCorrespondenceSameAsRegistered")]
    public bool IsCorrespondenceSameAsRegistered { get; set; }

    [JsonPropertyName("correspondenceAddress1")]
    public string? CorrespondenceAddress1 { get; set; }

    [JsonPropertyName("correspondenceAddress2")]
    public string? CorrespondenceAddress2 { get; set; }

    [JsonPropertyName("correspondenceAddress3")]
    public string? CorrespondenceAddress3 { get; set; }

    [JsonPropertyName("correspondenceCountryName")]
    public string? CorrespondenceCountryName { get; set; }

    [JsonPropertyName("correspondenceStateName")]
    public string? CorrespondenceStateName { get; set; }

    [JsonPropertyName("correspondenceCityName")]
    public string? CorrespondenceCityName { get; set; }

    [JsonPropertyName("correspondencePostcode")]
    public string? CorrespondencePostcode { get; set; }

    [JsonPropertyName("customsControlStationName")]
    public string? CustomsControlStationName { get; set; }

    [JsonPropertyName("sourcePulledAt")]
    public DateTime? SourcePulledAt { get; set; }

    [JsonPropertyName("directors")]
    public List<ApplicantApplicationCompanyDirectorModel> Directors { get; set; } = [];

    [JsonPropertyName("contactPersons")]
    public List<ApplicantApplicationCompanyContactPersonModel> ContactPersons { get; set; } = [];
}

public sealed class ApplicantApplicationCompanySectionModel
{
    [JsonPropertyName("applicantApplicationId")]
    public Guid ApplicantApplicationId { get; set; }

    [JsonPropertyName("availableCompanies")]
    public List<ApplicantApplicationCompanyOptionModel> AvailableCompanies { get; set; } = [];

    [JsonPropertyName("profile")]
    public ApplicantApplicationCompanyProfileModel? Profile { get; set; }
}

public sealed class SelectApplicantApplicationCompanyRequestModel
{
    [JsonPropertyName("companyProfileId")]
    public Guid CompanyProfileId { get; set; }
}

public sealed class CompanyProfileFinancialAmountModel
{
    [JsonPropertyName("amountRm")]
    public decimal? AmountRm { get; set; }

    [JsonPropertyName("amountPercent")]
    public decimal? AmountPercent { get; set; }
}

public sealed class CompanyProfileFinancialNamedAmountModel
{
    [JsonPropertyName("migratedId")]
    public long? MigratedId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("amountRm")]
    public decimal? AmountRm { get; set; }

    [JsonPropertyName("amountPercent")]
    public decimal? AmountPercent { get; set; }
}

public sealed class CompanyProfileFinancialCompanyIncorporatedCountryModel
{
    [JsonPropertyName("migratedId")]
    public long MigratedId { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("countryPercent")]
    public decimal? CountryPercent { get; set; }

    [JsonPropertyName("amountRm")]
    public decimal? AmountRm { get; set; }

    [JsonPropertyName("percentOverTotal")]
    public decimal? PercentOverTotal { get; set; }
}

public sealed class CompanyProfileFinancialCompanyIncorporatedModel
{
    [JsonPropertyName("migratedId")]
    public long MigratedId { get; set; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("localCompanyTypeId")]
    public int? LocalCompanyTypeId { get; set; }

    [JsonPropertyName("bumiPercent")]
    public decimal? BumiPercent { get; set; }

    [JsonPropertyName("nonBumiPercent")]
    public decimal? NonBumiPercent { get; set; }

    [JsonPropertyName("foreignCountry")]
    public string? ForeignCountry { get; set; }

    [JsonPropertyName("foreignPercent")]
    public decimal? ForeignPercent { get; set; }

    [JsonPropertyName("totalPercent")]
    public decimal? TotalPercent { get; set; }

    [JsonPropertyName("countries")]
    public List<CompanyProfileFinancialCompanyIncorporatedCountryModel> Countries { get; set; } = [];
}

public sealed class CompanyProfileFinancialUltimateParentModel
{
    [JsonPropertyName("migratedId")]
    public long MigratedId { get; set; }

    [JsonPropertyName("foreignCompanyName")]
    public string? ForeignCompanyName { get; set; }

    [JsonPropertyName("ultimateCompany")]
    public string? UltimateCompany { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public sealed class CompanyProfileFinancialSelectedDetailModel
{
    [JsonPropertyName("financialDetailsId")]
    public Guid FinancialDetailsId { get; set; }

    [JsonPropertyName("migratedId")]
    public long MigratedId { get; set; }

    [JsonPropertyName("legacyProjectId")]
    public long? LegacyProjectId { get; set; }

    [JsonPropertyName("financialYear")]
    public int? FinancialYear { get; set; }

    [JsonPropertyName("effectiveDate")]
    public DateTime? EffectiveDate { get; set; }

    [JsonPropertyName("projectStatusId")]
    public int? ProjectStatusId { get; set; }

    [JsonPropertyName("authorizedCapital")]
    public decimal? AuthorizedCapital { get; set; }

    [JsonPropertyName("totalPaidUpCapital")]
    public decimal? TotalPaidUpCapital { get; set; }

    [JsonPropertyName("totalReserves")]
    public decimal? TotalReserves { get; set; }

    [JsonPropertyName("totalShareholderFund")]
    public decimal? TotalShareholderFund { get; set; }

    [JsonPropertyName("totalRmMalaysianIndividuals")]
    public decimal? TotalRmMalaysianIndividuals { get; set; }

    [JsonPropertyName("totalPercentMalaysianIndividuals")]
    public decimal? TotalPercentMalaysianIndividuals { get; set; }

    [JsonPropertyName("totalRmForeignCompany")]
    public decimal? TotalRmForeignCompany { get; set; }

    [JsonPropertyName("totalPercentForeignCompany")]
    public decimal? TotalPercentForeignCompany { get; set; }

    [JsonPropertyName("totalRmCompanyMalaysia")]
    public decimal? TotalRmCompanyMalaysia { get; set; }

    [JsonPropertyName("totalPercentCompanyMalaysia")]
    public decimal? TotalPercentCompanyMalaysia { get; set; }

    [JsonPropertyName("malaysianBumiputeraRm")]
    public decimal? MalaysianBumiputeraRm { get; set; }

    [JsonPropertyName("malaysianBumiputeraPercent")]
    public decimal? MalaysianBumiputeraPercent { get; set; }

    [JsonPropertyName("malaysianNonBumiputeraRm")]
    public decimal? MalaysianNonBumiputeraRm { get; set; }

    [JsonPropertyName("malaysianNonBumiputeraPercent")]
    public decimal? MalaysianNonBumiputeraPercent { get; set; }

    [JsonPropertyName("equityBumiRm")]
    public decimal? EquityBumiRm { get; set; }

    [JsonPropertyName("equityBumiPercent")]
    public decimal? EquityBumiPercent { get; set; }

    [JsonPropertyName("equityNonBumiRm")]
    public decimal? EquityNonBumiRm { get; set; }

    [JsonPropertyName("equityNonBumiPercent")]
    public decimal? EquityNonBumiPercent { get; set; }

    [JsonPropertyName("equityForeignRm")]
    public decimal? EquityForeignRm { get; set; }

    [JsonPropertyName("equityForeignPercent")]
    public decimal? EquityForeignPercent { get; set; }

    [JsonPropertyName("equityTotalRm")]
    public decimal? EquityTotalRm { get; set; }

    [JsonPropertyName("equityTotalPercent")]
    public decimal? EquityTotalPercent { get; set; }

    [JsonPropertyName("revenue")]
    public decimal? Revenue { get; set; }

    [JsonPropertyName("profit")]
    public decimal? Profit { get; set; }

    [JsonPropertyName("taxableExpenditure")]
    public decimal? TaxableExpenditure { get; set; }

    [JsonPropertyName("exportSales")]
    public decimal? ExportSales { get; set; }

    [JsonPropertyName("localSales")]
    public decimal? LocalSales { get; set; }

    [JsonPropertyName("totalAsset")]
    public decimal? TotalAsset { get; set; }

    [JsonPropertyName("shareholderFund")]
    public decimal? ShareholderFund { get; set; }

    [JsonPropertyName("employeeCount")]
    public int? EmployeeCount { get; set; }

    [JsonPropertyName("totalRmLoan")]
    public decimal? TotalRmLoan { get; set; }

    [JsonPropertyName("totalLoanPercent")]
    public decimal? TotalLoanPercent { get; set; }

    [JsonPropertyName("domesticDescription")]
    public string? DomesticDescription { get; set; }

    [JsonPropertyName("foreignDescription")]
    public string? ForeignDescription { get; set; }

    [JsonPropertyName("totalFinancingPaidUpCapital")]
    public decimal? TotalFinancingPaidUpCapital { get; set; }

    [JsonPropertyName("totalFinancingReserve")]
    public decimal? TotalFinancingReserve { get; set; }

    [JsonPropertyName("totalFinancingLoan")]
    public decimal? TotalFinancingLoan { get; set; }

    [JsonPropertyName("totalFinancingOtherSourcesRm")]
    public decimal? TotalFinancingOtherSourcesRm { get; set; }

    [JsonPropertyName("totalFinancingOtherSourcesPercent")]
    public decimal? TotalFinancingOtherSourcesPercent { get; set; }

    [JsonPropertyName("totalFinancingRm")]
    public decimal? TotalFinancingRm { get; set; }

    [JsonPropertyName("foreignCompanies")]
    public List<CompanyProfileFinancialNamedAmountModel> ForeignCompanies { get; set; } = [];

    [JsonPropertyName("companiesMalaysia")]
    public List<CompanyProfileFinancialNamedAmountModel> CompaniesMalaysia { get; set; } = [];

    [JsonPropertyName("companyIncorporated")]
    public List<CompanyProfileFinancialCompanyIncorporatedModel> CompanyIncorporated { get; set; } = [];

    [JsonPropertyName("ultimateParents")]
    public List<CompanyProfileFinancialUltimateParentModel> UltimateParents { get; set; } = [];

    [JsonPropertyName("loanDomestics")]
    public List<CompanyProfileFinancialAmountModel> LoanDomestics { get; set; } = [];

    [JsonPropertyName("loanForeigns")]
    public List<CompanyProfileFinancialNamedAmountModel> LoanForeigns { get; set; } = [];

    [JsonPropertyName("otherSources")]
    public List<CompanyProfileFinancialNamedAmountModel> OtherSources { get; set; } = [];
}

public sealed class CompanyProfileFinancialDetailsModel
{
    [JsonPropertyName("companyId")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("companyMigratedId")]
    public long? CompanyMigratedId { get; set; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("registrationNo")]
    public string? RegistrationNo { get; set; }

    [JsonPropertyName("newSsmCompanyRegNo")]
    public string? NewSsmCompanyRegNo { get; set; }

    [JsonPropertyName("sector")]
    public string? Sector { get; set; }

    [JsonPropertyName("lastSyncedAt")]
    public DateTime LastSyncedAt { get; set; }

    [JsonPropertyName("availableStatements")]
    public List<CompanyProfileFinancialDetailsSummaryModel> AvailableStatements { get; set; } = [];

    [JsonPropertyName("selectedStatement")]
    public CompanyProfileFinancialSelectedDetailModel? SelectedStatement { get; set; }
}

public sealed class CompanyProfileSyncStatusModel
{
    [JsonPropertyName("scheduleEnabled")]
    public bool ScheduleEnabled { get; set; }

    [JsonPropertyName("useLocalSynonym")]
    public bool UseLocalSynonym { get; set; }

    [JsonPropertyName("sourceConnectionStringName")]
    public string? SourceConnectionStringName { get; set; }

    [JsonPropertyName("sourceConnectionConfigured")]
    public bool SourceConnectionConfigured { get; set; }

    [JsonPropertyName("sourceObjectName")]
    public string SourceObjectName { get; set; } = string.Empty;

    [JsonPropertyName("batchSize")]
    public int BatchSize { get; set; }

    [JsonPropertyName("scheduleMinutes")]
    public int ScheduleMinutes { get; set; }

    [JsonPropertyName("localRowCount")]
    public long LocalRowCount { get; set; }

    [JsonPropertyName("lastStartedAt")]
    public DateTimeOffset? LastStartedAt { get; set; }

    [JsonPropertyName("lastCompletedAt")]
    public DateTimeOffset? LastCompletedAt { get; set; }

    [JsonPropertyName("lastRunSucceeded")]
    public bool? LastRunSucceeded { get; set; }

    [JsonPropertyName("lastProcessedRows")]
    public int LastProcessedRows { get; set; }

    [JsonPropertyName("lastRunMessage")]
    public string? LastRunMessage { get; set; }

    [JsonPropertyName("lastSourceModifiedDateTime")]
    public DateTime? LastSourceModifiedDateTime { get; set; }

    [JsonPropertyName("lastSourceCompanyId")]
    public long? LastSourceCompanyId { get; set; }
}

public sealed class CompanyUserSyncRunRequestModel
{
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Source company ID must be greater than zero.")]
    public long? SourceCompanyId { get; set; }
}

public sealed class CompanyUserSyncStatusModel
{
    [JsonPropertyName("sourceConnectionStringName")]
    public string? SourceConnectionStringName { get; set; }

    [JsonPropertyName("sourceConnectionConfigured")]
    public bool SourceConnectionConfigured { get; set; }

    [JsonPropertyName("contactPersonSourceObjectName")]
    public string ContactPersonSourceObjectName { get; set; } = string.Empty;

    [JsonPropertyName("userSourceObjectName")]
    public string UserSourceObjectName { get; set; } = string.Empty;

    [JsonPropertyName("individualSourceObjectName")]
    public string IndividualSourceObjectName { get; set; } = string.Empty;

    [JsonPropertyName("groupSourceObjectName")]
    public string GroupSourceObjectName { get; set; } = string.Empty;

    [JsonPropertyName("roleSourceObjectName")]
    public string RoleSourceObjectName { get; set; } = string.Empty;

    [JsonPropertyName("batchSize")]
    public int BatchSize { get; set; }

    [JsonPropertyName("localRowCount")]
    public long LocalRowCount { get; set; }

    [JsonPropertyName("lastStartedAt")]
    public DateTimeOffset? LastStartedAt { get; set; }

    [JsonPropertyName("lastCompletedAt")]
    public DateTimeOffset? LastCompletedAt { get; set; }

    [JsonPropertyName("lastRunSucceeded")]
    public bool? LastRunSucceeded { get; set; }

    [JsonPropertyName("lastProcessedRows")]
    public int LastProcessedRows { get; set; }

    [JsonPropertyName("lastRunMessage")]
    public string? LastRunMessage { get; set; }

    [JsonPropertyName("lastSourceCompanyId")]
    public long? LastSourceCompanyId { get; set; }
}

public sealed class CompanyRelatedDataSyncRunRequestModel
{
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Source company ID must be greater than zero.")]
    public long? SourceCompanyId { get; set; }
}

public sealed class CompanyRelatedDataSyncStatusModel
{
    [JsonPropertyName("sourceConnectionStringName")]
    public string? SourceConnectionStringName { get; set; }

    [JsonPropertyName("sourceConnectionConfigured")]
    public bool SourceConnectionConfigured { get; set; }

    [JsonPropertyName("authorizedPersonSourceObjectName")]
    public string AuthorizedPersonSourceObjectName { get; set; } = string.Empty;

    [JsonPropertyName("boardDirectorSourceObjectName")]
    public string BoardDirectorSourceObjectName { get; set; } = string.Empty;

    [JsonPropertyName("attachmentDocumentSourceObjectName")]
    public string AttachmentDocumentSourceObjectName { get; set; } = string.Empty;

    [JsonPropertyName("batchSize")]
    public int BatchSize { get; set; }

    [JsonPropertyName("localRowCount")]
    public long LocalRowCount { get; set; }

    [JsonPropertyName("lastStartedAt")]
    public DateTimeOffset? LastStartedAt { get; set; }

    [JsonPropertyName("lastCompletedAt")]
    public DateTimeOffset? LastCompletedAt { get; set; }

    [JsonPropertyName("lastRunSucceeded")]
    public bool? LastRunSucceeded { get; set; }

    [JsonPropertyName("lastProcessedRows")]
    public int LastProcessedRows { get; set; }

    [JsonPropertyName("lastRunMessage")]
    public string? LastRunMessage { get; set; }

    [JsonPropertyName("lastSourceCompanyId")]
    public long? LastSourceCompanyId { get; set; }
}

public sealed class DownloadFilePayload
{
    public DownloadFilePayload(string fileName, string contentType, byte[] content)
    {
        FileName = fileName;
        ContentType = contentType;
        Content = content;
    }

    public string FileName { get; set; }
    public string ContentType { get; set; }
    public byte[] Content { get; set; }
}
