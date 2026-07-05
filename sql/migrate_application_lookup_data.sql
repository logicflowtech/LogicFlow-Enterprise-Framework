SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

MERGE dbo.ApplicationCategories AS target
USING
(
    SELECT
        CAST(src.ID AS INT) AS LegacyId,
        NULLIF(LTRIM(RTRIM(src.LABEL)), N'') AS Name,
        NULLIF(LTRIM(RTRIM(src.CODE)), N'') AS Code,
        NULLIF(LTRIM(RTRIM(src.CODEKEY)), N'') AS CodeKey,
        CAST(src.CATEGORYNUMBER AS INT) AS CategoryNumber,
        CAST(src.[ORDER] AS INT) AS SortOrder,
        CAST(COALESCE(src.IS_ACTIVE, 0) AS BIT) AS IsActive
    FROM [172.16.203.144].[Outsystems].[dbo].[OSUSR_D22_APPLICATIONCATEGORY] AS src
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        Code = source.Code,
        CodeKey = source.CodeKey,
        CategoryNumber = source.CategoryNumber,
        SortOrder = source.SortOrder,
        IsActive = source.IsActive,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED BY TARGET THEN
    INSERT (LegacyId, Name, Code, CodeKey, CategoryNumber, SortOrder, IsActive, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (source.LegacyId, source.Name, source.Code, source.CodeKey, source.CategoryNumber, source.SortOrder, source.IsActive, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();

MERGE dbo.ApplicationFors AS target
USING
(
    SELECT
        CAST(src.ID AS INT) AS LegacyId,
        CAST(src.APPLICATIONCATEGORYID AS INT) AS LegacyApplicationCategoryId,
        categories.Id AS ApplicationCategoryId,
        NULLIF(LTRIM(RTRIM(src.LABEL)), N'') AS Name,
        NULLIF(LTRIM(RTRIM(src.LABELBM)), N'') AS NameBahasa,
        NULLIF(LTRIM(RTRIM(src.DESCRIPTION)), N'') AS Description,
        CAST(src.[ORDER] AS INT) AS SortOrder,
        CAST(COALESCE(src.IS_ACTIVE, 0) AS BIT) AS IsActive
    FROM [172.16.203.144].[Outsystems].[dbo].[OSUSR_D22_APPLICATIONFOR] AS src
    LEFT JOIN dbo.ApplicationCategories AS categories
        ON categories.LegacyId = src.APPLICATIONCATEGORYID
       AND categories.IsDeleted = 0
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        LegacyApplicationCategoryId = source.LegacyApplicationCategoryId,
        ApplicationCategoryId = source.ApplicationCategoryId,
        Name = source.Name,
        NameBahasa = source.NameBahasa,
        Description = source.Description,
        SortOrder = source.SortOrder,
        IsActive = source.IsActive,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED BY TARGET THEN
    INSERT (LegacyId, LegacyApplicationCategoryId, ApplicationCategoryId, Name, NameBahasa, Description, SortOrder, IsActive, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (source.LegacyId, source.LegacyApplicationCategoryId, source.ApplicationCategoryId, source.Name, source.NameBahasa, source.Description, source.SortOrder, source.IsActive, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();

MERGE dbo.ApplicationTypes AS target
USING
(
    SELECT
        CAST(src.ID AS INT) AS LegacyId,
        NULLIF(LTRIM(RTRIM(src.LABEL)), N'') AS Name,
        NULLIF(LTRIM(RTRIM(src.LABELBAHASA)), N'') AS NameBahasa,
        CAST(src.[ORDER] AS INT) AS SortOrder,
        CAST(COALESCE(src.IS_ACTIVE, 0) AS BIT) AS IsActive
    FROM [172.16.203.144].[Outsystems].[dbo].[OSUSR_D22_APPLICATIONTYPE] AS src
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        NameBahasa = source.NameBahasa,
        SortOrder = source.SortOrder,
        IsActive = source.IsActive,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED BY TARGET THEN
    INSERT (LegacyId, Name, NameBahasa, SortOrder, IsActive, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (source.LegacyId, source.Name, source.NameBahasa, source.SortOrder, source.IsActive, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();

MERGE dbo.ApplicationStatuses AS target
USING
(
    SELECT
        CAST(src.ID AS INT) AS LegacyId,
        NULLIF(LTRIM(RTRIM(src.LABEL)), N'') AS Name,
        NULLIF(LTRIM(RTRIM(src.CODEKEY)), N'') AS CodeKey,
        CAST(src.[ORDER] AS INT) AS SortOrder,
        CAST(COALESCE(src.ISACTIVE, 0) AS BIT) AS IsActive,
        CAST(src.APPLICATIONSTATUSMAINTYPEID AS INT) AS LegacyMainTypeId,
        CAST(src.APPLICATIONSTATUSAPPLICANTID AS INT) AS LegacyApplicantStatusId,
        CAST(src.APPLICATIONSTATUSCUSTOMID AS INT) AS LegacyCustomStatusId
    FROM [172.16.203.144].[Outsystems].[dbo].[OSUSR_D22_APPLICATIONSTATUS] AS src
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        CodeKey = source.CodeKey,
        SortOrder = source.SortOrder,
        IsActive = source.IsActive,
        LegacyMainTypeId = source.LegacyMainTypeId,
        LegacyApplicantStatusId = source.LegacyApplicantStatusId,
        LegacyCustomStatusId = source.LegacyCustomStatusId,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED BY TARGET THEN
    INSERT (LegacyId, Name, CodeKey, SortOrder, IsActive, LegacyMainTypeId, LegacyApplicantStatusId, LegacyCustomStatusId, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (source.LegacyId, source.Name, source.CodeKey, source.SortOrder, source.IsActive, source.LegacyMainTypeId, source.LegacyApplicantStatusId, source.LegacyCustomStatusId, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();

MERGE dbo.ApplicationCategoryFors AS target
USING
(
    SELECT
        CAST(src.ID AS BIGINT) AS LegacyId,
        categories.Id AS ApplicationCategoryId,
        appFors.Id AS ApplicationForId,
        CAST(src.APPLICATIONCATEGORYID AS INT) AS LegacyApplicationCategoryId,
        CAST(src.APPLICATIONFORID AS INT) AS LegacyApplicationForId,
        CAST(src.CREATEDBYID AS INT) AS SourceCreatedByLegacyUserId,
        src.CREATEDDATETIME AS SourceCreatedAt,
        CAST(src.MODIFIEDBYID AS INT) AS SourceUpdatedByLegacyUserId,
        src.MODIFIEDDATETIME AS SourceUpdatedAt
    FROM [172.16.203.144].[Outsystems].[dbo].[OSUSR_D22_APPLICATIONFORCATEGORY] AS src
    LEFT JOIN dbo.ApplicationCategories AS categories
        ON categories.LegacyId = src.APPLICATIONCATEGORYID
       AND categories.IsDeleted = 0
    LEFT JOIN dbo.ApplicationFors AS appFors
        ON appFors.LegacyId = src.APPLICATIONFORID
       AND appFors.IsDeleted = 0
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        ApplicationCategoryId = source.ApplicationCategoryId,
        ApplicationForId = source.ApplicationForId,
        LegacyApplicationCategoryId = source.LegacyApplicationCategoryId,
        LegacyApplicationForId = source.LegacyApplicationForId,
        SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
        SourceCreatedAt = source.SourceCreatedAt,
        SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
        SourceUpdatedAt = source.SourceUpdatedAt,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED BY TARGET THEN
    INSERT (LegacyId, ApplicationCategoryId, ApplicationForId, LegacyApplicationCategoryId, LegacyApplicationForId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (source.LegacyId, source.ApplicationCategoryId, source.ApplicationForId, source.LegacyApplicationCategoryId, source.LegacyApplicationForId, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();

MERGE dbo.ApplicationForTypes AS target
USING
(
    SELECT
        CAST(src.ID AS INT) AS LegacyId,
        appFors.Id AS ApplicationForId,
        appTypes.Id AS ApplicationTypeId,
        CAST(src.APPLICATIONFORID AS INT) AS LegacyApplicationForId,
        CAST(src.APPLICATIONTYPEID AS INT) AS LegacyApplicationTypeId,
        CAST(src.APPLICATIONFOREXEMPTIONTYPEI AS INT) AS LegacyApplicationForExemptionTypeId,
        CAST(src.CREATEDBYID AS INT) AS SourceCreatedByLegacyUserId,
        src.CREATEDDATETIME AS SourceCreatedAt,
        CAST(src.MODIFIEDBYID AS INT) AS SourceUpdatedByLegacyUserId,
        src.MODIFIEDDATETIME AS SourceUpdatedAt
    FROM [172.16.203.144].[Outsystems].[dbo].[OSUSR_D22_APPLICATIONFORAPPLICATIONTYPE] AS src
    LEFT JOIN dbo.ApplicationFors AS appFors
        ON appFors.LegacyId = src.APPLICATIONFORID
       AND appFors.IsDeleted = 0
    LEFT JOIN dbo.ApplicationTypes AS appTypes
        ON appTypes.LegacyId = src.APPLICATIONTYPEID
       AND appTypes.IsDeleted = 0
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        ApplicationForId = source.ApplicationForId,
        ApplicationTypeId = source.ApplicationTypeId,
        LegacyApplicationForId = source.LegacyApplicationForId,
        LegacyApplicationTypeId = source.LegacyApplicationTypeId,
        LegacyApplicationForExemptionTypeId = source.LegacyApplicationForExemptionTypeId,
        SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
        SourceCreatedAt = source.SourceCreatedAt,
        SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
        SourceUpdatedAt = source.SourceUpdatedAt,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED BY TARGET THEN
    INSERT (LegacyId, ApplicationForId, ApplicationTypeId, LegacyApplicationForId, LegacyApplicationTypeId, LegacyApplicationForExemptionTypeId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (source.LegacyId, source.ApplicationForId, source.ApplicationTypeId, source.LegacyApplicationForId, source.LegacyApplicationTypeId, source.LegacyApplicationForExemptionTypeId, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();

MERGE dbo.ApplicationSectors AS target
USING
(
    SELECT
        CAST(src.ID AS INT) AS LegacyId,
        NULLIF(LTRIM(RTRIM(src.LABEL)), N'') AS Name,
        CAST(src.[ORDER] AS INT) AS SortOrder,
        CAST(COALESCE(src.IS_ACTIVE, 0) AS BIT) AS IsActive,
        NULLIF(LTRIM(RTRIM(src.FILTERINGLABEL)), N'') AS FilteringLabel
    FROM [172.16.203.144].[Outsystems].[dbo].[OSUSR_D22_SECTOR] AS src
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        SortOrder = source.SortOrder,
        IsActive = source.IsActive,
        FilteringLabel = source.FilteringLabel,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED BY TARGET THEN
    INSERT (LegacyId, Name, SortOrder, IsActive, FilteringLabel, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (source.LegacyId, source.Name, source.SortOrder, source.IsActive, source.FilteringLabel, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();

MERGE dbo.ApplicationMainIndustries AS target
USING
(
    SELECT
        CAST(src.ID AS INT) AS LegacyId,
        NULLIF(LTRIM(RTRIM(src.LABEL)), N'') AS Name,
        CAST(src.[ORDER] AS INT) AS SortOrder,
        CAST(COALESCE(src.IS_ACTIVE, 0) AS BIT) AS IsActive,
        CAST(src.NAVIGATIONTYPEID AS INT) AS LegacyNavigationTypeId
    FROM [172.16.203.144].[Outsystems].[dbo].[OSUSR_D22_MAININDUSTRY] AS src
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        SortOrder = source.SortOrder,
        IsActive = source.IsActive,
        LegacyNavigationTypeId = source.LegacyNavigationTypeId,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED BY TARGET THEN
    INSERT (LegacyId, Name, SortOrder, IsActive, LegacyNavigationTypeId, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (source.LegacyId, source.Name, source.SortOrder, source.IsActive, source.LegacyNavigationTypeId, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();

MERGE dbo.ApplicationForSectors AS target
USING
(
    SELECT
        CAST(src.ID AS BIGINT) AS LegacyId,
        appFors.Id AS ApplicationForId,
        sectors.Id AS SectorId,
        CAST(src.APPLICATIONFORID AS INT) AS LegacyApplicationForId,
        CAST(src.SECTORID AS INT) AS LegacySectorId,
        CAST(src.CREATEDBYID AS INT) AS SourceCreatedByLegacyUserId,
        src.CREATEDDATETIME AS SourceCreatedAt,
        CAST(src.MODIFIEDBYID AS INT) AS SourceUpdatedByLegacyUserId,
        src.MODIFIEDDATETIME AS SourceUpdatedAt
    FROM [172.16.203.144].[Outsystems].[dbo].[OSUSR_D22_APPLICATIONFORSECTOR] AS src
    LEFT JOIN dbo.ApplicationFors AS appFors
        ON appFors.LegacyId = src.APPLICATIONFORID
       AND appFors.IsDeleted = 0
    LEFT JOIN dbo.ApplicationSectors AS sectors
        ON sectors.LegacyId = src.SECTORID
       AND sectors.IsDeleted = 0
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        ApplicationForId = source.ApplicationForId,
        SectorId = source.SectorId,
        LegacyApplicationForId = source.LegacyApplicationForId,
        LegacySectorId = source.LegacySectorId,
        SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
        SourceCreatedAt = source.SourceCreatedAt,
        SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
        SourceUpdatedAt = source.SourceUpdatedAt,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED BY TARGET THEN
    INSERT (LegacyId, ApplicationForId, SectorId, LegacyApplicationForId, LegacySectorId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (source.LegacyId, source.ApplicationForId, source.SectorId, source.LegacyApplicationForId, source.LegacySectorId, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();

MERGE dbo.ApplicationSectorIndustries AS target
USING
(
    SELECT
        CAST(src.ID AS BIGINT) AS LegacyId,
        sectors.Id AS SectorId,
        industries.Id AS MainIndustryId,
        CAST(src.SECTORID AS INT) AS LegacySectorId,
        CAST(src.MAININDUSTRYID AS INT) AS LegacyMainIndustryId,
        CAST(src.CREATEDBYUSERID AS INT) AS SourceCreatedByLegacyUserId,
        src.CREATEDDATETIME AS SourceCreatedAt,
        CAST(src.MODIFIEDBYUSERID AS INT) AS SourceUpdatedByLegacyUserId,
        src.MODIFIEDDATETIME AS SourceUpdatedAt
    FROM [172.16.203.144].[Outsystems].[dbo].[OSUSR_D22_APPLICATIONSECTORINDUSTRY] AS src
    LEFT JOIN dbo.ApplicationSectors AS sectors
        ON sectors.LegacyId = src.SECTORID
       AND sectors.IsDeleted = 0
    LEFT JOIN dbo.ApplicationMainIndustries AS industries
        ON industries.LegacyId = src.MAININDUSTRYID
       AND industries.IsDeleted = 0
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        SectorId = source.SectorId,
        MainIndustryId = source.MainIndustryId,
        LegacySectorId = source.LegacySectorId,
        LegacyMainIndustryId = source.LegacyMainIndustryId,
        SourceCreatedByLegacyUserId = source.SourceCreatedByLegacyUserId,
        SourceCreatedAt = source.SourceCreatedAt,
        SourceUpdatedByLegacyUserId = source.SourceUpdatedByLegacyUserId,
        SourceUpdatedAt = source.SourceUpdatedAt,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED BY TARGET THEN
    INSERT (LegacyId, SectorId, MainIndustryId, LegacySectorId, LegacyMainIndustryId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (source.LegacyId, source.SectorId, source.MainIndustryId, source.LegacySectorId, source.LegacyMainIndustryId, source.SourceCreatedByLegacyUserId, source.SourceCreatedAt, source.SourceUpdatedByLegacyUserId, source.SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
WHEN NOT MATCHED BY SOURCE THEN
    UPDATE SET IsDeleted = 1, DeletedAt = SYSDATETIMEOFFSET();

COMMIT TRANSACTION;
