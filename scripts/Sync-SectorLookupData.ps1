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

    return ,$table
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
        [void]$command.Parameters.AddWithValue($entry.Key, (Get-DbValue $entry.Value))
    }

    [void]$command.ExecuteNonQuery()
}

$sourceConnectionString = "Server=$SourceServer;Database=$SourceDatabase;User ID=$SourceUser;Password=$SourcePassword;TrustServerCertificate=True;Encrypt=False"
$localConnectionString = "Server=$LocalServer;Database=$LocalDatabase;Integrated Security=True;TrustServerCertificate=True;Encrypt=False"

$sectorRows = Load-DataTable -ConnectionString $sourceConnectionString -Query @"
SELECT
    CAST(ID AS INT) AS LegacyId,
    NULLIF(LTRIM(RTRIM(LABEL)), N'') AS Name,
    CAST([ORDER] AS INT) AS SortOrder,
    CAST(COALESCE(IS_ACTIVE, 0) AS BIT) AS IsActive,
    NULLIF(LTRIM(RTRIM(FILTERINGLABEL)), N'') AS FilteringLabel
FROM dbo.OSUSR_D22_SECTOR
ORDER BY ID;
"@

$industryRows = Load-DataTable -ConnectionString $sourceConnectionString -Query @"
SELECT
    CAST(ID AS INT) AS LegacyId,
    NULLIF(LTRIM(RTRIM(LABEL)), N'') AS Name,
    CAST([ORDER] AS INT) AS SortOrder,
    CAST(COALESCE(IS_ACTIVE, 0) AS BIT) AS IsActive,
    CAST(NAVIGATIONTYPEID AS INT) AS LegacyNavigationTypeId
FROM dbo.OSUSR_D22_MAININDUSTRY
ORDER BY ID;
"@

$forSectorRows = Load-DataTable -ConnectionString $sourceConnectionString -Query @"
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

$sectorIndustryRows = Load-DataTable -ConnectionString $sourceConnectionString -Query @"
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

$localConnection = New-Object System.Data.SqlClient.SqlConnection $localConnectionString
try {
    $localConnection.Open()
    $transaction = $localConnection.BeginTransaction()

    try {
        foreach ($row in $sectorRows.Select()) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationSectors AS target
USING (SELECT @LegacyId AS LegacyId) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        Name = @Name,
        SortOrder = @SortOrder,
        IsActive = @IsActive,
        FilteringLabel = @FilteringLabel,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, Name, SortOrder, IsActive, FilteringLabel, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, @Name, @SortOrder, @IsActive, @FilteringLabel, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.Item("LegacyId")
                "@Name" = $row.Item("Name")
                "@SortOrder" = $row.Item("SortOrder")
                "@IsActive" = $row.Item("IsActive")
                "@FilteringLabel" = $row.Item("FilteringLabel")
            }
        }

        foreach ($row in $industryRows.Select()) {
            Invoke-Upsert -Connection $localConnection -Transaction $transaction -Sql @"
MERGE dbo.ApplicationMainIndustries AS target
USING (SELECT @LegacyId AS LegacyId) AS source
ON target.LegacyId = source.LegacyId
WHEN MATCHED THEN
    UPDATE SET
        Name = @Name,
        SortOrder = @SortOrder,
        IsActive = @IsActive,
        LegacyNavigationTypeId = @LegacyNavigationTypeId,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, Name, SortOrder, IsActive, LegacyNavigationTypeId, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, @Name, @SortOrder, @IsActive, @LegacyNavigationTypeId, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.Item("LegacyId")
                "@Name" = $row.Item("Name")
                "@SortOrder" = $row.Item("SortOrder")
                "@IsActive" = $row.Item("IsActive")
                "@LegacyNavigationTypeId" = $row.Item("LegacyNavigationTypeId")
            }
        }

        foreach ($row in $forSectorRows.Select()) {
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
    UPDATE SET
        ApplicationForId = source.ApplicationForId,
        SectorId = source.SectorId,
        LegacyApplicationForId = @LegacyApplicationForId,
        LegacySectorId = @LegacySectorId,
        SourceCreatedByLegacyUserId = @SourceCreatedByLegacyUserId,
        SourceCreatedAt = @SourceCreatedAt,
        SourceUpdatedByLegacyUserId = @SourceUpdatedByLegacyUserId,
        SourceUpdatedAt = @SourceUpdatedAt,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, ApplicationForId, SectorId, LegacyApplicationForId, LegacySectorId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, source.ApplicationForId, source.SectorId, @LegacyApplicationForId, @LegacySectorId, @SourceCreatedByLegacyUserId, @SourceCreatedAt, @SourceUpdatedByLegacyUserId, @SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.Item("LegacyId")
                "@LegacyApplicationForId" = $row.Item("LegacyApplicationForId")
                "@LegacySectorId" = $row.Item("LegacySectorId")
                "@SourceCreatedByLegacyUserId" = $row.Item("SourceCreatedByLegacyUserId")
                "@SourceCreatedAt" = $row.Item("SourceCreatedAt")
                "@SourceUpdatedByLegacyUserId" = $row.Item("SourceUpdatedByLegacyUserId")
                "@SourceUpdatedAt" = $row.Item("SourceUpdatedAt")
            }
        }

        foreach ($row in $sectorIndustryRows.Select()) {
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
    UPDATE SET
        SectorId = source.SectorId,
        MainIndustryId = source.MainIndustryId,
        LegacySectorId = @LegacySectorId,
        LegacyMainIndustryId = @LegacyMainIndustryId,
        SourceCreatedByLegacyUserId = @SourceCreatedByLegacyUserId,
        SourceCreatedAt = @SourceCreatedAt,
        SourceUpdatedByLegacyUserId = @SourceUpdatedByLegacyUserId,
        SourceUpdatedAt = @SourceUpdatedAt,
        LastSyncedAt = SYSUTCDATETIME(),
        IsDeleted = 0,
        DeletedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (LegacyId, SectorId, MainIndustryId, LegacySectorId, LegacyMainIndustryId, SourceCreatedByLegacyUserId, SourceCreatedAt, SourceUpdatedByLegacyUserId, SourceUpdatedAt, LastSyncedAt, CreatedAt, IsDeleted)
    VALUES (@LegacyId, source.SectorId, source.MainIndustryId, @LegacySectorId, @LegacyMainIndustryId, @SourceCreatedByLegacyUserId, @SourceCreatedAt, @SourceUpdatedByLegacyUserId, @SourceUpdatedAt, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
"@ -Parameters @{
                "@LegacyId" = $row.Item("LegacyId")
                "@LegacySectorId" = $row.Item("LegacySectorId")
                "@LegacyMainIndustryId" = $row.Item("LegacyMainIndustryId")
                "@SourceCreatedByLegacyUserId" = $row.Item("SourceCreatedByLegacyUserId")
                "@SourceCreatedAt" = $row.Item("SourceCreatedAt")
                "@SourceUpdatedByLegacyUserId" = $row.Item("SourceUpdatedByLegacyUserId")
                "@SourceUpdatedAt" = $row.Item("SourceUpdatedAt")
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

Write-Host "Sector lookup migration completed successfully."
