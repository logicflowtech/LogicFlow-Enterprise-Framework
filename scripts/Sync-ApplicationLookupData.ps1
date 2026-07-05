param(
    [string]$SourceServer = "172.16.203.144",
    [string]$SourceDatabase = "Outsystems",
    [string]$SourceUser = "sa",
    [string]$SourcePassword,
    [string]$LocalServer = "localhost",
    [string]$LocalDatabase = "LogicFlowEnterpriseFrameworkDb"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourcePassword)) {
    throw "SourcePassword is required."
}

function Get-DbValue {
    param([object]$Value)

    if ($null -eq $Value -or $Value -is [System.DBNull]) {
        return [DBNull]::Value
    }

    return $Value
}

function Load-DataTable {
    param(
        [string]$ConnectionString,
        [string]$Query
    )

    $table = New-Object System.Data.DataTable
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        $command.CommandTimeout = 120
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
        [void]$adapter.Fill($table)
    }
    finally {
        $connection.Dispose()
    }

    return $table
}

function Invoke-Upsert {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [System.Data.SqlClient.SqlTransaction]$Transaction,
        [string]$Sql,
        [hashtable]$Parameters
    )

    $command = $Connection.CreateCommand()
    $command.Transaction = $Transaction
    $command.CommandText = $Sql
    $command.CommandTimeout = 120

    foreach ($entry in $Parameters.GetEnumerator()) {
        $parameter = $command.Parameters.AddWithValue($entry.Key, (Get-DbValue $entry.Value))
        if ($parameter.Value -is [string] -and $parameter.Value.Length -gt 4000) {
            $parameter.SqlDbType = [System.Data.SqlDbType]::NVarChar
            $parameter.Size = -1
        }
    }

    [void]$command.ExecuteNonQuery()
}

$sourceConnectionString = "Server=$SourceServer;Database=$SourceDatabase;User ID=$SourceUser;Password=$SourcePassword;TrustServerCertificate=True;Encrypt=False"
$localConnectionString = "Server=$LocalServer;Database=$LocalDatabase;Integrated Security=True;TrustServerCertificate=True;Encrypt=False"

$queries = @{
    ApplicationCategories = @"
SELECT
    CAST(ID AS INT) AS LegacyId,
    NULLIF(LTRIM(RTRIM(LABEL)), N'') AS Name,
    NULLIF(LTRIM(RTRIM(CODE)), N'') AS Code,
    NULLIF(LTRIM(RTRIM(CODEKEY)), N'') AS CodeKey,
    CAST(CATEGORYNUMBER AS INT) AS CategoryNumber,
    CAST([ORDER] AS INT) AS SortOrder,
    CAST(COALESCE(IS_ACTIVE, 0) AS BIT) AS IsActive
FROM dbo.OSUSR_D22_APPLICATIONCATEGORY
ORDER BY ID;
"@
    ApplicationFors = @"
SELECT
    CAST(ID AS INT) AS LegacyId,
    CAST(APPLICATIONCATEGORYID AS INT) AS LegacyApplicationCategoryId,
    NULLIF(LTRIM(RTRIM(LABEL)), N'') AS Name,
    NULLIF(LTRIM(RTRIM(LABELBM)), N'') AS NameBahasa,
    NULLIF(LTRIM(RTRIM(DESCRIPTION)), N'') AS Description,
    CAST([ORDER] AS INT) AS SortOrder,
    CAST(COALESCE(IS_ACTIVE, 0) AS BIT) AS IsActive
FROM dbo.OSUSR_D22_APPLICATIONFOR
ORDER BY ID;
"@
    ApplicationTypes = @"
SELECT
    CAST(ID AS INT) AS LegacyId,
    NULLIF(LTRIM(RTRIM(LABEL)), N'') AS Name,
    NULLIF(LTRIM(RTRIM(LABELBAHASA)), N'') AS NameBahasa,
    CAST([ORDER] AS INT) AS SortOrder,
    CAST(COALESCE(IS_ACTIVE, 0) AS BIT) AS IsActive
FROM dbo.OSUSR_D22_APPLICATIONTYPE
ORDER BY ID;
"@
    ApplicationStatuses = @"
SELECT
    CAST(ID AS INT) AS LegacyId,
    NULLIF(LTRIM(RTRIM(LABEL)), N'') AS Name,
    NULLIF(LTRIM(RTRIM(CODEKEY)), N'') AS CodeKey,
    CAST([ORDER] AS INT) AS SortOrder,
    CAST(COALESCE(ISACTIVE, 0) AS BIT) AS IsActive,
    CAST(APPLICATIONSTATUSMAINTYPEID AS INT) AS LegacyMainTypeId,
    CAST(APPLICATIONSTATUSAPPLICANTID AS INT) AS LegacyApplicantStatusId,
    CAST(APPLICATIONSTATUSCUSTOMID AS INT) AS LegacyCustomStatusId
FROM dbo.OSUSR_D22_APPLICATIONSTATUS
ORDER BY ID;
"@
    ApplicationCategoryFors = @"
SELECT
    CAST(ID AS BIGINT) AS LegacyId,
    CAST(APPLICATIONCATEGORYID AS INT) AS LegacyApplicationCategoryId,
    CAST(APPLICATIONFORID AS INT) AS LegacyApplicationForId,
    CAST(CREATEDBYID AS INT) AS SourceCreatedByLegacyUserId,
    CREATEDDATETIME AS SourceCreatedAt,
    CAST(MODIFIEDBYID AS INT) AS SourceUpdatedByLegacyUserId,
    MODIFIEDDATETIME AS SourceUpdatedAt
FROM dbo.OSUSR_D22_APPLICATIONFORCATEGORY
ORDER BY ID;
"@
    ApplicationForTypes = @"
SELECT
    CAST(ID AS INT) AS LegacyId,
    CAST(APPLICATIONFORID AS INT) AS LegacyApplicationForId,
    CAST(APPLICATIONTYPEID AS INT) AS LegacyApplicationTypeId,
    CAST(APPLICATIONFOREXEMPTIONTYPEI AS INT) AS LegacyApplicationForExemptionTypeId,
    CAST(CREATEDBYID AS INT) AS SourceCreatedByLegacyUserId,
    CREATEDDATETIME AS SourceCreatedAt,
    CAST(MODIFIEDBYID AS INT) AS SourceUpdatedByLegacyUserId,
    MODIFIEDDATETIME AS SourceUpdatedAt
FROM dbo.OSUSR_D22_APPLICATIONFORAPPLICATIONTYPE
ORDER BY ID;
"@
    ApplicationSectors = @"
SELECT
    CAST(ID AS INT) AS LegacyId,
    NULLIF(LTRIM(RTRIM(LABEL)), N'') AS Name,
    CAST([ORDER] AS INT) AS SortOrder,
    CAST(COALESCE(IS_ACTIVE, 0) AS BIT) AS IsActive,
    NULLIF(LTRIM(RTRIM(FILTERINGLABEL)), N'') AS FilteringLabel
FROM dbo.OSUSR_D22_SECTOR
ORDER BY ID;
"@
    ApplicationMainIndustries = @"
SELECT
    CAST(ID AS INT) AS LegacyId,
    NULLIF(LTRIM(RTRIM(LABEL)), N'') AS Name,
    CAST([ORDER] AS INT) AS SortOrder,
    CAST(COALESCE(IS_ACTIVE, 0) AS BIT) AS IsActive,
    CAST(NAVIGATIONTYPEID AS INT) AS LegacyNavigationTypeId
FROM dbo.OSUSR_D22_MAININDUSTRY
ORDER BY ID;
"@
    ApplicationForSectors = @"
SELECT
    CAST(ID AS BIGINT) AS LegacyId,
    CAST(APPLICATIONFORID AS INT) AS LegacyApplicationForId,
    CAST(SECTORID AS INT) AS LegacySectorId,
    CAST(CREATEDBYID AS INT) AS SourceCreatedByLegacyUserId,
    CREATEDDATETIME AS SourceCreatedAt,
    CAST(MODIFIEDBYID AS INT) AS SourceUpdatedByLegacyUserId,
    MODIFIEDDATETIME AS SourceUpdatedAt
FROM dbo.OSUSR_D22_APPLICATIONFORSECTOR
ORDER BY ID;
"@
    ApplicationSectorIndustries = @"
SELECT
    CAST(ID AS BIGINT) AS LegacyId,
    CAST(SECTORID AS INT) AS LegacySectorId,
    CAST(MAININDUSTRYID AS INT) AS LegacyMainIndustryId,
    CAST(CREATEDBYUSERID AS INT) AS SourceCreatedByLegacyUserId,
    CREATEDDATETIME AS SourceCreatedAt,
    CAST(MODIFIEDBYUSERID AS INT) AS SourceUpdatedByLegacyUserId,
    MODIFIEDDATETIME AS SourceUpdatedAt
FROM dbo.OSUSR_D22_APPLICATIONSECTORINDUSTRY
ORDER BY ID;
"@
}

$data = @{}
foreach ($entry in $queries.GetEnumerator()) {
    $data[$entry.Key] = Load-DataTable -ConnectionString $sourceConnectionString -Query $entry.Value
}

$localConnection = New-Object System.Data.SqlClient.SqlConnection $localConnectionString
try {
    $localConnection.Open()
    $transaction = $localConnection.BeginTransaction()

    try {
        foreach ($row in $data.ApplicationCategories.Rows) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationCategories AS target
USING (SELECT @LegacyId AS LegacyId) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET Name = @Name, Code = @Code, CodeKey = @CodeKey, CategoryNumber = @CategoryNumber, SortOrder = @SortOrder, IsActive = @IsActive, LastSyncedAt = SYSUTCDATETIME(), IsDeleted = 0, DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, Name, Code, CodeKey, CategoryNumber, SortOrder, IsActive, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, @Name, @Code, @CodeKey, @CategoryNumber, @SortOrder, @IsActive, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.LegacyId
                "@Name" = $row.Name
                "@Code" = $row.Code
                "@CodeKey" = $row.CodeKey
                "@CategoryNumber" = $row.CategoryNumber
                "@SortOrder" = $row.SortOrder
                "@IsActive" = $row.IsActive
            }
        }

        foreach ($row in $data.ApplicationFors.Rows) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationFors AS target
USING
(
    SELECT
        @LegacyId AS LegacyId,
        @LegacyApplicationCategoryId AS LegacyApplicationCategoryId,
        (SELECT TOP 1 Id FROM dbo.ApplicationCategories WHERE LegacyId = @LegacyApplicationCategoryId AND IsDeleted = 0) AS ApplicationCategoryId
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET LegacyApplicationCategoryId = source.LegacyApplicationCategoryId, ApplicationCategoryId = source.ApplicationCategoryId, Name = @Name, NameBahasa = @NameBahasa, Description = @Description, SortOrder = @SortOrder, IsActive = @IsActive, LastSyncedAt = SYSUTCDATETIME(), IsDeleted = 0, DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, LegacyApplicationCategoryId, ApplicationCategoryId, Name, NameBahasa, Description, SortOrder, IsActive, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, source.LegacyApplicationCategoryId, source.ApplicationCategoryId, @Name, @NameBahasa, @Description, @SortOrder, @IsActive, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.LegacyId
                "@LegacyApplicationCategoryId" = $row.LegacyApplicationCategoryId
                "@Name" = $row.Name
                "@NameBahasa" = $row.NameBahasa
                "@Description" = $row.Description
                "@SortOrder" = $row.SortOrder
                "@IsActive" = $row.IsActive
            }
        }

        foreach ($row in $data.ApplicationTypes.Rows) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationTypes AS target
USING (SELECT @LegacyId AS LegacyId) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET Name = @Name, NameBahasa = @NameBahasa, SortOrder = @SortOrder, IsActive = @IsActive, LastSyncedAt = SYSUTCDATETIME(), IsDeleted = 0, DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, Name, NameBahasa, SortOrder, IsActive, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, @Name, @NameBahasa, @SortOrder, @IsActive, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.LegacyId
                "@Name" = $row.Name
                "@NameBahasa" = $row.NameBahasa
                "@SortOrder" = $row.SortOrder
                "@IsActive" = $row.IsActive
            }
        }

        foreach ($row in $data.ApplicationStatuses.Rows) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationStatuses AS target
USING (SELECT @LegacyId AS LegacyId) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET Name = @Name, CodeKey = @CodeKey, SortOrder = @SortOrder, IsActive = @IsActive, LegacyMainTypeId = @LegacyMainTypeId, LegacyApplicantStatusId = @LegacyApplicantStatusId, LegacyCustomStatusId = @LegacyCustomStatusId, LastSyncedAt = SYSUTCDATETIME(), IsDeleted = 0, DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, Name, CodeKey, SortOrder, IsActive, LegacyMainTypeId, LegacyApplicantStatusId, LegacyCustomStatusId, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, @Name, @CodeKey, @SortOrder, @IsActive, @LegacyMainTypeId, @LegacyApplicantStatusId, @LegacyCustomStatusId, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.LegacyId
                "@Name" = $row.Name
                "@CodeKey" = $row.CodeKey
                "@SortOrder" = $row.SortOrder
                "@IsActive" = $row.IsActive
                "@LegacyMainTypeId" = $row.LegacyMainTypeId
                "@LegacyApplicantStatusId" = $row.LegacyApplicantStatusId
                "@LegacyCustomStatusId" = $row.LegacyCustomStatusId
            }
        }

        foreach ($row in $data.ApplicationCategoryFors.Rows) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationCategoryFors AS target
USING
(
    SELECT
        @LegacyId AS LegacyId,
        (SELECT TOP 1 Id FROM dbo.ApplicationCategories WHERE LegacyId = @LegacyApplicationCategoryId AND IsDeleted = 0) AS ApplicationCategoryId,
        (SELECT TOP 1 Id FROM dbo.ApplicationFors WHERE LegacyId = @LegacyApplicationForId AND IsDeleted = 0) AS ApplicationForId
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET ApplicationCategoryId = source.ApplicationCategoryId, ApplicationForId = source.ApplicationForId, LegacyApplicationCategoryId = @LegacyApplicationCategoryId, LegacyApplicationForId = @LegacyApplicationForId, SourceCreatedByLegacyUserId = @SourceCreatedByLegacyUserId, SourceCreatedAt = @SourceCreatedAt, SourceUpdatedByLegacyUserId = @SourceUpdatedByLegacyUserId, SourceUpdatedAt = @SourceUpdatedAt, LastSyncedAt = SYSUTCDATETIME(), IsDeleted = 0, DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, ApplicationCategoryId, ApplicationForId, LegacyApplicationCategoryId, LegacyApplicationForId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, source.ApplicationCategoryId, source.ApplicationForId, @LegacyApplicationCategoryId, @LegacyApplicationForId, @SourceCreatedByLegacyUserId, @SourceCreatedAt, @SourceUpdatedByLegacyUserId, @SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.LegacyId
                "@LegacyApplicationCategoryId" = $row.LegacyApplicationCategoryId
                "@LegacyApplicationForId" = $row.LegacyApplicationForId
                "@SourceCreatedByLegacyUserId" = $row.SourceCreatedByLegacyUserId
                "@SourceCreatedAt" = $row.SourceCreatedAt
                "@SourceUpdatedByLegacyUserId" = $row.SourceUpdatedByLegacyUserId
                "@SourceUpdatedAt" = $row.SourceUpdatedAt
            }
        }

        foreach ($row in $data.ApplicationForTypes.Rows) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationForTypes AS target
USING
(
    SELECT
        @LegacyId AS LegacyId,
        (SELECT TOP 1 Id FROM dbo.ApplicationFors WHERE LegacyId = @LegacyApplicationForId AND IsDeleted = 0) AS ApplicationForId,
        (SELECT TOP 1 Id FROM dbo.ApplicationTypes WHERE LegacyId = @LegacyApplicationTypeId AND IsDeleted = 0) AS ApplicationTypeId
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET ApplicationForId = source.ApplicationForId, ApplicationTypeId = source.ApplicationTypeId, LegacyApplicationForId = @LegacyApplicationForId, LegacyApplicationTypeId = @LegacyApplicationTypeId, LegacyApplicationForExemptionTypeId = @LegacyApplicationForExemptionTypeId, SourceCreatedByLegacyUserId = @SourceCreatedByLegacyUserId, SourceCreatedAt = @SourceCreatedAt, SourceUpdatedByLegacyUserId = @SourceUpdatedByLegacyUserId, SourceUpdatedAt = @SourceUpdatedAt, LastSyncedAt = SYSUTCDATETIME(), IsDeleted = 0, DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, ApplicationForId, ApplicationTypeId, LegacyApplicationForId, LegacyApplicationTypeId, LegacyApplicationForExemptionTypeId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, source.ApplicationForId, source.ApplicationTypeId, @LegacyApplicationForId, @LegacyApplicationTypeId, @LegacyApplicationForExemptionTypeId, @SourceCreatedByLegacyUserId, @SourceCreatedAt, @SourceUpdatedByLegacyUserId, @SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.LegacyId
                "@LegacyApplicationForId" = $row.LegacyApplicationForId
                "@LegacyApplicationTypeId" = $row.LegacyApplicationTypeId
                "@LegacyApplicationForExemptionTypeId" = $row.LegacyApplicationForExemptionTypeId
                "@SourceCreatedByLegacyUserId" = $row.SourceCreatedByLegacyUserId
                "@SourceCreatedAt" = $row.SourceCreatedAt
                "@SourceUpdatedByLegacyUserId" = $row.SourceUpdatedByLegacyUserId
                "@SourceUpdatedAt" = $row.SourceUpdatedAt
            }
        }

        foreach ($row in $data.ApplicationSectors.Rows) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationSectors AS target
USING (SELECT @LegacyId AS LegacyId) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET Name = @Name, SortOrder = @SortOrder, IsActive = @IsActive, FilteringLabel = @FilteringLabel, LastSyncedAt = SYSUTCDATETIME(), IsDeleted = 0, DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, Name, SortOrder, IsActive, FilteringLabel, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, @Name, @SortOrder, @IsActive, @FilteringLabel, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.LegacyId
                "@Name" = $row.Name
                "@SortOrder" = $row.SortOrder
                "@IsActive" = $row.IsActive
                "@FilteringLabel" = $row.FilteringLabel
            }
        }

        foreach ($row in $data.ApplicationMainIndustries.Rows) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationMainIndustries AS target
USING (SELECT @LegacyId AS LegacyId) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET Name = @Name, SortOrder = @SortOrder, IsActive = @IsActive, LegacyNavigationTypeId = @LegacyNavigationTypeId, LastSyncedAt = SYSUTCDATETIME(), IsDeleted = 0, DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, Name, SortOrder, IsActive, LegacyNavigationTypeId, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, @Name, @SortOrder, @IsActive, @LegacyNavigationTypeId, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.LegacyId
                "@Name" = $row.Name
                "@SortOrder" = $row.SortOrder
                "@IsActive" = $row.IsActive
                "@LegacyNavigationTypeId" = $row.LegacyNavigationTypeId
            }
        }

        foreach ($row in $data.ApplicationForSectors.Rows) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationForSectors AS target
USING
(
    SELECT
        @LegacyId AS LegacyId,
        (SELECT TOP 1 Id FROM dbo.ApplicationFors WHERE LegacyId = @LegacyApplicationForId AND IsDeleted = 0) AS ApplicationForId,
        (SELECT TOP 1 Id FROM dbo.ApplicationSectors WHERE LegacyId = @LegacySectorId AND IsDeleted = 0) AS SectorId
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET ApplicationForId = source.ApplicationForId, SectorId = source.SectorId, LegacyApplicationForId = @LegacyApplicationForId, LegacySectorId = @LegacySectorId, SourceCreatedByLegacyUserId = @SourceCreatedByLegacyUserId, SourceCreatedAt = @SourceCreatedAt, SourceUpdatedByLegacyUserId = @SourceUpdatedByLegacyUserId, SourceUpdatedAt = @SourceUpdatedAt, LastSyncedAt = SYSUTCDATETIME(), IsDeleted = 0, DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, ApplicationForId, SectorId, LegacyApplicationForId, LegacySectorId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, source.ApplicationForId, source.SectorId, @LegacyApplicationForId, @LegacySectorId, @SourceCreatedByLegacyUserId, @SourceCreatedAt, @SourceUpdatedByLegacyUserId, @SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.LegacyId
                "@LegacyApplicationForId" = $row.LegacyApplicationForId
                "@LegacySectorId" = $row.LegacySectorId
                "@SourceCreatedByLegacyUserId" = $row.SourceCreatedByLegacyUserId
                "@SourceCreatedAt" = $row.SourceCreatedAt
                "@SourceUpdatedByLegacyUserId" = $row.SourceUpdatedByLegacyUserId
                "@SourceUpdatedAt" = $row.SourceUpdatedAt
            }
        }

        foreach ($row in $data.ApplicationSectorIndustries.Rows) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationSectorIndustries AS target
USING
(
    SELECT
        @LegacyId AS LegacyId,
        (SELECT TOP 1 Id FROM dbo.ApplicationSectors WHERE LegacyId = @LegacySectorId AND IsDeleted = 0) AS SectorId,
        (SELECT TOP 1 Id FROM dbo.ApplicationMainIndustries WHERE LegacyId = @LegacyMainIndustryId AND IsDeleted = 0) AS MainIndustryId
) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET SectorId = source.SectorId, MainIndustryId = source.MainIndustryId, LegacySectorId = @LegacySectorId, LegacyMainIndustryId = @LegacyMainIndustryId, SourceCreatedByLegacyUserId = @SourceCreatedByLegacyUserId, SourceCreatedAt = @SourceCreatedAt, SourceUpdatedByLegacyUserId = @SourceUpdatedByLegacyUserId, SourceUpdatedAt = @SourceUpdatedAt, LastSyncedAt = SYSUTCDATETIME(), IsDeleted = 0, DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, SectorId, MainIndustryId, LegacySectorId, LegacyMainIndustryId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, source.SectorId, source.MainIndustryId, @LegacySectorId, @LegacyMainIndustryId, @SourceCreatedByLegacyUserId, @SourceCreatedAt, @SourceUpdatedByLegacyUserId, @SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.LegacyId
                "@LegacySectorId" = $row.LegacySectorId
                "@LegacyMainIndustryId" = $row.LegacyMainIndustryId
                "@SourceCreatedByLegacyUserId" = $row.SourceCreatedByLegacyUserId
                "@SourceCreatedAt" = $row.SourceCreatedAt
                "@SourceUpdatedByLegacyUserId" = $row.SourceUpdatedByLegacyUserId
                "@SourceUpdatedAt" = $row.SourceUpdatedAt
            }
        }

        $transaction.Commit()
    }
    catch {
        $transaction.Rollback()
        throw
    }
}
finally {
    $localConnection.Dispose()
}

Write-Host "Application lookup migration completed successfully."
