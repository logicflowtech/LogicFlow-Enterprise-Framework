namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class ApplicationCategory : BaseEntity
{
    public int LegacyId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? CodeKey { get; set; }
    public int? CategoryNumber { get; set; }
    public int? SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public ICollection<ApplicationFor> ApplicationFors { get; set; } = new List<ApplicationFor>();
    public ICollection<ApplicationCategoryFor> CategoryFors { get; set; } = new List<ApplicationCategoryFor>();
}

public sealed class ApplicationFor : BaseEntity
{
    public int LegacyId { get; set; }
    public int? LegacyApplicationCategoryId { get; set; }
    public Guid? ApplicationCategoryId { get; set; }
    public ApplicationCategory? ApplicationCategory { get; set; }
    public string? Name { get; set; }
    public string? NameBahasa { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public ICollection<ApplicationForType> ApplicationForTypes { get; set; } = new List<ApplicationForType>();
    public ICollection<ApplicationCategoryFor> CategoryFors { get; set; } = new List<ApplicationCategoryFor>();
}

public sealed class ApplicationType : BaseEntity
{
    public int LegacyId { get; set; }
    public string? Name { get; set; }
    public string? NameBahasa { get; set; }
    public int? SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public ICollection<ApplicationForType> ApplicationForTypes { get; set; } = new List<ApplicationForType>();
}

public sealed class ApplicationStatus : BaseEntity
{
    public int LegacyId { get; set; }
    public string? Name { get; set; }
    public string? CodeKey { get; set; }
    public int? SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int? LegacyMainTypeId { get; set; }
    public int? LegacyApplicantStatusId { get; set; }
    public int? LegacyCustomStatusId { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class ApplicationForType : BaseEntity
{
    public int LegacyId { get; set; }
    public Guid? ApplicationForId { get; set; }
    public ApplicationFor? ApplicationFor { get; set; }
    public Guid? ApplicationTypeId { get; set; }
    public ApplicationType? ApplicationType { get; set; }
    public int? LegacyApplicationForId { get; set; }
    public int? LegacyApplicationTypeId { get; set; }
    public int? LegacyApplicationForExemptionTypeId { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class ApplicationCategoryFor : BaseEntity
{
    public long LegacyId { get; set; }
    public Guid? ApplicationCategoryId { get; set; }
    public ApplicationCategory? ApplicationCategory { get; set; }
    public Guid? ApplicationForId { get; set; }
    public ApplicationFor? ApplicationFor { get; set; }
    public int? LegacyApplicationCategoryId { get; set; }
    public int? LegacyApplicationForId { get; set; }
    public int? SourceCreatedByLegacyUserId { get; set; }
    public DateTime? SourceCreatedAt { get; set; }
    public int? SourceUpdatedByLegacyUserId { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public sealed class ApplicationLookupSyncState
{
    public Guid Id { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string SyncName { get; set; } = string.Empty;
    public DateTime? LastStartedAt { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public bool? LastRunSucceeded { get; set; }
    public int LastProcessedRows { get; set; }
    public string? LastRunMessage { get; set; }
}
