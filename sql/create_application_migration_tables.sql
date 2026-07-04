SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ApplicationSectors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationSectors
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationSectors PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId INT NOT NULL,
        Name NVARCHAR(200) NULL,
        SortOrder INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ApplicationSectors_IsActive DEFAULT (1),
        FilteringLabel NVARCHAR(200) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationSectors_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationSectors_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationSectors_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_ApplicationSectors_LegacyId ON dbo.ApplicationSectors (LegacyId);
    CREATE INDEX IX_ApplicationSectors_SortOrder ON dbo.ApplicationSectors (SortOrder, Name);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationMainIndustries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationMainIndustries
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationMainIndustries PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId INT NOT NULL,
        Name NVARCHAR(200) NULL,
        SortOrder INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ApplicationMainIndustries_IsActive DEFAULT (1),
        LegacyNavigationTypeId INT NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationMainIndustries_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationMainIndustries_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationMainIndustries_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );

    CREATE UNIQUE INDEX IX_ApplicationMainIndustries_LegacyId ON dbo.ApplicationMainIndustries (LegacyId);
    CREATE INDEX IX_ApplicationMainIndustries_SortOrder ON dbo.ApplicationMainIndustries (SortOrder, Name);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationForSectors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationForSectors
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationForSectors PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId BIGINT NOT NULL,
        ApplicationForId UNIQUEIDENTIFIER NULL,
        SectorId UNIQUEIDENTIFIER NULL,
        LegacyApplicationForId INT NULL,
        LegacySectorId INT NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationForSectors_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationForSectors_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationForSectors_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationForSectors_ApplicationFors_ApplicationForId
            FOREIGN KEY (ApplicationForId) REFERENCES dbo.ApplicationFors (Id),
        CONSTRAINT FK_ApplicationForSectors_ApplicationSectors_SectorId
            FOREIGN KEY (SectorId) REFERENCES dbo.ApplicationSectors (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicationForSectors_LegacyId ON dbo.ApplicationForSectors (LegacyId);
    CREATE UNIQUE INDEX IX_ApplicationForSectors_ApplicationForId_SectorId
        ON dbo.ApplicationForSectors (ApplicationForId, SectorId)
        WHERE ApplicationForId IS NOT NULL AND SectorId IS NOT NULL;
    CREATE INDEX IX_ApplicationForSectors_LegacyApplicationForId ON dbo.ApplicationForSectors (LegacyApplicationForId);
    CREATE INDEX IX_ApplicationForSectors_LegacySectorId ON dbo.ApplicationForSectors (LegacySectorId);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationSectorIndustries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationSectorIndustries
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationSectorIndustries PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId BIGINT NOT NULL,
        SectorId UNIQUEIDENTIFIER NULL,
        MainIndustryId UNIQUEIDENTIFIER NULL,
        LegacySectorId INT NULL,
        LegacyMainIndustryId INT NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ApplicationSectorIndustries_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationSectorIndustries_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationSectorIndustries_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationSectorIndustries_ApplicationSectors_SectorId
            FOREIGN KEY (SectorId) REFERENCES dbo.ApplicationSectors (Id),
        CONSTRAINT FK_ApplicationSectorIndustries_ApplicationMainIndustries_MainIndustryId
            FOREIGN KEY (MainIndustryId) REFERENCES dbo.ApplicationMainIndustries (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicationSectorIndustries_LegacyId ON dbo.ApplicationSectorIndustries (LegacyId);
    CREATE UNIQUE INDEX IX_ApplicationSectorIndustries_SectorId_MainIndustryId
        ON dbo.ApplicationSectorIndustries (SectorId, MainIndustryId)
        WHERE SectorId IS NOT NULL AND MainIndustryId IS NOT NULL;
    CREATE INDEX IX_ApplicationSectorIndustries_LegacySectorId ON dbo.ApplicationSectorIndustries (LegacySectorId);
    CREATE INDEX IX_ApplicationSectorIndustries_LegacyMainIndustryId ON dbo.ApplicationSectorIndustries (LegacyMainIndustryId);
END;
GO

IF OBJECT_ID(N'dbo.Applications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Applications
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Applications PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        LegacyId BIGINT NULL,
        ApplicationNumber NVARCHAR(100) NULL,
        ApplicationDate DATETIME2(3) NULL,
        ApplicationCategoryId UNIQUEIDENTIFIER NULL,
        ApplicationForId UNIQUEIDENTIFIER NULL,
        ApplicationTypeId UNIQUEIDENTIFIER NULL,
        ApplicationStatusId UNIQUEIDENTIFIER NULL,
        SectorId UNIQUEIDENTIFIER NULL,
        MainIndustryId UNIQUEIDENTIFIER NULL,
        LegacyCompanyId BIGINT NULL,
        LegacyFormId BIGINT NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Applications_IsActive DEFAULT (1),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_Applications_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_Applications_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_Applications_ApplicationCategories_ApplicationCategoryId
            FOREIGN KEY (ApplicationCategoryId) REFERENCES dbo.ApplicationCategories (Id),
        CONSTRAINT FK_Applications_ApplicationFors_ApplicationForId
            FOREIGN KEY (ApplicationForId) REFERENCES dbo.ApplicationFors (Id),
        CONSTRAINT FK_Applications_ApplicationTypes_ApplicationTypeId
            FOREIGN KEY (ApplicationTypeId) REFERENCES dbo.ApplicationTypes (Id),
        CONSTRAINT FK_Applications_ApplicationStatuses_ApplicationStatusId
            FOREIGN KEY (ApplicationStatusId) REFERENCES dbo.ApplicationStatuses (Id),
        CONSTRAINT FK_Applications_ApplicationSectors_SectorId
            FOREIGN KEY (SectorId) REFERENCES dbo.ApplicationSectors (Id),
        CONSTRAINT FK_Applications_ApplicationMainIndustries_MainIndustryId
            FOREIGN KEY (MainIndustryId) REFERENCES dbo.ApplicationMainIndustries (Id)
    );

    CREATE UNIQUE INDEX IX_Applications_LegacyId ON dbo.Applications (LegacyId) WHERE LegacyId IS NOT NULL;
    CREATE INDEX IX_Applications_ApplicationNumber ON dbo.Applications (ApplicationNumber);
    CREATE INDEX IX_Applications_ApplicationDate ON dbo.Applications (ApplicationDate);
    CREATE INDEX IX_Applications_Status ON dbo.Applications (ApplicationStatusId, ApplicationDate);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationCompanyProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationCompanyProfiles
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationCompanyProfiles PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        ApplicationId UNIQUEIDENTIFIER NOT NULL,
        LegacyParticularOfCompanyId BIGINT NULL,
        LegacyApplicationId BIGINT NULL,
        LegacyCompanyId BIGINT NULL,
        LegacyAddressId BIGINT NULL,
        CompanyName NVARCHAR(200) NULL,
        RegistrationNumber NVARCHAR(100) NULL,
        RegistrationTypeId INT NULL,
        RegistrationTypeLabel NVARCHAR(200) NULL,
        DateOfIncorporation DATETIME2(3) NULL,
        RegistrationDate DATETIME2(3) NULL,
        TelephoneNumber NVARCHAR(100) NULL,
        FaxNumber NVARCHAR(100) NULL,
        Website NVARCHAR(200) NULL,
        Email NVARCHAR(250) NULL,
        IncomeTaxNo NVARCHAR(100) NULL,
        EpfNo NVARCHAR(50) NULL,
        SocsoNo NVARCHAR(50) NULL,
        LegacyUserId INT NULL,
        LegacyCompanySignatureId BIGINT NULL,
        LegacyCompanyTypeId INT NULL,
        IsCompanyCertified BIT NULL,
        LegacyCompanyApprovalStatusId INT NULL,
        IsPaid BIT NULL,
        IsCompanyLocal BIT NULL,
        TotalEmployment INT NULL,
        LegacyAgencyBranchId BIGINT NULL,
        CompanyBackground NVARCHAR(MAX) NULL,
        RegisteredAddress1 NVARCHAR(200) NULL,
        RegisteredAddress2 NVARCHAR(200) NULL,
        RegisteredAddress3 NVARCHAR(200) NULL,
        RegisteredCountryId BIGINT NULL,
        RegisteredCountryName NVARCHAR(200) NULL,
        RegisteredStateId BIGINT NULL,
        RegisteredStateName NVARCHAR(200) NULL,
        RegisteredCityId BIGINT NULL,
        RegisteredCityName NVARCHAR(200) NULL,
        RegisteredPostcode NVARCHAR(50) NULL,
        IsCorrespondenceSameAsRegistered BIT NOT NULL CONSTRAINT DF_ApplicationCompanyProfiles_IsCorrespondenceSameAsRegistered DEFAULT (1),
        CorrespondenceAddress1 NVARCHAR(200) NULL,
        CorrespondenceAddress2 NVARCHAR(200) NULL,
        CorrespondenceAddress3 NVARCHAR(200) NULL,
        CorrespondenceCountryId BIGINT NULL,
        CorrespondenceCountryName NVARCHAR(200) NULL,
        CorrespondenceStateId BIGINT NULL,
        CorrespondenceStateName NVARCHAR(200) NULL,
        CorrespondenceCityId BIGINT NULL,
        CorrespondenceCityName NVARCHAR(200) NULL,
        CorrespondencePostcode NVARCHAR(50) NULL,
        CustomsControlStationCode NVARCHAR(100) NULL,
        CustomsControlStationName NVARCHAR(200) NULL,
        SourcePulledAt DATETIME2(3) NULL,
        LastReviewedAt DATETIME2(3) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationCompanyProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationCompanyProfiles_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationCompanyProfiles_ApplicantApplications_ApplicationId
            FOREIGN KEY (ApplicationId) REFERENCES dbo.ApplicantApplications (Id)
    );

    CREATE UNIQUE INDEX IX_ApplicationCompanyProfiles_ApplicationId
        ON dbo.ApplicationCompanyProfiles (ApplicationId)
        WHERE IsDeleted = 0;
    CREATE INDEX IX_ApplicationCompanyProfiles_LegacyParticularOfCompanyId
        ON dbo.ApplicationCompanyProfiles (LegacyParticularOfCompanyId)
        WHERE LegacyParticularOfCompanyId IS NOT NULL AND IsDeleted = 0;
    CREATE INDEX IX_ApplicationCompanyProfiles_LegacyApplicationId
        ON dbo.ApplicationCompanyProfiles (LegacyApplicationId);
    CREATE INDEX IX_ApplicationCompanyProfiles_LegacyCompanyId
        ON dbo.ApplicationCompanyProfiles (LegacyCompanyId);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationCompanyProfiles', N'U') IS NOT NULL
   AND EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = N'FK_ApplicationCompanyProfiles_Applications_ApplicationId'
         AND parent_object_id = OBJECT_ID(N'dbo.ApplicationCompanyProfiles')
   )
BEGIN
    ALTER TABLE dbo.ApplicationCompanyProfiles
        DROP CONSTRAINT FK_ApplicationCompanyProfiles_Applications_ApplicationId;

    ALTER TABLE dbo.ApplicationCompanyProfiles
        ADD CONSTRAINT FK_ApplicationCompanyProfiles_ApplicantApplications_ApplicationId
            FOREIGN KEY (ApplicationId) REFERENCES dbo.ApplicantApplications (Id);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationCompanyProfiles', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = N'FK_ApplicationCompanyProfiles_ApplicantApplications_ApplicationId'
         AND parent_object_id = OBJECT_ID(N'dbo.ApplicationCompanyProfiles')
   )
BEGIN
    ALTER TABLE dbo.ApplicationCompanyProfiles
        ADD CONSTRAINT FK_ApplicationCompanyProfiles_ApplicantApplications_ApplicationId
            FOREIGN KEY (ApplicationId) REFERENCES dbo.ApplicantApplications (Id);
END;
GO

IF OBJECT_ID(N'dbo.ApplicationCompanyDirectors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationCompanyDirectors
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationCompanyDirectors PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        ApplicationCompanyProfileId UNIQUEIDENTIFIER NOT NULL,
        LegacyDirectorId BIGINT NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_ApplicationCompanyDirectors_DisplayOrder DEFAULT (1),
        DirectorName NVARCHAR(200) NULL,
        LegacyNationalityId BIGINT NULL,
        NationalityName NVARCHAR(200) NULL,
        SharesHeldPercent DECIMAL(9,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationCompanyDirectors_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationCompanyDirectors_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationCompanyDirectors_ApplicationCompanyProfiles_ApplicationCompanyProfileId
            FOREIGN KEY (ApplicationCompanyProfileId) REFERENCES dbo.ApplicationCompanyProfiles (Id)
    );

    CREATE INDEX IX_ApplicationCompanyDirectors_ApplicationCompanyProfileId
        ON dbo.ApplicationCompanyDirectors (ApplicationCompanyProfileId, DisplayOrder);
    CREATE UNIQUE INDEX IX_ApplicationCompanyDirectors_LegacyDirectorId_ApplicationCompanyProfileId
        ON dbo.ApplicationCompanyDirectors (ApplicationCompanyProfileId, LegacyDirectorId)
        WHERE LegacyDirectorId IS NOT NULL AND IsDeleted = 0;
END;
GO

IF OBJECT_ID(N'dbo.ApplicationCompanyContactPersons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationCompanyContactPersons
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ApplicationCompanyContactPersons PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        ApplicationCompanyProfileId UNIQUEIDENTIFIER NOT NULL,
        LegacyParticularContactPersonId BIGINT NULL,
        LegacyContactPersonId BIGINT NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_ApplicationCompanyContactPersons_DisplayOrder DEFAULT (1),
        Title NVARCHAR(50) NULL,
        FullName NVARCHAR(256) NULL,
        Designation NVARCHAR(200) NULL,
        Email NVARCHAR(250) NULL,
        PhoneNo NVARCHAR(100) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_ApplicationCompanyContactPersons_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApplicationCompanyContactPersons_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL,
        CONSTRAINT FK_ApplicationCompanyContactPersons_ApplicationCompanyProfiles_ApplicationCompanyProfileId
            FOREIGN KEY (ApplicationCompanyProfileId) REFERENCES dbo.ApplicationCompanyProfiles (Id)
    );

    CREATE INDEX IX_ApplicationCompanyContactPersons_ApplicationCompanyProfileId
        ON dbo.ApplicationCompanyContactPersons (ApplicationCompanyProfileId, DisplayOrder);
    CREATE INDEX IX_ApplicationCompanyContactPersons_LegacyParticularContactPersonId
        ON dbo.ApplicationCompanyContactPersons (LegacyParticularContactPersonId)
        WHERE LegacyParticularContactPersonId IS NOT NULL AND IsDeleted = 0;
    CREATE INDEX IX_ApplicationCompanyContactPersons_LegacyContactPersonId
        ON dbo.ApplicationCompanyContactPersons (LegacyContactPersonId);
END;
GO
