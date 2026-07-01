IF OBJECT_ID(N'dbo.CompanyAuthorizedPersons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyAuthorizedPersons
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyAuthorizedPersons PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        FullName NVARCHAR(100) NULL,
        Designation NVARCHAR(100) NULL,
        LegacyIdentityTypeId INT NULL,
        IdentityNumber NVARCHAR(50) NULL,
        Email NVARCHAR(250) NULL,
        TelephoneNo NVARCHAR(20) NULL,
        LegacyUserId INT NULL,
        IsDigiCertPaid BIT NULL,
        IsCertified BIT NULL,
        IsPinVerified BIT NULL,
        IsDeletedInSource BIT NOT NULL CONSTRAINT DF_CompanyAuthorizedPersons_IsDeletedInSource DEFAULT (0),
        LegacyTitleId INT NULL,
        LegacyCitizenshipId BIGINT NULL,
        CanEdit BIT NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyAuthorizedPersons_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyAuthorizedPersons_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyAuthorizedPersons_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_CompanyAuthorizedPersons_MigratedId
        ON dbo.CompanyAuthorizedPersons (MigratedId);

    CREATE INDEX IX_CompanyAuthorizedPersons_CompanyProfileId
        ON dbo.CompanyAuthorizedPersons (CompanyProfileId);
END;

IF OBJECT_ID(N'dbo.CompanyBoardDirectors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyBoardDirectors
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyBoardDirectors PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        [Name] NVARCHAR(50) NULL,
        LegacyNationalityId BIGINT NULL,
        SharePercentage DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyBoardDirectors_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyBoardDirectors_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyBoardDirectors_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_CompanyBoardDirectors_MigratedId
        ON dbo.CompanyBoardDirectors (MigratedId);

    CREATE INDEX IX_CompanyBoardDirectors_CompanyProfileId
        ON dbo.CompanyBoardDirectors (CompanyProfileId);
END;

IF OBJECT_ID(N'dbo.CompanyAttachmentDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyAttachmentDocuments
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyAttachmentDocuments PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        FileName NVARCHAR(300) NULL,
        FileType NVARCHAR(100) NULL,
        FileContent VARBINARY(MAX) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyAttachmentDocuments_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyAttachmentDocuments_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyAttachmentDocuments_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_CompanyAttachmentDocuments_MigratedId
        ON dbo.CompanyAttachmentDocuments (MigratedId);

    CREATE INDEX IX_CompanyAttachmentDocuments_CompanyProfileId
        ON dbo.CompanyAttachmentDocuments (CompanyProfileId);
END;

IF OBJECT_ID(N'dbo.CompanyRelatedDataSyncStates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyRelatedDataSyncStates
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyRelatedDataSyncStates PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        SourceSystem NVARCHAR(100) NOT NULL,
        SyncName NVARCHAR(100) NOT NULL,
        LastSourceCompanyId BIGINT NULL,
        LastStartedAt DATETIME2(3) NULL,
        LastCompletedAt DATETIME2(3) NULL,
        LastRunSucceeded BIT NULL,
        LastProcessedRows INT NOT NULL CONSTRAINT DF_CompanyRelatedDataSyncStates_LastProcessedRows DEFAULT (0),
        LastRunMessage NVARCHAR(4000) NULL
    );

    CREATE UNIQUE INDEX IX_CompanyRelatedDataSyncStates_SourceSystem_SyncName
        ON dbo.CompanyRelatedDataSyncStates (SourceSystem, SyncName);
END;
