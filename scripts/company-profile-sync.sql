/*
    Company Profile local cache and incremental sync

    Run this against the local application database:
        LogicFlowEnterpriseFrameworkDb

    Prerequisite:
        dbo.syn_Company must exist and point to the external source table.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.syn_Company', N'SN') IS NULL
BEGIN
    THROW 50001, 'Required synonym dbo.syn_Company was not found in the current database.', 1;
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfiles
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CompanyProfiles_Id DEFAULT NEWSEQUENTIALID(),
        MigratedId BIGINT NULL,
        CompanyName NVARCHAR(100) NULL,
        RegistrationNo NVARCHAR(50) NULL,
        RegistrationDate DATETIME2(3) NULL,
        DateOfIncorporation DATETIME2(3) NULL,
        TelephoneNo NVARCHAR(100) NULL,
        FaxNo NVARCHAR(100) NULL,
        Website NVARCHAR(100) NULL,
        Email NVARCHAR(250) NULL,
        IncomeTaxNo NVARCHAR(50) NULL,
        EpfNo NVARCHAR(20) NULL,
        SocsoNo NVARCHAR(20) NULL,
        UserId INT NULL,
        CompanySignatureId BIGINT NULL,
        CompanyType INT NULL,
        IsCompanyCertified BIT NULL,
        CompanyApprovalStatus INT NULL,
        IsPaid BIT NULL,
        IsCompanyLocal BIT NULL,
        CreatedBySourceUserId INT NULL,
        SourceCreatedDateTime DATETIME2(3) NULL,
        ModifiedBySourceUserId INT NULL,
        SourceModifiedDateTime DATETIME2(3) NULL,
        AddressId BIGINT NULL,
        BackgroundDescription1 NVARCHAR(MAX) NULL,
        NewSsmCompanyRegNo NVARCHAR(50) NULL,
        CompanyStatusId INT NULL,
        TotalEmployment INT NULL,
        AnnualClosingDateDay INT NULL,
        AnnualClosingDateMonth INT NULL,
        AprNo NVARCHAR(50) NULL,
        NonCode NVARCHAR(2) NULL,
        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfiles_LastSyncedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_CompanyProfiles PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_MigratedId' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
BEGIN
    CREATE UNIQUE INDEX IX_CompanyProfiles_MigratedId
        ON dbo.CompanyProfiles (MigratedId)
        WHERE MigratedId IS NOT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_RegistrationNo' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
BEGIN
    CREATE INDEX IX_CompanyProfiles_RegistrationNo
        ON dbo.CompanyProfiles (RegistrationNo);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_NewSsmCompanyRegNo' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
BEGIN
    CREATE INDEX IX_CompanyProfiles_NewSsmCompanyRegNo
        ON dbo.CompanyProfiles (NewSsmCompanyRegNo);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_CompanyName' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
BEGIN
    CREATE INDEX IX_CompanyProfiles_CompanyName
        ON dbo.CompanyProfiles (CompanyName);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_SourceModifiedDateTime' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
BEGIN
    CREATE INDEX IX_CompanyProfiles_SourceModifiedDateTime
        ON dbo.CompanyProfiles (SourceModifiedDateTime DESC, MigratedId DESC);
END;
GO

IF OBJECT_ID(N'dbo.CompanyProfileSyncState', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfileSyncState
    (
        SourceName NVARCHAR(100) NOT NULL,
        LastSourceModifiedDateTime DATETIME2(3) NULL,
        LastSourceCompanyId BIGINT NULL,
        LastStartedAt DATETIME2(3) NULL,
        LastCompletedAt DATETIME2(3) NULL,
        LastRunSucceeded BIT NULL,
        LastProcessedRows INT NOT NULL CONSTRAINT DF_CompanyProfileSyncState_LastProcessedRows DEFAULT (0),
        LastRunMessage NVARCHAR(4000) NULL,
        CONSTRAINT PK_CompanyProfileSyncState PRIMARY KEY CLUSTERED (SourceName)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CompanyProfileSyncState WHERE SourceName = N'syn_Company')
BEGIN
    INSERT INTO dbo.CompanyProfileSyncState (SourceName, LastRunSucceeded, LastProcessedRows, LastRunMessage)
    VALUES (N'syn_Company', NULL, 0, N'Not started');
END;
GO

CREATE OR ALTER PROCEDURE dbo.SyncCompanyProfilesFromSynonym
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @startedAt DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @processedRows INT = 0;
    DECLARE @lastModified DATETIME2(3);
    DECLARE @lastId BIGINT;
    DECLARE @newLastModified DATETIME2(3);
    DECLARE @newLastId BIGINT;
    DECLARE @appLockResult INT;

    EXEC @appLockResult = sp_getapplock
        @Resource = N'CompanyProfileSync:syn_Company',
        @LockMode = N'Exclusive',
        @LockOwner = N'Session',
        @LockTimeout = 10000;

    IF @appLockResult < 0
    BEGIN
        THROW 50002, 'Unable to acquire sync lock for syn_Company.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.CompanyProfileSyncState
        SET LastStartedAt = @startedAt,
            LastRunSucceeded = NULL,
            LastRunMessage = N'Running'
        WHERE SourceName = N'syn_Company';

        SELECT
            @lastModified = LastSourceModifiedDateTime,
            @lastId = LastSourceCompanyId
        FROM dbo.CompanyProfileSyncState
        WHERE SourceName = N'syn_Company';

        IF OBJECT_ID(N'tempdb..#SourceRows', N'U') IS NOT NULL
        BEGIN
            DROP TABLE #SourceRows;
        END;

        SELECT
            CAST(src.ID AS BIGINT) AS MigratedId,
            NULLIF(LTRIM(RTRIM(src.COMPANYNAME)), N'') AS CompanyName,
            NULLIF(LTRIM(RTRIM(src.REGISTRATIONNO)), N'') AS RegistrationNo,
            CASE WHEN src.REGISTRATIONDATE IS NULL OR CONVERT(date, src.REGISTRATIONDATE) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.REGISTRATIONDATE) END AS RegistrationDate,
            CASE WHEN src.DATEOFINCORPORATION IS NULL OR CONVERT(date, src.DATEOFINCORPORATION) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.DATEOFINCORPORATION) END AS DateOfIncorporation,
            NULLIF(LTRIM(RTRIM(src.TELEPHONENO)), N'') AS TelephoneNo,
            NULLIF(LTRIM(RTRIM(src.FAXNO)), N'') AS FaxNo,
            NULLIF(LTRIM(RTRIM(src.WEBSITE)), N'') AS Website,
            NULLIF(LTRIM(RTRIM(src.EMAIL)), N'') AS Email,
            NULLIF(LTRIM(RTRIM(src.INCOMETAXNO)), N'') AS IncomeTaxNo,
            NULLIF(LTRIM(RTRIM(src.EPFNO)), N'') AS EpfNo,
            NULLIF(LTRIM(RTRIM(src.SOCSONO)), N'') AS SocsoNo,
            src.USERID AS UserId,
            src.COMPANYSIGNATUREID AS CompanySignatureId,
            src.COMPANYTYPE AS CompanyType,
            src.ISCOMPANYCERTIFIED AS IsCompanyCertified,
            src.COMPANYAPPROVALSTATUS AS CompanyApprovalStatus,
            src.ISPAID AS IsPaid,
            src.ISCOMPANYLOCAL AS IsCompanyLocal,
            src.CREATEDBY AS CreatedBySourceUserId,
            CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END AS SourceCreatedDateTime,
            src.MODIFIEDBY AS ModifiedBySourceUserId,
            CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END AS SourceModifiedDateTime,
            src.ADDRESSID AS AddressId,
            NULLIF(src.BACKGROUNDDESCRIPTION1, N'') AS BackgroundDescription1,
            NULLIF(LTRIM(RTRIM(src.NEWSSM_COMPANYREGNO)), N'') AS NewSsmCompanyRegNo,
            src.COMPANYSTATUSID AS CompanyStatusId,
            src.TOTALEMPLOYMENT AS TotalEmployment,
            src.ANNUALCLOSINGDATE_DAY AS AnnualClosingDateDay,
            src.ANNUALCLOSINGDATE_MONTH AS AnnualClosingDateMonth,
            NULLIF(LTRIM(RTRIM(src.APRNO)), N'') AS AprNo,
            NULLIF(LTRIM(RTRIM(src.NON)), N'') AS NonCode,
            COALESCE(
                CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END,
                CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END,
                CONVERT(DATETIME2(3), '1900-01-01')
            ) AS SourceWatermarkDateTime
        INTO #SourceRows
        FROM dbo.syn_Company AS src
        WHERE @lastModified IS NULL
           OR COALESCE(
                CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END,
                CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END,
                CONVERT(DATETIME2(3), '1900-01-01')
              ) > @lastModified
           OR (
                COALESCE(
                    CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END,
                    CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= '19000101' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END,
                    CONVERT(DATETIME2(3), '1900-01-01')
                ) = @lastModified
                AND CAST(src.ID AS BIGINT) > ISNULL(@lastId, 0)
              );

        MERGE dbo.CompanyProfiles AS target
        USING #SourceRows AS source
            ON target.MigratedId = source.MigratedId
        WHEN MATCHED THEN
            UPDATE SET
                MigratedId = source.MigratedId,
                CompanyName = source.CompanyName,
                RegistrationNo = source.RegistrationNo,
                RegistrationDate = source.RegistrationDate,
                DateOfIncorporation = source.DateOfIncorporation,
                TelephoneNo = source.TelephoneNo,
                FaxNo = source.FaxNo,
                Website = source.Website,
                Email = source.Email,
                IncomeTaxNo = source.IncomeTaxNo,
                EpfNo = source.EpfNo,
                SocsoNo = source.SocsoNo,
                UserId = source.UserId,
                CompanySignatureId = source.CompanySignatureId,
                CompanyType = source.CompanyType,
                IsCompanyCertified = source.IsCompanyCertified,
                CompanyApprovalStatus = source.CompanyApprovalStatus,
                IsPaid = source.IsPaid,
                IsCompanyLocal = source.IsCompanyLocal,
                CreatedBySourceUserId = source.CreatedBySourceUserId,
                SourceCreatedDateTime = source.SourceCreatedDateTime,
                ModifiedBySourceUserId = source.ModifiedBySourceUserId,
                SourceModifiedDateTime = source.SourceModifiedDateTime,
                AddressId = source.AddressId,
                BackgroundDescription1 = source.BackgroundDescription1,
                NewSsmCompanyRegNo = source.NewSsmCompanyRegNo,
                CompanyStatusId = source.CompanyStatusId,
                TotalEmployment = source.TotalEmployment,
                AnnualClosingDateDay = source.AnnualClosingDateDay,
                AnnualClosingDateMonth = source.AnnualClosingDateMonth,
                AprNo = source.AprNo,
                NonCode = source.NonCode,
                LastSyncedAt = SYSUTCDATETIME()
        WHEN NOT MATCHED BY TARGET THEN
            INSERT
            (
                MigratedId,
                CompanyName,
                RegistrationNo,
                RegistrationDate,
                DateOfIncorporation,
                TelephoneNo,
                FaxNo,
                Website,
                Email,
                IncomeTaxNo,
                EpfNo,
                SocsoNo,
                UserId,
                CompanySignatureId,
                CompanyType,
                IsCompanyCertified,
                CompanyApprovalStatus,
                IsPaid,
                IsCompanyLocal,
                CreatedBySourceUserId,
                SourceCreatedDateTime,
                ModifiedBySourceUserId,
                SourceModifiedDateTime,
                AddressId,
                BackgroundDescription1,
                NewSsmCompanyRegNo,
                CompanyStatusId,
                TotalEmployment,
                AnnualClosingDateDay,
                AnnualClosingDateMonth,
                AprNo,
                NonCode,
                LastSyncedAt
            )
            VALUES
            (
                source.MigratedId,
                source.CompanyName,
                source.RegistrationNo,
                source.RegistrationDate,
                source.DateOfIncorporation,
                source.TelephoneNo,
                source.FaxNo,
                source.Website,
                source.Email,
                source.IncomeTaxNo,
                source.EpfNo,
                source.SocsoNo,
                source.UserId,
                source.CompanySignatureId,
                source.CompanyType,
                source.IsCompanyCertified,
                source.CompanyApprovalStatus,
                source.IsPaid,
                source.IsCompanyLocal,
                source.CreatedBySourceUserId,
                source.SourceCreatedDateTime,
                source.ModifiedBySourceUserId,
                source.SourceModifiedDateTime,
                source.AddressId,
                source.BackgroundDescription1,
                source.NewSsmCompanyRegNo,
                source.CompanyStatusId,
                source.TotalEmployment,
                source.AnnualClosingDateDay,
                source.AnnualClosingDateMonth,
                source.AprNo,
                source.NonCode,
                SYSUTCDATETIME()
            )
        WHEN NOT MATCHED BY SOURCE AND target.MigratedId IS NOT NULL THEN
            DELETE;

        SET @processedRows = @@ROWCOUNT;

        SELECT TOP (1)
            @newLastModified = SourceWatermarkDateTime,
            @newLastId = MigratedId
        FROM #SourceRows
        ORDER BY SourceWatermarkDateTime DESC, MigratedId DESC;

        UPDATE dbo.CompanyProfileSyncState
        SET LastSourceModifiedDateTime = COALESCE(@newLastModified, LastSourceModifiedDateTime),
            LastSourceCompanyId = COALESCE(@newLastId, LastSourceCompanyId),
            LastCompletedAt = SYSUTCDATETIME(),
            LastRunSucceeded = 1,
            LastProcessedRows = @processedRows,
            LastRunMessage = CONCAT(N'Success. Rows merged: ', @processedRows)
        WHERE SourceName = N'syn_Company';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        UPDATE dbo.CompanyProfileSyncState
        SET LastCompletedAt = SYSUTCDATETIME(),
            LastRunSucceeded = 0,
            LastProcessedRows = 0,
            LastRunMessage = LEFT(ERROR_MESSAGE(), 4000)
        WHERE SourceName = N'syn_Company';

        EXEC sp_releaseapplock
            @Resource = N'CompanyProfileSync:syn_Company',
            @LockOwner = N'Session';

        THROW;
    END CATCH;

    EXEC sp_releaseapplock
        @Resource = N'CompanyProfileSync:syn_Company',
        @LockOwner = N'Session';
END;
GO

/*
    Manual execution:

    EXEC dbo.SyncCompanyProfilesFromSynonym;

    SELECT *
    FROM dbo.CompanyProfileSyncState
    WHERE SourceName = N'syn_Company';
*/
