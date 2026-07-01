SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @TenantId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.Tenants
    WHERE Identifier = N'default'
    ORDER BY CreatedAt
);

IF @TenantId IS NULL
BEGIN
    SELECT TOP (1) @TenantId = Id
    FROM dbo.Tenants
    ORDER BY CreatedAt;
END;

IF @TenantId IS NULL
BEGIN
    THROW 50000, 'No tenant record found in dbo.Tenants.', 1;
END;

BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#AuthorizedSource') IS NOT NULL DROP TABLE #AuthorizedSource;
IF OBJECT_ID('tempdb..#DirectorSource') IS NOT NULL DROP TABLE #DirectorSource;
IF OBJECT_ID('tempdb..#AttachmentSource') IS NOT NULL DROP TABLE #AttachmentSource;

SELECT
    cp.Id AS CompanyProfileId,
    src.MigratedId,
    src.FullName,
    src.Designation,
    src.LegacyIdentityTypeId,
    src.IdentityNumber,
    src.Email,
    src.TelephoneNo,
    src.LegacyUserId,
    src.IsDigiCertPaid,
    src.IsCertified,
    src.IsPinVerified,
    src.IsDeletedInSource,
    src.LegacyTitleId,
    src.LegacyCitizenshipId,
    src.CanEdit,
    src.SourceCreatedByLegacyUserId,
    src.SourceCreatedAt,
    src.SourceUpdatedByLegacyUserId,
    src.SourceUpdatedAt
INTO #AuthorizedSource
FROM OPENROWSET(
    'MSOLEDBSQL',
    'Server=172.16.203.144;Database=Outsystems;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
    '
    SELECT
        CAST(src.ID AS BIGINT) AS MigratedId,
        CAST(src.COMPANYID AS BIGINT) AS LegacyCompanyId,
        NULLIF(LTRIM(RTRIM(src.FULLNAME)), N'''') AS FullName,
        NULLIF(LTRIM(RTRIM(src.DESIGNATION)), N'''') AS Designation,
        CAST(src.IDENTITYTYPE AS INT) AS LegacyIdentityTypeId,
        NULLIF(LTRIM(RTRIM(src.IDENTITYNO)), N'''') AS IdentityNumber,
        NULLIF(LTRIM(RTRIM(src.EMAIL)), N'''') AS Email,
        NULLIF(LTRIM(RTRIM(src.TELEPHONENO)), N'''') AS TelephoneNo,
        CAST(src.USERID AS INT) AS LegacyUserId,
        CAST(src.ISDIGICERTPAID AS BIT) AS IsDigiCertPaid,
        CAST(src.ISCERTIFIED AS BIT) AS IsCertified,
        CAST(src.ISPINVERIFIED AS BIT) AS IsPinVerified,
        CAST(COALESCE(src.ISDELETED, 0) AS BIT) AS IsDeletedInSource,
        CAST(src.TITLEID AS INT) AS LegacyTitleId,
        CAST(src.CITIZENSHIP AS BIGINT) AS LegacyCitizenshipId,
        CAST(src.CANEDIT AS BIT) AS CanEdit,
        CAST(src.CREATEDBY AS INT) AS SourceCreatedByLegacyUserId,
        CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= ''19000101'' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END AS SourceCreatedAt,
        CAST(src.MODIFIEDBY AS INT) AS SourceUpdatedByLegacyUserId,
        CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= ''19000101'' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END AS SourceUpdatedAt
    FROM dbo.OSUSR_1sw_AuthorizedPerson src
    WHERE src.COMPANYID IS NOT NULL
    '
) AS src
INNER JOIN dbo.CompanyProfiles cp
    ON cp.MigratedId = src.LegacyCompanyId;

SELECT
    cp.Id AS CompanyProfileId,
    src.MigratedId,
    src.Name,
    src.LegacyNationalityId,
    src.SharePercentage,
    src.SourceCreatedByLegacyUserId,
    src.SourceCreatedAt,
    src.SourceUpdatedByLegacyUserId,
    src.SourceUpdatedAt
INTO #DirectorSource
FROM OPENROWSET(
    'MSOLEDBSQL',
    'Server=172.16.203.144;Database=Outsystems;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
    '
    SELECT
        CAST(src.ID AS BIGINT) AS MigratedId,
        CAST(src.COMPANYID AS BIGINT) AS LegacyCompanyId,
        NULLIF(LTRIM(RTRIM(src.[NAME])), N'''') AS [Name],
        CAST(src.NATIONALITY AS BIGINT) AS LegacyNationalityId,
        CAST(src.SHAREPERCENTAGE AS DECIMAL(18,2)) AS SharePercentage,
        CAST(src.CREATEDBY AS INT) AS SourceCreatedByLegacyUserId,
        CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= ''19000101'' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END AS SourceCreatedAt,
        CAST(src.MODIFIEDBY AS INT) AS SourceUpdatedByLegacyUserId,
        CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= ''19000101'' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END AS SourceUpdatedAt
    FROM dbo.OSUSR_1sw_BoardOfDirector src
    WHERE src.COMPANYID IS NOT NULL
    '
) AS src
INNER JOIN dbo.CompanyProfiles cp
    ON cp.MigratedId = src.LegacyCompanyId;

SELECT
    cp.Id AS CompanyProfileId,
    src.MigratedId,
    src.FileName,
    src.FileType,
    src.FileContent,
    src.SourceCreatedByLegacyUserId,
    src.SourceCreatedAt,
    src.SourceUpdatedByLegacyUserId,
    src.SourceUpdatedAt
INTO #AttachmentSource
FROM OPENROWSET(
    'MSOLEDBSQL',
    'Server=172.16.203.144;Database=Outsystems;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
    '
    SELECT
        CAST(src.ID AS BIGINT) AS MigratedId,
        CAST(src.COMPANYID AS BIGINT) AS LegacyCompanyId,
        NULLIF(LTRIM(RTRIM(src.FILENAME)), N'''') AS FileName,
        NULLIF(LTRIM(RTRIM(src.FILETYPE)), N'''') AS FileType,
        CAST(NULL AS VARBINARY(MAX)) AS FileContent,
        CAST(src.CREATEDBY AS INT) AS SourceCreatedByLegacyUserId,
        CASE WHEN src.CREATEDDATETIME IS NULL OR CONVERT(date, src.CREATEDDATETIME) <= ''19000101'' THEN NULL ELSE CONVERT(DATETIME2(3), src.CREATEDDATETIME) END AS SourceCreatedAt,
        CAST(src.MODIFIEDBY AS INT) AS SourceUpdatedByLegacyUserId,
        CASE WHEN src.MODIFIEDDATETIME IS NULL OR CONVERT(date, src.MODIFIEDDATETIME) <= ''19000101'' THEN NULL ELSE CONVERT(DATETIME2(3), src.MODIFIEDDATETIME) END AS SourceUpdatedAt
    FROM dbo.OSUSR_1sw_CompanyAttachmentDocument src
    WHERE src.COMPANYID IS NOT NULL
    '
) AS src
INNER JOIN dbo.CompanyProfiles cp
    ON cp.MigratedId = src.LegacyCompanyId;

MERGE dbo.CompanyAuthorizedPersons AS target
USING #AuthorizedSource AS source
    ON target.MigratedId = source.MigratedId
WHEN MATCHED THEN
    UPDATE SET
        target.CompanyProfileId = source.CompanyProfileId,
        target.FullName = source.FullName,
        target.Designation = source.Designation,
        target.LegacyIdentityTypeId = source.LegacyIdentityTypeId,
        target.IdentityNumber = source.IdentityNumber,
        target.Email = source.Email,
        target.TelephoneNo = source.TelephoneNo,
        target.LegacyUserId = source.LegacyUserId,
        target.IsDigiCertPaid = source.IsDigiCertPaid,
        target.IsCertified = source.IsCertified,
        target.IsPinVerified = source.IsPinVerified,
        target.IsDeletedInSource = source.IsDeletedInSource,
        target.LegacyTitleId = source.LegacyTitleId,
        target.LegacyCitizenshipId = source.LegacyCitizenshipId,
        target.CanEdit = source.CanEdit,
        target.SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
        target.SourceCreatedAt = source.SourceCreatedAt,
        target.SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
        target.SourceUpdatedAt = source.SourceUpdatedAt,
        target.LastSyncedAt = SYSUTCDATETIME(),
        target.TenantId = COALESCE(target.TenantId, @TenantId),
        target.IsDeleted = 0,
        target.DeletedAt = NULL,
        target.UpdatedAt = SYSDATETIMEOFFSET(),
        target.UpdatedBy = N'one-time-sql-sync'
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        Id, TenantId, CompanyProfileId, MigratedId, FullName, Designation, LegacyIdentityTypeId, IdentityNumber,
        Email, TelephoneNo, LegacyUserId, IsDigiCertPaid, IsCertified, IsPinVerified, IsDeletedInSource,
        LegacyTitleId, LegacyCitizenshipId, CanEdit, SourceCreatedByLegacyUserId, SourceCreatedAt,
        SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt
    )
    VALUES
    (
        NEWID(), @TenantId, source.CompanyProfileId, source.MigratedId, source.FullName, source.Designation, source.LegacyIdentityTypeId, source.IdentityNumber,
        source.Email, source.TelephoneNo, source.LegacyUserId, source.IsDigiCertPaid, source.IsCertified, source.IsPinVerified, source.IsDeletedInSource,
        source.LegacyTitleId, source.LegacyCitizenshipId, source.CanEdit, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt,
        source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSDATETIMEOFFSET(), N'one-time-sql-sync', NULL, NULL, 0, NULL
    )
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET
        target.IsDeleted = 1,
        target.DeletedAt = SYSDATETIMEOFFSET(),
        target.UpdatedAt = SYSDATETIMEOFFSET(),
        target.UpdatedBy = N'one-time-sql-sync';

MERGE dbo.CompanyBoardDirectors AS target
USING #DirectorSource AS source
    ON target.MigratedId = source.MigratedId
WHEN MATCHED THEN
    UPDATE SET
        target.CompanyProfileId = source.CompanyProfileId,
        target.Name = source.Name,
        target.LegacyNationalityId = source.LegacyNationalityId,
        target.SharePercentage = source.SharePercentage,
        target.SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
        target.SourceCreatedAt = source.SourceCreatedAt,
        target.SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
        target.SourceUpdatedAt = source.SourceUpdatedAt,
        target.LastSyncedAt = SYSUTCDATETIME(),
        target.TenantId = COALESCE(target.TenantId, @TenantId),
        target.IsDeleted = 0,
        target.DeletedAt = NULL,
        target.UpdatedAt = SYSDATETIMEOFFSET(),
        target.UpdatedBy = N'one-time-sql-sync'
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        Id, TenantId, CompanyProfileId, MigratedId, Name, LegacyNationalityId, SharePercentage,
        SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt,
        LastSyncedAt, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt
    )
    VALUES
    (
        NEWID(), @TenantId, source.CompanyProfileId, source.MigratedId, source.Name, source.LegacyNationalityId, source.SharePercentage,
        source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt,
        SYSUTCDATETIME(), SYSDATETIMEOFFSET(), N'one-time-sql-sync', NULL, NULL, 0, NULL
    )
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET
        target.IsDeleted = 1,
        target.DeletedAt = SYSDATETIMEOFFSET(),
        target.UpdatedAt = SYSDATETIMEOFFSET(),
        target.UpdatedBy = N'one-time-sql-sync';

MERGE dbo.CompanyAttachmentDocuments AS target
USING #AttachmentSource AS source
    ON target.MigratedId = source.MigratedId
WHEN MATCHED THEN
    UPDATE SET
        target.CompanyProfileId = source.CompanyProfileId,
        target.FileName = source.FileName,
        target.FileType = source.FileType,
        target.FileContent = source.FileContent,
        target.SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
        target.SourceCreatedAt = source.SourceCreatedAt,
        target.SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
        target.SourceUpdatedAt = source.SourceUpdatedAt,
        target.LastSyncedAt = SYSUTCDATETIME(),
        target.TenantId = COALESCE(target.TenantId, @TenantId),
        target.IsDeleted = 0,
        target.DeletedAt = NULL,
        target.UpdatedAt = SYSDATETIMEOFFSET(),
        target.UpdatedBy = N'one-time-sql-sync'
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        Id, TenantId, CompanyProfileId, MigratedId, FileName, FileType, FileContent,
        SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt,
        LastSyncedAt, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt
    )
    VALUES
    (
        NEWID(), @TenantId, source.CompanyProfileId, source.MigratedId, source.FileName, source.FileType, source.FileContent,
        source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt,
        SYSUTCDATETIME(), SYSDATETIMEOFFSET(), N'one-time-sql-sync', NULL, NULL, 0, NULL
    )
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET
        target.IsDeleted = 1,
        target.DeletedAt = SYSDATETIMEOFFSET(),
        target.UpdatedAt = SYSDATETIMEOFFSET(),
        target.UpdatedBy = N'one-time-sql-sync';

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.CompanyRelatedDataSyncStates
    WHERE SourceSystem = N'outsystems'
      AND SyncName = N'company-related-data'
)
BEGIN
    INSERT INTO dbo.CompanyRelatedDataSyncStates
    (
        Id, SourceSystem, SyncName, LastSourceCompanyId, LastStartedAt, LastCompletedAt,
        LastRunSucceeded, LastProcessedRows, LastRunMessage
    )
    VALUES
    (
        NEWID(), N'outsystems', N'company-related-data', NULL, SYSUTCDATETIME(), SYSUTCDATETIME(),
        1, 0, N'One-time SQL sync completed without attachment binary payloads.'
    );
END;
ELSE
BEGIN
    UPDATE dbo.CompanyRelatedDataSyncStates
    SET LastSourceCompanyId = NULL,
        LastStartedAt = SYSUTCDATETIME(),
        LastCompletedAt = SYSUTCDATETIME(),
        LastRunSucceeded = 1,
        LastProcessedRows = (SELECT COUNT(*) FROM #AuthorizedSource) + (SELECT COUNT(*) FROM #DirectorSource) + (SELECT COUNT(*) FROM #AttachmentSource),
        LastRunMessage = N'One-time SQL sync completed without attachment binary payloads.'
    WHERE SourceSystem = N'outsystems'
      AND SyncName = N'company-related-data';
END;

COMMIT TRANSACTION;

SELECT N'CompanyAuthorizedPersons' AS TableName, COUNT(*) AS [RowCount] FROM dbo.CompanyAuthorizedPersons WHERE IsDeleted = 0
UNION ALL
SELECT N'CompanyBoardDirectors' AS TableName, COUNT(*) AS [RowCount] FROM dbo.CompanyBoardDirectors WHERE IsDeleted = 0
UNION ALL
SELECT N'CompanyAttachmentDocuments' AS TableName, COUNT(*) AS [RowCount] FROM dbo.CompanyAttachmentDocuments WHERE IsDeleted = 0;
