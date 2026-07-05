IF OBJECT_ID(N'dbo.ApplicationTemplateStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationTemplateStatuses
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationTemplateStatuses PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_ApplicationTemplateStatuses_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_ApplicationTemplateStatuses_IsActive DEFAULT (1),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationTemplateStatuses_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationTemplateStatuses_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_ApplicationTemplateStatuses_Code ON dbo.ApplicationTemplateStatuses (Code);
END;
GO

IF OBJECT_ID(N'dbo.FormDefinitionStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FormDefinitionStatuses
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormDefinitionStatuses PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_FormDefinitionStatuses_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_FormDefinitionStatuses_IsActive DEFAULT (1),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_FormDefinitionStatuses_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_FormDefinitionStatuses_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_FormDefinitionStatuses_Code ON dbo.FormDefinitionStatuses (Code);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationSectionTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationSectionTypes
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationSectionTypes PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_ApplicationSectionTypes_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_ApplicationSectionTypes_IsActive DEFAULT (1),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationSectionTypes_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationSectionTypes_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_ApplicationSectionTypes_Code ON dbo.ApplicationSectionTypes (Code);
END;
GO

IF OBJECT_ID(N'dbo.FormFieldTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FormFieldTypes
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormFieldTypes PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_FormFieldTypes_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_FormFieldTypes_IsActive DEFAULT (1),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_FormFieldTypes_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_FormFieldTypes_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_FormFieldTypes_Code ON dbo.FormFieldTypes (Code);
END;
GO

IF OBJECT_ID(N'dbo.FormLookupSources', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FormLookupSources
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormLookupSources PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        Code NVARCHAR(100) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        SourceType NVARCHAR(50) NOT NULL,
        SourceTableName NVARCHAR(256) NULL,
        SourceValueColumn NVARCHAR(128) NULL,
        SourceLabelColumn NVARCHAR(128) NULL,
        QuerySql NVARCHAR(MAX) NULL,
        ApiRoute NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_FormLookupSources_IsActive DEFAULT (1),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_FormLookupSources_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_FormLookupSources_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_FormLookupSources_Code ON dbo.FormLookupSources (Code);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationTemplates
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationTemplates PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        Code NVARCHAR(100) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        ApplicationCategoryId UNIQUEIDENTIFIER NULL,
        ApplicationForId UNIQUEIDENTIFIER NULL,
        ApplicationTypeId UNIQUEIDENTIFIER NULL,
        DefaultApplicationStatusId UNIQUEIDENTIFIER NULL,
        CurrentPublishedVersionId UNIQUEIDENTIFIER NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ApplicationTemplates_IsActive DEFAULT (1),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationTemplates_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationTemplates_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationTemplates_ApplicationCategories_ApplicationCategoryId
            FOREIGN KEY (ApplicationCategoryId) REFERENCES dbo.ApplicationCategories (Id),
        CONSTRAINT FK_ApplicationTemplates_ApplicationFors_ApplicationForId
            FOREIGN KEY (ApplicationForId) REFERENCES dbo.ApplicationFors (Id),
        CONSTRAINT FK_ApplicationTemplates_ApplicationTypes_ApplicationTypeId
            FOREIGN KEY (ApplicationTypeId) REFERENCES dbo.ApplicationTypes (Id),
        CONSTRAINT FK_ApplicationTemplates_ApplicationStatuses_DefaultApplicationStatusId
            FOREIGN KEY (DefaultApplicationStatusId) REFERENCES dbo.ApplicationStatuses (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicationTemplates_Code ON dbo.ApplicationTemplates (Code);
    CREATE INDEX IX_ApplicationTemplates_CategoryForType
        ON dbo.ApplicationTemplates (ApplicationCategoryId, ApplicationForId, ApplicationTypeId);
END;
GO

IF OBJECT_ID(N'dbo.FormDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FormDefinitions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormDefinitions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        Code NVARCHAR(100) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        FormPurpose NVARCHAR(50) NOT NULL CONSTRAINT DF_FormDefinitions_FormPurpose DEFAULT (N'Section'),
        IsSystem BIT NOT NULL CONSTRAINT DF_FormDefinitions_IsSystem DEFAULT (0),
        CurrentPublishedVersionId UNIQUEIDENTIFIER NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_FormDefinitions_IsActive DEFAULT (1),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_FormDefinitions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_FormDefinitions_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_FormDefinitions_Code ON dbo.FormDefinitions (Code);
END;
GO

IF OBJECT_ID(N'dbo.FormDefinitionVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FormDefinitionVersions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormDefinitionVersions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FormDefinitionId UNIQUEIDENTIFIER NOT NULL,
        VersionNumber INT NOT NULL,
        FormDefinitionStatusId UNIQUEIDENTIFIER NOT NULL,
        VersionName NVARCHAR(200) NULL,
        Description NVARCHAR(1000) NULL,
        LayoutJson NVARCHAR(MAX) NULL,
        ValidationJson NVARCHAR(MAX) NULL,
        EffectiveFrom DATETIME2(3) NULL,
        EffectiveTo DATETIME2(3) NULL,
        PublishedAt DATETIME2(3) NULL,
        PublishedBy NVARCHAR(450) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_FormDefinitionVersions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_FormDefinitionVersions_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_FormDefinitionVersions_FormDefinitions_FormDefinitionId
            FOREIGN KEY (FormDefinitionId) REFERENCES dbo.FormDefinitions (Id),
        CONSTRAINT FK_FormDefinitionVersions_FormDefinitionStatuses_FormDefinitionStatusId
            FOREIGN KEY (FormDefinitionStatusId) REFERENCES dbo.FormDefinitionStatuses (Id)
    );

    CREATE UNIQUE INDEX IX_FormDefinitionVersions_FormDefinitionId_VersionNumber
        ON dbo.FormDefinitionVersions (FormDefinitionId, VersionNumber);
    CREATE INDEX IX_FormDefinitionVersions_Status
        ON dbo.FormDefinitionVersions (FormDefinitionStatusId, PublishedAt);
END;
GO

IF OBJECT_ID(N'dbo.FormFieldDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FormFieldDefinitions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormFieldDefinitions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FormDefinitionVersionId UNIQUEIDENTIFIER NOT NULL,
        ParentFieldDefinitionId UNIQUEIDENTIFIER NULL,
        FieldCode NVARCHAR(100) NOT NULL,
        Label NVARCHAR(300) NOT NULL,
        Placeholder NVARCHAR(300) NULL,
        HelpText NVARCHAR(1000) NULL,
        FormFieldTypeId UNIQUEIDENTIFIER NOT NULL,
        LookupSourceId UNIQUEIDENTIFIER NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_FormFieldDefinitions_DisplayOrder DEFAULT (0),
        GroupName NVARCHAR(100) NULL,
        IsRequired BIT NOT NULL CONSTRAINT DF_FormFieldDefinitions_IsRequired DEFAULT (0),
        IsReadOnly BIT NOT NULL CONSTRAINT DF_FormFieldDefinitions_IsReadOnly DEFAULT (0),
        IsRepeatable BIT NOT NULL CONSTRAINT DF_FormFieldDefinitions_IsRepeatable DEFAULT (0),
        DefaultValue NVARCHAR(MAX) NULL,
        ValidationJson NVARCHAR(MAX) NULL,
        VisibilityRuleJson NVARCHAR(MAX) NULL,
        FieldSettingsJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_FormFieldDefinitions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_FormFieldDefinitions_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_FormFieldDefinitions_FormDefinitionVersions_FormDefinitionVersionId
            FOREIGN KEY (FormDefinitionVersionId) REFERENCES dbo.FormDefinitionVersions (Id),
        CONSTRAINT FK_FormFieldDefinitions_FormFieldDefinitions_ParentFieldDefinitionId
            FOREIGN KEY (ParentFieldDefinitionId) REFERENCES dbo.FormFieldDefinitions (Id),
        CONSTRAINT FK_FormFieldDefinitions_FormFieldTypes_FormFieldTypeId
            FOREIGN KEY (FormFieldTypeId) REFERENCES dbo.FormFieldTypes (Id),
        CONSTRAINT FK_FormFieldDefinitions_FormLookupSources_LookupSourceId
            FOREIGN KEY (LookupSourceId) REFERENCES dbo.FormLookupSources (Id)
    );

    CREATE UNIQUE INDEX IX_FormFieldDefinitions_FormDefinitionVersionId_FieldCode
        ON dbo.FormFieldDefinitions (FormDefinitionVersionId, FieldCode);
    CREATE INDEX IX_FormFieldDefinitions_FormDefinitionVersionId_DisplayOrder
        ON dbo.FormFieldDefinitions (FormDefinitionVersionId, DisplayOrder);
END;
GO

IF OBJECT_ID(N'dbo.FormFieldOptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FormFieldOptions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormFieldOptions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FormFieldDefinitionId UNIQUEIDENTIFIER NOT NULL,
        OptionValue NVARCHAR(200) NOT NULL,
        OptionLabel NVARCHAR(300) NOT NULL,
        OptionGroup NVARCHAR(100) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_FormFieldOptions_DisplayOrder DEFAULT (0),
        IsDefault BIT NOT NULL CONSTRAINT DF_FormFieldOptions_IsDefault DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_FormFieldOptions_IsActive DEFAULT (1),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_FormFieldOptions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_FormFieldOptions_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_FormFieldOptions_FormFieldDefinitions_FormFieldDefinitionId
            FOREIGN KEY (FormFieldDefinitionId) REFERENCES dbo.FormFieldDefinitions (Id)
    );

    CREATE UNIQUE INDEX IX_FormFieldOptions_FormFieldDefinitionId_OptionValue
        ON dbo.FormFieldOptions (FormFieldDefinitionId, OptionValue);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationTemplateVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationTemplateVersions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationTemplateVersions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        ApplicationTemplateId UNIQUEIDENTIFIER NOT NULL,
        VersionNumber INT NOT NULL,
        ApplicationTemplateStatusId UNIQUEIDENTIFIER NOT NULL,
        VersionName NVARCHAR(200) NULL,
        Description NVARCHAR(1000) NULL,
        EffectiveFrom DATETIME2(3) NULL,
        EffectiveTo DATETIME2(3) NULL,
        PublishedAt DATETIME2(3) NULL,
        PublishedBy NVARCHAR(450) NULL,
        SubmissionConfigJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationTemplateVersions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationTemplateVersions_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationTemplateVersions_ApplicationTemplates_ApplicationTemplateId
            FOREIGN KEY (ApplicationTemplateId) REFERENCES dbo.ApplicationTemplates (Id),
        CONSTRAINT FK_ApplicationTemplateVersions_ApplicationTemplateStatuses_ApplicationTemplateStatusId
            FOREIGN KEY (ApplicationTemplateStatusId) REFERENCES dbo.ApplicationTemplateStatuses (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicationTemplateVersions_ApplicationTemplateId_VersionNumber
        ON dbo.ApplicationTemplateVersions (ApplicationTemplateId, VersionNumber);
    CREATE INDEX IX_ApplicationTemplateVersions_Status
        ON dbo.ApplicationTemplateVersions (ApplicationTemplateStatusId, PublishedAt);
END;
GO

IF COL_LENGTH(N'dbo.ApplicationTemplates', N'CurrentPublishedVersionId') IS NOT NULL
    AND NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_ApplicationTemplates_ApplicationTemplateVersions_CurrentPublishedVersionId'
    )
BEGIN
    ALTER TABLE dbo.ApplicationTemplates
    ADD CONSTRAINT FK_ApplicationTemplates_ApplicationTemplateVersions_CurrentPublishedVersionId
        FOREIGN KEY (CurrentPublishedVersionId) REFERENCES dbo.ApplicationTemplateVersions (Id);
END;
GO

IF COL_LENGTH(N'dbo.FormDefinitions', N'CurrentPublishedVersionId') IS NOT NULL
    AND NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_FormDefinitions_FormDefinitionVersions_CurrentPublishedVersionId'
    )
BEGIN
    ALTER TABLE dbo.FormDefinitions
    ADD CONSTRAINT FK_FormDefinitions_FormDefinitionVersions_CurrentPublishedVersionId
        FOREIGN KEY (CurrentPublishedVersionId) REFERENCES dbo.FormDefinitionVersions (Id);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationTemplateSections', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationTemplateSections
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationTemplateSections PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        ApplicationTemplateVersionId UNIQUEIDENTIFIER NOT NULL,
        ParentSectionId UNIQUEIDENTIFIER NULL,
        SectionCode NVARCHAR(100) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_ApplicationTemplateSections_DisplayOrder DEFAULT (0),
        ApplicationSectionTypeId UNIQUEIDENTIFIER NOT NULL,
        SystemRouteKey NVARCHAR(200) NULL,
        SystemComponentKey NVARCHAR(200) NULL,
        FormDefinitionVersionId UNIQUEIDENTIFIER NULL,
        StepIcon NVARCHAR(100) NULL,
        IsVisible BIT NOT NULL CONSTRAINT DF_ApplicationTemplateSections_IsVisible DEFAULT (1),
        IsRequired BIT NOT NULL CONSTRAINT DF_ApplicationTemplateSections_IsRequired DEFAULT (0),
        CanRepeat BIT NOT NULL CONSTRAINT DF_ApplicationTemplateSections_CanRepeat DEFAULT (0),
        ValidationMode NVARCHAR(50) NOT NULL CONSTRAINT DF_ApplicationTemplateSections_ValidationMode DEFAULT (N'OnSubmit'),
        VisibilityRuleJson NVARCHAR(MAX) NULL,
        EditableRuleJson NVARCHAR(MAX) NULL,
        CompletionRuleJson NVARCHAR(MAX) NULL,
        SectionSettingsJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationTemplateSections_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationTemplateSections_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationTemplateSections_ApplicationTemplateVersions_ApplicationTemplateVersionId
            FOREIGN KEY (ApplicationTemplateVersionId) REFERENCES dbo.ApplicationTemplateVersions (Id),
        CONSTRAINT FK_ApplicationTemplateSections_ApplicationTemplateSections_ParentSectionId
            FOREIGN KEY (ParentSectionId) REFERENCES dbo.ApplicationTemplateSections (Id),
        CONSTRAINT FK_ApplicationTemplateSections_ApplicationSectionTypes_ApplicationSectionTypeId
            FOREIGN KEY (ApplicationSectionTypeId) REFERENCES dbo.ApplicationSectionTypes (Id),
        CONSTRAINT FK_ApplicationTemplateSections_FormDefinitionVersions_FormDefinitionVersionId
            FOREIGN KEY (FormDefinitionVersionId) REFERENCES dbo.FormDefinitionVersions (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicationTemplateSections_TemplateVersionId_SectionCode
        ON dbo.ApplicationTemplateSections (ApplicationTemplateVersionId, SectionCode);
    CREATE INDEX IX_ApplicationTemplateSections_TemplateVersionId_DisplayOrder
        ON dbo.ApplicationTemplateSections (ApplicationTemplateVersionId, DisplayOrder);
END;
GO

IF OBJECT_ID(N'dbo.ApplicantApplications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicantApplications
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicantApplications PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        ApplicationNo NVARCHAR(100) NULL,
        ApplicantUserId NVARCHAR(450) NULL,
        CompanyProfileId UNIQUEIDENTIFIER NULL,
        ApplicationTemplateId UNIQUEIDENTIFIER NOT NULL,
        ApplicationTemplateVersionId UNIQUEIDENTIFIER NOT NULL,
        ApplicationCategoryId UNIQUEIDENTIFIER NULL,
        ApplicationForId UNIQUEIDENTIFIER NULL,
        ApplicationTypeId UNIQUEIDENTIFIER NULL,
        ApplicationStatusId UNIQUEIDENTIFIER NULL,
        CurrentSectionId UNIQUEIDENTIFIER NULL,
        CurrentSectionCode NVARCHAR(100) NULL,
        StartupDataJson NVARCHAR(MAX) NULL,
        VersionSnapshotJson NVARCHAR(MAX) NULL,
        SubmittedAt DATETIME2(3) NULL,
        ApprovedAt DATETIME2(3) NULL,
        RejectedAt DATETIME2(3) NULL,
        LastSavedAt DATETIME2(3) NULL,
        LastSubmittedBy NVARCHAR(450) NULL,
        Remarks NVARCHAR(2000) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicantApplications_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicantApplications_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicantApplications_CompanyProfiles_CompanyProfileId
            FOREIGN KEY (CompanyProfileId) REFERENCES dbo.CompanyProfiles (Id),
        CONSTRAINT FK_ApplicantApplications_ApplicationTemplates_ApplicationTemplateId
            FOREIGN KEY (ApplicationTemplateId) REFERENCES dbo.ApplicationTemplates (Id),
        CONSTRAINT FK_ApplicantApplications_ApplicationTemplateVersions_ApplicationTemplateVersionId
            FOREIGN KEY (ApplicationTemplateVersionId) REFERENCES dbo.ApplicationTemplateVersions (Id),
        CONSTRAINT FK_ApplicantApplications_ApplicationCategories_ApplicationCategoryId
            FOREIGN KEY (ApplicationCategoryId) REFERENCES dbo.ApplicationCategories (Id),
        CONSTRAINT FK_ApplicantApplications_ApplicationFors_ApplicationForId
            FOREIGN KEY (ApplicationForId) REFERENCES dbo.ApplicationFors (Id),
        CONSTRAINT FK_ApplicantApplications_ApplicationTypes_ApplicationTypeId
            FOREIGN KEY (ApplicationTypeId) REFERENCES dbo.ApplicationTypes (Id),
        CONSTRAINT FK_ApplicantApplications_ApplicationStatuses_ApplicationStatusId
            FOREIGN KEY (ApplicationStatusId) REFERENCES dbo.ApplicationStatuses (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicantApplications_ApplicationNo
        ON dbo.ApplicantApplications (ApplicationNo)
        WHERE ApplicationNo IS NOT NULL;
    CREATE INDEX IX_ApplicantApplications_ApplicantUserId
        ON dbo.ApplicantApplications (ApplicantUserId, CreatedAt);
    CREATE INDEX IX_ApplicantApplications_CompanyProfileId
        ON dbo.ApplicantApplications (CompanyProfileId, CreatedAt);
    CREATE INDEX IX_ApplicantApplications_TemplateStatus
        ON dbo.ApplicantApplications (ApplicationTemplateId, ApplicationStatusId, SubmittedAt);
END;
GO

IF COL_LENGTH(N'dbo.ApplicantApplications', N'StartupDataJson') IS NULL
BEGIN
    ALTER TABLE dbo.ApplicantApplications
    ADD StartupDataJson NVARCHAR(MAX) NULL;
END;
GO

IF OBJECT_ID(N'dbo.ApplicantApplicationSections', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicantApplicationSections
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicantApplicationSections PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        ApplicantApplicationId UNIQUEIDENTIFIER NOT NULL,
        ApplicationTemplateSectionId UNIQUEIDENTIFIER NOT NULL,
        SectionCode NVARCHAR(100) NOT NULL,
        SectionTitle NVARCHAR(200) NOT NULL,
        FormDefinitionVersionId UNIQUEIDENTIFIER NULL,
        SectionStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_ApplicantApplicationSections_SectionStatus DEFAULT (N'NotStarted'),
        IsStarted BIT NOT NULL CONSTRAINT DF_ApplicantApplicationSections_IsStarted DEFAULT (0),
        IsCompleted BIT NOT NULL CONSTRAINT DF_ApplicantApplicationSections_IsCompleted DEFAULT (0),
        StartedAt DATETIME2(3) NULL,
        CompletedAt DATETIME2(3) NULL,
        LastSavedAt DATETIME2(3) NULL,
        RepeatIndex INT NOT NULL CONSTRAINT DF_ApplicantApplicationSections_RepeatIndex DEFAULT (1),
        SectionDataJson NVARCHAR(MAX) NULL,
        ValidationSummaryJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicantApplicationSections_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicantApplicationSections_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicantApplicationSections_ApplicantApplications_ApplicantApplicationId
            FOREIGN KEY (ApplicantApplicationId) REFERENCES dbo.ApplicantApplications (Id),
        CONSTRAINT FK_ApplicantApplicationSections_ApplicationTemplateSections_ApplicationTemplateSectionId
            FOREIGN KEY (ApplicationTemplateSectionId) REFERENCES dbo.ApplicationTemplateSections (Id),
        CONSTRAINT FK_ApplicantApplicationSections_FormDefinitionVersions_FormDefinitionVersionId
            FOREIGN KEY (FormDefinitionVersionId) REFERENCES dbo.FormDefinitionVersions (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicantApplicationSections_ApplicationId_SectionId_RepeatIndex
        ON dbo.ApplicantApplicationSections (ApplicantApplicationId, ApplicationTemplateSectionId, RepeatIndex);
    CREATE INDEX IX_ApplicantApplicationSections_ApplicationId_Status
        ON dbo.ApplicantApplicationSections (ApplicantApplicationId, SectionStatus, RepeatIndex);
END;
GO

IF OBJECT_ID(N'dbo.ApplicantApplicationFieldValues', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicantApplicationFieldValues
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicantApplicationFieldValues PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        ApplicantApplicationId UNIQUEIDENTIFIER NOT NULL,
        ApplicantApplicationSectionId UNIQUEIDENTIFIER NOT NULL,
        FormDefinitionVersionId UNIQUEIDENTIFIER NOT NULL,
        FormFieldDefinitionId UNIQUEIDENTIFIER NOT NULL,
        FieldCode NVARCHAR(100) NOT NULL,
        RepeatIndex INT NOT NULL CONSTRAINT DF_ApplicantApplicationFieldValues_RepeatIndex DEFAULT (1),
        ValueString NVARCHAR(MAX) NULL,
        ValueNumber DECIMAL(18, 6) NULL,
        ValueDate DATETIME2(3) NULL,
        ValueBit BIT NULL,
        ValueJson NVARCHAR(MAX) NULL,
        DisplayValue NVARCHAR(1000) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicantApplicationFieldValues_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicantApplicationFieldValues_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicantApplicationFieldValues_ApplicantApplications_ApplicantApplicationId
            FOREIGN KEY (ApplicantApplicationId) REFERENCES dbo.ApplicantApplications (Id),
        CONSTRAINT FK_ApplicantApplicationFieldValues_ApplicantApplicationSections_ApplicantApplicationSectionId
            FOREIGN KEY (ApplicantApplicationSectionId) REFERENCES dbo.ApplicantApplicationSections (Id),
        CONSTRAINT FK_ApplicantApplicationFieldValues_FormDefinitionVersions_FormDefinitionVersionId
            FOREIGN KEY (FormDefinitionVersionId) REFERENCES dbo.FormDefinitionVersions (Id),
        CONSTRAINT FK_ApplicantApplicationFieldValues_FormFieldDefinitions_FormFieldDefinitionId
            FOREIGN KEY (FormFieldDefinitionId) REFERENCES dbo.FormFieldDefinitions (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicantApplicationFieldValues_SectionId_FieldId_RepeatIndex
        ON dbo.ApplicantApplicationFieldValues (ApplicantApplicationSectionId, FormFieldDefinitionId, RepeatIndex);
    CREATE INDEX IX_ApplicantApplicationFieldValues_ApplicationId_FieldCode
        ON dbo.ApplicantApplicationFieldValues (ApplicantApplicationId, FieldCode);
END;
GO

IF OBJECT_ID(N'dbo.ApplicantApplicationAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicantApplicationAttachments
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicantApplicationAttachments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        ApplicantApplicationId UNIQUEIDENTIFIER NOT NULL,
        ApplicantApplicationSectionId UNIQUEIDENTIFIER NULL,
        AttachmentCategory NVARCHAR(100) NULL,
        FileName NVARCHAR(500) NOT NULL,
        ContentType NVARCHAR(200) NULL,
        StorageProvider NVARCHAR(100) NULL,
        StoragePath NVARCHAR(1000) NULL,
        BlobKey NVARCHAR(500) NULL,
        FileSizeBytes BIGINT NULL,
        FileHash NVARCHAR(256) NULL,
        IsRequiredDocument BIT NOT NULL CONSTRAINT DF_ApplicantApplicationAttachments_IsRequiredDocument DEFAULT (0),
        IsVerified BIT NOT NULL CONSTRAINT DF_ApplicantApplicationAttachments_IsVerified DEFAULT (0),
        UploadedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicantApplicationAttachments_UploadedAt DEFAULT SYSUTCDATETIME(),
        UploadedBy NVARCHAR(450) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicantApplicationAttachments_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicantApplicationAttachments_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicantApplicationAttachments_ApplicantApplications_ApplicantApplicationId
            FOREIGN KEY (ApplicantApplicationId) REFERENCES dbo.ApplicantApplications (Id),
        CONSTRAINT FK_ApplicantApplicationAttachments_ApplicantApplicationSections_ApplicantApplicationSectionId
            FOREIGN KEY (ApplicantApplicationSectionId) REFERENCES dbo.ApplicantApplicationSections (Id)
    );

    CREATE INDEX IX_ApplicantApplicationAttachments_ApplicationId
        ON dbo.ApplicantApplicationAttachments (ApplicantApplicationId, UploadedAt);
END;
GO

IF OBJECT_ID(N'dbo.ApplicantApplicationAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicantApplicationAuditLogs
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicantApplicationAuditLogs PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        ApplicantApplicationId UNIQUEIDENTIFIER NOT NULL,
        ApplicantApplicationSectionId UNIQUEIDENTIFIER NULL,
        EventType NVARCHAR(100) NOT NULL,
        EventAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicantApplicationAuditLogs_EventAt DEFAULT SYSUTCDATETIME(),
        EventBy NVARCHAR(450) NULL,
        Remarks NVARCHAR(2000) NULL,
        SnapshotJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicantApplicationAuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicantApplicationAuditLogs_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicantApplicationAuditLogs_ApplicantApplications_ApplicantApplicationId
            FOREIGN KEY (ApplicantApplicationId) REFERENCES dbo.ApplicantApplications (Id),
        CONSTRAINT FK_ApplicantApplicationAuditLogs_ApplicantApplicationSections_ApplicantApplicationSectionId
            FOREIGN KEY (ApplicantApplicationSectionId) REFERENCES dbo.ApplicantApplicationSections (Id)
    );

    CREATE INDEX IX_ApplicantApplicationAuditLogs_ApplicationId_EventAt
        ON dbo.ApplicantApplicationAuditLogs (ApplicantApplicationId, EventAt);
END;
GO

MERGE dbo.ApplicationTemplateStatuses AS target
USING
(
    VALUES
        (N'Draft', N'Draft', 10),
        (N'Published', N'Published', 20),
        (N'Retired', N'Retired', 30)
) AS source (Code, Name, SortOrder)
    ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET
        target.Name = source.Name,
        target.SortOrder = source.SortOrder,
        target.IsActive = 1,
        target.UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Code, Name, SortOrder)
    VALUES (source.Code, source.Name, source.SortOrder);
;
GO

MERGE dbo.FormDefinitionStatuses AS target
USING
(
    VALUES
        (N'Draft', N'Draft', 10),
        (N'Published', N'Published', 20),
        (N'Retired', N'Retired', 30)
) AS source (Code, Name, SortOrder)
    ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET
        target.Name = source.Name,
        target.SortOrder = source.SortOrder,
        target.IsActive = 1,
        target.UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Code, Name, SortOrder)
    VALUES (source.Code, source.Name, source.SortOrder);
;
GO

MERGE dbo.ApplicationSectionTypes AS target
USING
(
    VALUES
        (N'SystemForm', N'System Form', 10),
        (N'DynamicForm', N'Dynamic Form', 20),
        (N'DocumentUpload', N'Document Upload', 30),
        (N'ReadOnlySummary', N'Read Only Summary', 40),
        (N'ReviewSubmit', N'Review & Submit', 50)
) AS source (Code, Name, SortOrder)
    ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET
        target.Name = source.Name,
        target.SortOrder = source.SortOrder,
        target.IsActive = 1,
        target.UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Code, Name, SortOrder)
    VALUES (source.Code, source.Name, source.SortOrder);
;
GO

MERGE dbo.FormFieldTypes AS target
USING
(
    VALUES
        (N'Text', N'Text', 10),
        (N'TextArea', N'Text Area', 20),
        (N'Number', N'Number', 30),
        (N'Date', N'Date', 40),
        (N'Dropdown', N'Dropdown', 50),
        (N'Radio', N'Radio', 60),
        (N'Checkbox', N'Checkbox', 70),
        (N'File', N'File', 80),
        (N'Group', N'Group', 90),
        (N'Label', N'Label', 100)
) AS source (Code, Name, SortOrder)
    ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET
        target.Name = source.Name,
        target.SortOrder = source.SortOrder,
        target.IsActive = 1,
        target.UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Code, Name, SortOrder)
    VALUES (source.Code, source.Name, source.SortOrder);
;
GO
