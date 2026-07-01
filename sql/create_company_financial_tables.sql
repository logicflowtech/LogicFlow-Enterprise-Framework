IF OBJECT_ID(N'dbo.CompanyProfileFinancialDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileFinancialDetails
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileFinancialDetails PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        LegacyProjectId BIGINT NULL,
        FinancialYear INT NULL,
        EffectiveDate DATETIME2(3) NULL,
        ProjectStatusId INT NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileFinancialDetails_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileFinancialDetails_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileFinancialDetails_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileFinancialDetails_MigratedId ON dbo.CompanyProfileFinancialDetails (MigratedId);
    CREATE INDEX IX_CompanyProfileFinancialDetails_CompanyProfileId ON dbo.CompanyProfileFinancialDetails (CompanyProfileId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileAuthorizedCapitals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileAuthorizedCapitals
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileAuthorizedCapitals PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        CompanyProfileId UNIQUEIDENTIFIER NOT NULL,
        MigratedCompanyId BIGINT NOT NULL,
        AuthorizedCapital DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileAuthorizedCapitals_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileAuthorizedCapitals_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileAuthorizedCapitals_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileAuthorizedCapitals_MigratedCompanyId ON dbo.CompanyProfileAuthorizedCapitals (MigratedCompanyId);
    CREATE INDEX IX_CompanyProfileAuthorizedCapitals_CompanyProfileId ON dbo.CompanyProfileAuthorizedCapitals (CompanyProfileId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileEquityStructures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileEquityStructures
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileEquityStructures PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        BumiRm DECIMAL(18,2) NULL,
        BumiPercent DECIMAL(18,2) NULL,
        NonBumiRm DECIMAL(18,2) NULL,
        NonBumiPercent DECIMAL(18,2) NULL,
        ForeignRm DECIMAL(18,2) NULL,
        ForeignPercent DECIMAL(18,2) NULL,
        TotalRm DECIMAL(18,2) NULL,
        TotalPercent DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileEquityStructures_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileEquityStructures_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileEquityStructures_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileEquityStructures_FinancialDetailsId ON dbo.CompanyProfileEquityStructures (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileFinancialPerformanceRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileFinancialPerformanceRecords
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileFinancialPerformanceRecords PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        Revenue DECIMAL(18,2) NULL,
        Profit DECIMAL(18,2) NULL,
        TaxableExpenditure DECIMAL(18,2) NULL,
        ExportSales DECIMAL(18,2) NULL,
        LocalSales DECIMAL(18,2) NULL,
        TotalAsset DECIMAL(18,2) NULL,
        ShareholderFund DECIMAL(18,2) NULL,
        EmployeeCount INT NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileFinancialPerformanceRecords_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileFinancialPerformanceRecords_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileFinancialPerformanceRecords_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileFinancialPerformanceRecords_FinancialDetailsId ON dbo.CompanyProfileFinancialPerformanceRecords (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfilePaidUpCapitals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfilePaidUpCapitals
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfilePaidUpCapitals PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        TotalPaidUpCapital DECIMAL(18,2) NULL,
        TotalReserves DECIMAL(18,2) NULL,
        TotalShareholderFund DECIMAL(18,2) NULL,
        TotalRmMalaysianIndividuals DECIMAL(18,2) NULL,
        TotalPercentMalaysianIndividuals DECIMAL(18,2) NULL,
        TotalRmForeignCompany DECIMAL(18,2) NULL,
        TotalPercentForeignCompany DECIMAL(18,2) NULL,
        TotalRmCompanyMalaysia DECIMAL(18,2) NULL,
        TotalPercentCompanyMalaysia DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitals_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitals_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitals_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfilePaidUpCapitals_FinancialDetailsId ON dbo.CompanyProfilePaidUpCapitals (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfilePaidUpCapitalMalaysianIndividuals PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        BumiputeraRm DECIMAL(18,2) NULL,
        BumiputeraPercent DECIMAL(18,2) NULL,
        NonBumiputeraRm DECIMAL(18,2) NULL,
        NonBumiputeraPercent DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalMalaysianIndividuals_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalMalaysianIndividuals_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalMalaysianIndividuals_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfilePaidUpCapitalMalaysianIndividuals_FinancialDetailsId ON dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfilePaidUpCapitalForeignCompanies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfilePaidUpCapitalForeignCompanies
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfilePaidUpCapitalForeignCompanies PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        CompanyName NVARCHAR(300) NULL,
        CountryId UNIQUEIDENTIFIER NULL,
        LegacyCountryId BIGINT NULL,
        AmountRm DECIMAL(18,2) NULL,
        AmountPercent DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalForeignCompanies_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalForeignCompanies_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalForeignCompanies_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfilePaidUpCapitalForeignCompanies_MigratedId ON dbo.CompanyProfilePaidUpCapitalForeignCompanies (MigratedId);
    CREATE INDEX IX_CompanyProfilePaidUpCapitalForeignCompanies_FinancialDetailsId ON dbo.CompanyProfilePaidUpCapitalForeignCompanies (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfilePaidUpCapitalCompaniesMalaysia PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        CompanyName NVARCHAR(300) NULL,
        AmountRm DECIMAL(18,2) NULL,
        AmountPercent DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalCompaniesMalaysia_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalCompaniesMalaysia_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfilePaidUpCapitalCompaniesMalaysia_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfilePaidUpCapitalCompaniesMalaysia_MigratedId ON dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia (MigratedId);
    CREATE INDEX IX_CompanyProfilePaidUpCapitalCompaniesMalaysia_FinancialDetailsId ON dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileCompanyIncorporated', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileCompanyIncorporated
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileCompanyIncorporated PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        PaidUpCapitalCompanyMalaysiaEntryId UNIQUEIDENTIFIER NOT NULL,
        LocalCompanyTypeId INT NULL,
        BumiPercent DECIMAL(18,2) NULL,
        NonBumiPercent DECIMAL(18,2) NULL,
        ForeignCountryId UNIQUEIDENTIFIER NULL,
        LegacyForeignCountryId BIGINT NULL,
        ForeignPercent DECIMAL(18,2) NULL,
        TotalPercent DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporated_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporated_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporated_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileCompanyIncorporated_MigratedId ON dbo.CompanyProfileCompanyIncorporated (MigratedId);
    CREATE INDEX IX_CompanyProfileCompanyIncorporated_FinancialDetailsId ON dbo.CompanyProfileCompanyIncorporated (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileCompanyIncorporatedCountries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileCompanyIncorporatedCountries
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileCompanyIncorporatedCountries PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        CompanyIncorporatedEntryId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        CountryId UNIQUEIDENTIFIER NULL,
        LegacyCountryId BIGINT NULL,
        CountryPercent DECIMAL(18,2) NULL,
        AmountRm DECIMAL(18,2) NULL,
        PercentOverTotal DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporatedCountries_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporatedCountries_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileCompanyIncorporatedCountries_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileCompanyIncorporatedCountries_MigratedId ON dbo.CompanyProfileCompanyIncorporatedCountries (MigratedId);
    CREATE INDEX IX_CompanyProfileCompanyIncorporatedCountries_CompanyIncorporatedEntryId ON dbo.CompanyProfileCompanyIncorporatedCountries (CompanyIncorporatedEntryId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileLoans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileLoans
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileLoans PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        TotalRmLoan DECIMAL(18,2) NULL,
        TotalLoanPercent DECIMAL(18,2) NULL,
        DomesticDescription NVARCHAR(MAX) NULL,
        ForeignDescription NVARCHAR(MAX) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileLoans_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileLoans_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileLoans_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileLoans_FinancialDetailsId ON dbo.CompanyProfileLoans (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileLoanDomestics', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileLoanDomestics
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileLoanDomestics PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        AmountRm DECIMAL(18,2) NULL,
        AmountPercent DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileLoanDomestics_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileLoanDomestics_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileLoanDomestics_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileLoanDomestics_FinancialDetailsId ON dbo.CompanyProfileLoanDomestics (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileLoanForeigns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileLoanForeigns
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileLoanForeigns PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        CountryId UNIQUEIDENTIFIER NULL,
        LegacyCountryId BIGINT NULL,
        AmountRm DECIMAL(18,2) NULL,
        AmountPercent DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileLoanForeigns_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileLoanForeigns_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileLoanForeigns_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileLoanForeigns_MigratedId ON dbo.CompanyProfileLoanForeigns (MigratedId);
    CREATE INDEX IX_CompanyProfileLoanForeigns_FinancialDetailsId ON dbo.CompanyProfileLoanForeigns (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileTotalFinancings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileTotalFinancings
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileTotalFinancings PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        TotalPaidUpCapital DECIMAL(18,2) NULL,
        TotalReserve DECIMAL(18,2) NULL,
        TotalLoan DECIMAL(18,2) NULL,
        TotalRmOtherSources DECIMAL(18,2) NULL,
        TotalPercentOtherSources DECIMAL(18,2) NULL,
        TotalFinancingRm DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileTotalFinancings_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileTotalFinancings_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileTotalFinancings_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileTotalFinancings_FinancialDetailsId ON dbo.CompanyProfileTotalFinancings (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileOtherSources', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileOtherSources
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileOtherSources PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        OtherSources NVARCHAR(500) NULL,
        AmountRm DECIMAL(18,2) NULL,
        AmountPercent DECIMAL(18,2) NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileOtherSources_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileOtherSources_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileOtherSources_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileOtherSources_MigratedId ON dbo.CompanyProfileOtherSources (MigratedId);
    CREATE INDEX IX_CompanyProfileOtherSources_FinancialDetailsId ON dbo.CompanyProfileOtherSources (FinancialDetailsId);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileUltimateParentHoldingCompanies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileUltimateParentHoldingCompanies
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyProfileUltimateParentHoldingCompanies PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NULL,
        FinancialDetailsId UNIQUEIDENTIFIER NOT NULL,
        MigratedId BIGINT NOT NULL,
        PaidUpCapitalForeignCompanyEntryId UNIQUEIDENTIFIER NOT NULL,
        UltimateCompany NVARCHAR(300) NULL,
        CountryId UNIQUEIDENTIFIER NULL,
        LegacyCountryId BIGINT NULL,
        SourceCreatedByLegacyUserId INT NULL,
        SourceCreatedAt DATETIME2(3) NULL,
        SourceUpdatedByLegacyUserId INT NULL,
        SourceUpdatedAt DATETIME2(3) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfileUltimateParentHoldingCompanies_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_CompanyProfileUltimateParentHoldingCompanies_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(450) NULL,
        UpdatedAt DATETIMEOFFSET NULL,
        UpdatedBy NVARCHAR(450) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_CompanyProfileUltimateParentHoldingCompanies_IsDeleted DEFAULT (0),
        DeletedAt DATETIMEOFFSET NULL
    );
    CREATE UNIQUE INDEX IX_CompanyProfileUltimateParentHoldingCompanies_MigratedId ON dbo.CompanyProfileUltimateParentHoldingCompanies (MigratedId);
    CREATE INDEX IX_CompanyProfileUltimateParentHoldingCompanies_FinancialDetailsId ON dbo.CompanyProfileUltimateParentHoldingCompanies (FinancialDetailsId);
END;
GO
