IF OBJECT_ID(N'dbo.ApplicationCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationCategories
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationCategories PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId INT NOT NULL,
        Name NVARCHAR(200) NULL,
        Code NVARCHAR(100) NULL,
        CodeKey NVARCHAR(100) NULL,
        CategoryNumber INT NULL,
        SortOrder INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ApplicationCategories_IsActive DEFAULT (1),
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationCategories_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationCategories_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationCategories_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_ApplicationCategories_LegacyId ON dbo.ApplicationCategories (LegacyId);
    CREATE UNIQUE INDEX IX_ApplicationCategories_CodeKey ON dbo.ApplicationCategories (CodeKey) WHERE CodeKey IS NOT NULL;
    CREATE INDEX IX_ApplicationCategories_SortOrder ON dbo.ApplicationCategories (SortOrder, Name);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationTypes
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationTypes PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId INT NOT NULL,
        Name NVARCHAR(200) NULL,
        NameBahasa NVARCHAR(500) NULL,
        SortOrder INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ApplicationTypes_IsActive DEFAULT (1),
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationTypes_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationTypes_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationTypes_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_ApplicationTypes_LegacyId ON dbo.ApplicationTypes (LegacyId);
    CREATE INDEX IX_ApplicationTypes_SortOrder ON dbo.ApplicationTypes (SortOrder, Name);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationStatuses
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationStatuses PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId INT NOT NULL,
        Name NVARCHAR(200) NULL,
        CodeKey NVARCHAR(100) NULL,
        SortOrder INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ApplicationStatuses_IsActive DEFAULT (1),
        LegacyMainTypeId INT NULL,
        LegacyApplicantStatusId INT NULL,
        LegacyCustomStatusId INT NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationStatuses_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationStatuses_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationStatuses_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_ApplicationStatuses_LegacyId ON dbo.ApplicationStatuses (LegacyId);
    CREATE UNIQUE INDEX IX_ApplicationStatuses_CodeKey ON dbo.ApplicationStatuses (CodeKey) WHERE CodeKey IS NOT NULL;
    CREATE INDEX IX_ApplicationStatuses_SortOrder ON dbo.ApplicationStatuses (SortOrder, Name);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationFors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationFors
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationFors PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId INT NOT NULL,
        LegacyApplicationCategoryId INT NULL,
        ApplicationCategoryId UNIQUEIDENTIFIER NULL,
        Name NVARCHAR(500) NULL,
        NameBahasa NVARCHAR(500) NULL,
        Description NVARCHAR(1000) NULL,
        SortOrder INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ApplicationFors_IsActive DEFAULT (1),
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationFors_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationFors_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationFors_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationFors_ApplicationCategories_ApplicationCategoryId
            FOREIGN KEY (ApplicationCategoryId) REFERENCES dbo.ApplicationCategories (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicationFors_LegacyId ON dbo.ApplicationFors (LegacyId);
    CREATE INDEX IX_ApplicationFors_ApplicationCategoryId ON dbo.ApplicationFors (ApplicationCategoryId, SortOrder);
    CREATE INDEX IX_ApplicationFors_LegacyApplicationCategoryId ON dbo.ApplicationFors (LegacyApplicationCategoryId);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationForTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationForTypes
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationForTypes PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId INT NOT NULL,
        ApplicationForId UNIQUEIDENTIFIER NULL,
        ApplicationTypeId UNIQUEIDENTIFIER NULL,
        LegacyApplicationForId INT NULL,
        LegacyApplicationTypeId INT NULL,
        LegacyApplicationForExemptionTypeId INT NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationForTypes_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationForTypes_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationForTypes_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationForTypes_ApplicationFors_ApplicationForId
            FOREIGN KEY (ApplicationForId) REFERENCES dbo.ApplicationFors (Id),
        CONSTRAINT FK_ApplicationForTypes_ApplicationTypes_ApplicationTypeId
            FOREIGN KEY (ApplicationTypeId) REFERENCES dbo.ApplicationTypes (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicationForTypes_LegacyId ON dbo.ApplicationForTypes (LegacyId);
    CREATE UNIQUE INDEX IX_ApplicationForTypes_ApplicationForId_ApplicationTypeId
        ON dbo.ApplicationForTypes (ApplicationForId, ApplicationTypeId)
        WHERE ApplicationForId IS NOT NULL AND ApplicationTypeId IS NOT NULL;
    CREATE INDEX IX_ApplicationForTypes_LegacyApplicationForId ON dbo.ApplicationForTypes (LegacyApplicationForId);
    CREATE INDEX IX_ApplicationForTypes_LegacyApplicationTypeId ON dbo.ApplicationForTypes (LegacyApplicationTypeId);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationCategoryFors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationCategoryFors
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationCategoryFors PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId BIGINT NOT NULL,
        ApplicationCategoryId UNIQUEIDENTIFIER NULL,
        ApplicationForId UNIQUEIDENTIFIER NULL,
        LegacyApplicationCategoryId INT NULL,
        LegacyApplicationForId INT NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationCategoryFors_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationCategoryFors_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationCategoryFors_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationCategoryFors_ApplicationCategories_ApplicationCategoryId
            FOREIGN KEY (ApplicationCategoryId) REFERENCES dbo.ApplicationCategories (Id),
        CONSTRAINT FK_ApplicationCategoryFors_ApplicationFors_ApplicationForId
            FOREIGN KEY (ApplicationForId) REFERENCES dbo.ApplicationFors (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicationCategoryFors_LegacyId ON dbo.ApplicationCategoryFors (LegacyId);
    CREATE UNIQUE INDEX IX_ApplicationCategoryFors_ApplicationCategoryId_ApplicationForId
        ON dbo.ApplicationCategoryFors (ApplicationCategoryId, ApplicationForId)
        WHERE ApplicationCategoryId IS NOT NULL AND ApplicationForId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.ApplicationLookupSyncStates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationLookupSyncStates
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationLookupSyncStates PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        SourceSystem NVARCHAR(100) NOT NULL,
        SyncName NVARCHAR(100) NOT NULL,
        LastStartedAt DATETIME2(3) NULL,
        LastCompletedAt DATETIME2(3) NULL,
        LastRunSucceeded BIT NULL,
        LastProcessedRows INT NOT NULL CONSTRAINT DF_ApplicationLookupSyncStates_LastProcessedRows DEFAULT (0),
        LastRunMessage NVARCHAR(4000) NULL
    );

    CREATE UNIQUE INDEX IX_ApplicationLookupSyncStates_SourceSystem_SyncName
        ON dbo.ApplicationLookupSyncStates (SourceSystem, SyncName);
END;
GO
