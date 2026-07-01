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
