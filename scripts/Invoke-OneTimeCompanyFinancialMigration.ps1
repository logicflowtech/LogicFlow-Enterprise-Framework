$ErrorActionPreference = "Stop"

$sourceConnectionString = "Server=172.16.203.144;Database=Outsystems;User ID=sa;Password=M1D@db@dM1N;TrustServerCertificate=True;"
$targetConnectionString = "Server=127.0.0.1;Database=LogicFlowEnterpriseFrameworkDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"

Add-Type -AssemblyName System.Data
function New-Connection([string]$connectionString) {
    $connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    $connection.Open()
    return $connection
}

function Invoke-QueryTable($connection, [string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 300
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    $table = [System.Data.DataTable]::new()
    [void]$adapter.Fill($table)
    return $table
}

function Invoke-NonQuery($connection, [string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 300
    [void]$command.ExecuteNonQuery()
}

function Write-BulkTable($connection, [string]$destinationTable, $table) {
    if ($table.Rows.Count -eq 0) {
        Write-Host "Skipping $destinationTable because there are no rows."
        return
    }

    $bulkCopy = [System.Data.SqlClient.SqlBulkCopy]::new($connection)
    $bulkCopy.DestinationTableName = "dbo.$destinationTable"
    $bulkCopy.BulkCopyTimeout = 300
    foreach ($column in $table.Columns) {
        [void]$bulkCopy.ColumnMappings.Add($column.ColumnName, $column.ColumnName)
    }

    $bulkCopy.WriteToServer($table)
    $bulkCopy.Close()
    Write-Host "Inserted $($table.Rows.Count) rows into $destinationTable."
}

function Normalize-DateValue($value) {
    if ($null -eq $value -or $value -is [System.DBNull]) {
        return [System.DBNull]::Value
    }

    $dateValue = [datetime]$value
    if ($dateValue.Year -le 1900) {
        return [System.DBNull]::Value
    }

    return $dateValue
}

function New-TargetTable([string[]]$columns) {
    $table = [System.Data.DataTable]::new()
    foreach ($name in $columns) {
        [void]$table.Columns.Add($name)
    }

    return $table
}

$targetConnection = New-Connection $targetConnectionString
$sourceConnection = New-Connection $sourceConnectionString

try {
    $companyProfiles = Invoke-QueryTable $targetConnection @"
SELECT Id, MigratedId
FROM dbo.CompanyProfiles
WHERE MigratedId IS NOT NULL;
"@

    $companyMap = @{}
    foreach ($row in $companyProfiles.Rows) {
        $companyMap[[int64]$row["MigratedId"]] = [guid]$row["Id"]
    }

    $countryLookup = Invoke-QueryTable $targetConnection @"
SELECT Id, MigratedId
FROM dbo.LookupCountries
WHERE MigratedId IS NOT NULL;
"@

    $countryMap = @{}
    foreach ($row in $countryLookup.Rows) {
        $countryMap[[int64]$row["MigratedId"]] = [guid]$row["Id"]
    }

    Invoke-NonQuery $targetConnection @"
DELETE FROM dbo.CompanyProfileUltimateParentHoldingCompanies;
DELETE FROM dbo.CompanyProfileOtherSources;
DELETE FROM dbo.CompanyProfileTotalFinancings;
DELETE FROM dbo.CompanyProfileLoanForeigns;
DELETE FROM dbo.CompanyProfileLoanDomestics;
DELETE FROM dbo.CompanyProfileLoans;
DELETE FROM dbo.CompanyProfileCompanyIncorporatedCountries;
DELETE FROM dbo.CompanyProfileCompanyIncorporated;
DELETE FROM dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia;
DELETE FROM dbo.CompanyProfilePaidUpCapitalForeignCompanies;
DELETE FROM dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals;
DELETE FROM dbo.CompanyProfilePaidUpCapitals;
DELETE FROM dbo.CompanyProfileFinancialPerformanceRecords;
DELETE FROM dbo.CompanyProfileEquityStructures;
DELETE FROM dbo.CompanyProfileAuthorizedCapitals;
DELETE FROM dbo.CompanyProfileFinancialDetails;
"@

    $financialDetails = New-TargetTable @(
        "CompanyProfileId",
        "MigratedId",
        "LegacyProjectId",
        "FinancialYear",
        "EffectiveDate",
        "ProjectStatusId",
        "SourceCreatedByLegacyUserId",
        "SourceCreatedAt",
        "SourceUpdatedByLegacyUserId",
        "SourceUpdatedAt"
    )

    $sourceFinancialDetails = Invoke-QueryTable $sourceConnection @"
SELECT
    pf.ID AS MigratedId,
    pf.PROJECTID AS LegacyProjectId,
    pf.[YEAR] AS FinancialYear,
    pf.EFFECTIVEDATE,
    pf.PROJECTSTATUSID,
    pf.CREATEDBY,
    pf.CREATEDDATETIME,
    pf.MODIFIEDBY,
    pf.MODIFIEDDATETIME,
    COALESCE(fs.COMPANYID, project.COMPANYID) AS LegacyCompanyId
FROM dbo.OSUSR_LPP_PROJECTFINANCING pf
LEFT JOIN dbo.OSUSR_LPP_PROJECT project ON project.ID = pf.PROJECTID
LEFT JOIN dbo.OSUSR_LPP_FINANCINGSTRUCTURE fs ON fs.ID = pf.ID
WHERE COALESCE(fs.COMPANYID, project.COMPANYID) IS NOT NULL
ORDER BY pf.ID;
"@

    foreach ($row in $sourceFinancialDetails.Rows) {
        $legacyCompanyId = [int64]$row["LegacyCompanyId"]
        if (-not $companyMap.ContainsKey($legacyCompanyId)) {
            continue
        }

        [void]$financialDetails.Rows.Add(
            $companyMap[$legacyCompanyId],
            [int64]$row["MigratedId"],
            $(if ($row["LegacyProjectId"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int64]$row["LegacyProjectId"] }),
            $(if ($row["FinancialYear"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["FinancialYear"] }),
            (Normalize-DateValue $row["EffectiveDate"]),
            $(if ($row["ProjectStatusId"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["ProjectStatusId"] }),
            $(if ($row["CreatedBy"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CreatedBy"] }),
            (Normalize-DateValue $row["CreatedDateTime"]),
            $(if ($row["ModifiedBy"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["ModifiedBy"] }),
            (Normalize-DateValue $row["ModifiedDateTime"])
        )
    }

    Write-BulkTable $targetConnection "CompanyProfileFinancialDetails" $financialDetails

    $financialDetailLookup = Invoke-QueryTable $targetConnection @"
SELECT Id, MigratedId
FROM dbo.CompanyProfileFinancialDetails;
"@

    $financialDetailMap = @{}
    foreach ($row in $financialDetailLookup.Rows) {
        $financialDetailMap[[int64]$row["MigratedId"]] = [guid]$row["Id"]
    }

    $authorizedCapital = New-TargetTable @(
        "CompanyProfileId",
        "MigratedCompanyId",
        "AuthorizedCapital",
        "SourceCreatedByLegacyUserId",
        "SourceCreatedAt",
        "SourceUpdatedByLegacyUserId",
        "SourceUpdatedAt"
    )

    $sourceAuthorizedCapital = Invoke-QueryTable $sourceConnection @"
SELECT COMPANYID, AUTHORIZEDCAPITAL, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_AUTHORIZEDCAPITAL1
WHERE COMPANYID IS NOT NULL
ORDER BY COMPANYID;
"@

    foreach ($row in $sourceAuthorizedCapital.Rows) {
        $companyId = [int64]$row["CompanyId"]
        if (-not $companyMap.ContainsKey($companyId)) {
            continue
        }

        [void]$authorizedCapital.Rows.Add(
            $companyMap[$companyId],
            $companyId,
            $(if ($row["AuthorizedCapital"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AuthorizedCapital"] }),
            $(if ($row["CreatedBy"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CreatedBy"] }),
            (Normalize-DateValue $row["CreatedDateTime"]),
            $(if ($row["ModifiedBy"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["ModifiedBy"] }),
            (Normalize-DateValue $row["ModifiedDateTime"])
        )
    }

    Write-BulkTable $targetConnection "CompanyProfileAuthorizedCapitals" $authorizedCapital

    function Import-OneToOne($sourceSql, $targetTableName, [string[]]$targetColumns, $transform) {
        $table = New-TargetTable $targetColumns
        $sourceRows = Invoke-QueryTable $sourceConnection $sourceSql
        foreach ($sourceRow in $sourceRows.Rows) {
            $mapped = & $transform $sourceRow
            if ($null -ne $mapped) {
                [void]$table.Rows.Add($mapped)
            }
        }
        Write-BulkTable $targetConnection $targetTableName $table
    }

    Import-OneToOne @"
SELECT FINANCINGDETAILSID, BUMI_RM, BUMI_PERCENT, NONBUMI_RM, NONBUMI_PERCENT, FOREIGN_RM, FOREIGN_PERCENT, TOTAL_RM, TOTAL_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_EQUITYSTRUCTURE;
"@ "CompanyProfileEquityStructures" @(
        "FinancialDetailsId","BumiRm","BumiPercent","NonBumiRm","NonBumiPercent","ForeignRm","ForeignPercent","TotalRm","TotalPercent","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["FINANCINGDETAILSID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        return @(
            $financialDetailMap[$legacyId],
            $(if ($row["BUMI_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["BUMI_RM"] }),
            $(if ($row["BUMI_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["BUMI_PERCENT"] }),
            $(if ($row["NONBUMI_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["NONBUMI_RM"] }),
            $(if ($row["NONBUMI_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["NONBUMI_PERCENT"] }),
            $(if ($row["FOREIGN_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["FOREIGN_RM"] }),
            $(if ($row["FOREIGN_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["FOREIGN_PERCENT"] }),
            $(if ($row["TOTAL_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTAL_RM"] }),
            $(if ($row["TOTAL_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTAL_PERCENT"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    Import-OneToOne @"
SELECT FINANCINGDETAILSID, REVENUE, PROFIT, TAXABLEEXPENDITURE, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_FINANCIALPERFORMANCERECORD;
"@ "CompanyProfileFinancialPerformanceRecords" @(
        "FinancialDetailsId","Revenue","Profit","TaxableExpenditure","ExportSales","LocalSales","TotalAsset","ShareholderFund","EmployeeCount","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["FINANCINGDETAILSID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        return @(
            $financialDetailMap[$legacyId],
            $(if ($row["REVENUE"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["REVENUE"] }),
            $(if ($row["PROFIT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["PROFIT"] }),
            $(if ($row["TAXABLEEXPENDITURE"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TAXABLEEXPENDITURE"] }),
            [System.DBNull]::Value,
            [System.DBNull]::Value,
            [System.DBNull]::Value,
            [System.DBNull]::Value,
            [System.DBNull]::Value,
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    Import-OneToOne @"
SELECT FINANCINGDETAILSID, TOTAL_PAIDUPCAPITAL, TOTAL_RESERVES, TOTAL_SHAREHOLDERFUND, TOTALRM_MSIANINDIVIDUALS, TOTALPERCENT_MSIANINDIVIDUAL, TOTALRM_FOREIGNCOMPANY, TOTALPERCENT_FOREIGNCOMPANY, TOTALRM_COMPANYMALAYSIA, TOTALPERCENT_COMPANYMALAYSI, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_PUC_PAIDUPCAPITAL;
"@ "CompanyProfilePaidUpCapitals" @(
        "FinancialDetailsId","TotalPaidUpCapital","TotalReserves","TotalShareholderFund","TotalRmMalaysianIndividuals","TotalPercentMalaysianIndividuals","TotalRmForeignCompany","TotalPercentForeignCompany","TotalRmCompanyMalaysia","TotalPercentCompanyMalaysia","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["FINANCINGDETAILSID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        return @(
            $financialDetailMap[$legacyId],
            $(if ($row["TOTAL_PAIDUPCAPITAL"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTAL_PAIDUPCAPITAL"] }),
            $(if ($row["TOTAL_RESERVES"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTAL_RESERVES"] }),
            $(if ($row["TOTAL_SHAREHOLDERFUND"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTAL_SHAREHOLDERFUND"] }),
            $(if ($row["TOTALRM_MSIANINDIVIDUALS"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALRM_MSIANINDIVIDUALS"] }),
            $(if ($row["TOTALPERCENT_MSIANINDIVIDUAL"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALPERCENT_MSIANINDIVIDUAL"] }),
            $(if ($row["TOTALRM_FOREIGNCOMPANY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALRM_FOREIGNCOMPANY"] }),
            $(if ($row["TOTALPERCENT_FOREIGNCOMPANY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALPERCENT_FOREIGNCOMPANY"] }),
            $(if ($row["TOTALRM_COMPANYMALAYSIA"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALRM_COMPANYMALAYSIA"] }),
            $(if ($row["TOTALPERCENT_COMPANYMALAYSI"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALPERCENT_COMPANYMALAYSI"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    Import-OneToOne @"
SELECT PUC_PAIDUPCAPITALID, BUMIPUTERA_RM, BUMIPUTERA_PERCENT, NONBUMIPUTERA_RM, NONBUMIPUTERA_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_PUC_MALAYSIANINDIVIDUALS;
"@ "CompanyProfilePaidUpCapitalMalaysianIndividuals" @(
        "FinancialDetailsId","BumiputeraRm","BumiputeraPercent","NonBumiputeraRm","NonBumiputeraPercent","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["PUC_PAIDUPCAPITALID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        return @(
            $financialDetailMap[$legacyId],
            $(if ($row["BUMIPUTERA_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["BUMIPUTERA_RM"] }),
            $(if ($row["BUMIPUTERA_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["BUMIPUTERA_PERCENT"] }),
            $(if ($row["NONBUMIPUTERA_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["NONBUMIPUTERA_RM"] }),
            $(if ($row["NONBUMIPUTERA_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["NONBUMIPUTERA_PERCENT"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    function Import-Owned([string]$sourceSql, [string]$targetTableName, [string[]]$targetColumns, $transform) {
        $table = New-TargetTable $targetColumns
        $sourceRows = Invoke-QueryTable $sourceConnection $sourceSql
        foreach ($sourceRow in $sourceRows.Rows) {
            $mapped = & $transform $sourceRow
            if ($null -ne $mapped) {
                [void]$table.Rows.Add($mapped)
            }
        }
        Write-BulkTable $targetConnection $targetTableName $table
    }

    Import-Owned @"
SELECT PUC_PAIDUPCAPITALID, ID, COMPANYNAME, COUNTRYID, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_PUC_FOREIGNCOMPANY;
"@ "CompanyProfilePaidUpCapitalForeignCompanies" @(
        "FinancialDetailsId","MigratedId","CompanyName","CountryId","LegacyCountryId","AmountRm","AmountPercent","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["PUC_PAIDUPCAPITALID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        $legacyCountryId = $(if ($row["COUNTRYID"] -is [System.DBNull]) { $null } else { [int64]$row["COUNTRYID"] })
        $countryId = if ($null -ne $legacyCountryId -and $countryMap.ContainsKey($legacyCountryId)) { $countryMap[$legacyCountryId] } else { [System.DBNull]::Value }
        return @(
            $financialDetailMap[$legacyId],
            [int64]$row["ID"],
            $(if ($row["COMPANYNAME"] -is [System.DBNull]) { [System.DBNull]::Value } else { [string]$row["COMPANYNAME"] }),
            $countryId,
            $(if ($null -eq $legacyCountryId) { [System.DBNull]::Value } else { $legacyCountryId }),
            $(if ($row["AMOUNT_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_RM"] }),
            $(if ($row["AMOUNT_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_PERCENT"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    Import-Owned @"
SELECT PUC_PAIDUPCAPITALID, ID, COMPANYNAME, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_PUC_COMPANYMALAYSIA;
"@ "CompanyProfilePaidUpCapitalCompaniesMalaysia" @(
        "FinancialDetailsId","MigratedId","CompanyName","AmountRm","AmountPercent","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["PUC_PAIDUPCAPITALID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        return @(
            $financialDetailMap[$legacyId],
            [int64]$row["ID"],
            $(if ($row["COMPANYNAME"] -is [System.DBNull]) { [System.DBNull]::Value } else { [string]$row["COMPANYNAME"] }),
            $(if ($row["AMOUNT_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_RM"] }),
            $(if ($row["AMOUNT_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_PERCENT"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    $companyMalaysiaLookup = Invoke-QueryTable $targetConnection @"
SELECT Id, MigratedId
FROM dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia;
"@
    $companyMalaysiaMap = @{}
    foreach ($row in $companyMalaysiaLookup.Rows) { $companyMalaysiaMap[[int64]$row["MigratedId"]] = [guid]$row["Id"] }

    Import-Owned @"
SELECT ID, FINANCINGDETAILSID, PUC_COMPANYMALAYSIAID, LOCALCOMPANYTYPEID, BUMI_PERCENT, NONBUMI_PERCENT, FOREIGN_COUNTRYID, FOREIGN_PERCENT, TOTAL_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_COMPANYINCORPORATED;
"@ "CompanyProfileCompanyIncorporated" @(
        "FinancialDetailsId","MigratedId","PaidUpCapitalCompanyMalaysiaEntryId","LocalCompanyTypeId","BumiPercent","NonBumiPercent","ForeignCountryId","LegacyForeignCountryId","ForeignPercent","TotalPercent","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["FINANCINGDETAILSID"]
        $companyMalaysiaSourceId = [int64]$row["PUC_COMPANYMALAYSIAID"]
        if (-not $financialDetailMap.ContainsKey($legacyId) -or -not $companyMalaysiaMap.ContainsKey($companyMalaysiaSourceId)) { return $null }
        $legacyCountryId = $(if ($row["FOREIGN_COUNTRYID"] -is [System.DBNull]) { $null } else { [int64]$row["FOREIGN_COUNTRYID"] })
        $countryId = if ($null -ne $legacyCountryId -and $countryMap.ContainsKey($legacyCountryId)) { $countryMap[$legacyCountryId] } else { [System.DBNull]::Value }
        return @(
            $financialDetailMap[$legacyId],
            [int64]$row["ID"],
            $companyMalaysiaMap[$companyMalaysiaSourceId],
            $(if ($row["LOCALCOMPANYTYPEID"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["LOCALCOMPANYTYPEID"] }),
            $(if ($row["BUMI_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["BUMI_PERCENT"] }),
            $(if ($row["NONBUMI_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["NONBUMI_PERCENT"] }),
            $countryId,
            $(if ($null -eq $legacyCountryId) { [System.DBNull]::Value } else { $legacyCountryId }),
            $(if ($row["FOREIGN_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["FOREIGN_PERCENT"] }),
            $(if ($row["TOTAL_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTAL_PERCENT"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    $companyIncorporatedLookup = Invoke-QueryTable $targetConnection @"
SELECT ci.Id, pcm.MigratedId AS PaidUpCapitalCompanyMalaysiaMigratedId
FROM dbo.CompanyProfileCompanyIncorporated ci
JOIN dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia pcm ON pcm.Id = ci.PaidUpCapitalCompanyMalaysiaEntryId;
"@
    $companyIncorporatedMap = @{}
    foreach ($row in $companyIncorporatedLookup.Rows) { $companyIncorporatedMap[[int64]$row["PaidUpCapitalCompanyMalaysiaMigratedId"]] = [guid]$row["Id"] }

    Import-Owned @"
SELECT ID, PUC_COMPANYMALAYSIAID, FOREIGN_COUNTRYID, COUNTRYPERCENT, AMOUNT_RM, PERCENTOVERTOTAL
FROM dbo.OSUSR_LPP_COMPFIN_COMPINCCOUNTRIES;
"@ "CompanyProfileCompanyIncorporatedCountries" @(
        "CompanyIncorporatedEntryId","MigratedId","CountryId","LegacyCountryId","CountryPercent","AmountRm","PercentOverTotal","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $companyMalaysiaSourceId = [int64]$row["PUC_COMPANYMALAYSIAID"]
        if (-not $companyIncorporatedMap.ContainsKey($companyMalaysiaSourceId)) { return $null }
        $legacyCountryId = $(if ($row["FOREIGN_COUNTRYID"] -is [System.DBNull]) { $null } else { [int64]$row["FOREIGN_COUNTRYID"] })
        $countryId = if ($null -ne $legacyCountryId -and $countryMap.ContainsKey($legacyCountryId)) { $countryMap[$legacyCountryId] } else { [System.DBNull]::Value }
        return @(
            $companyIncorporatedMap[$companyMalaysiaSourceId],
            [int64]$row["ID"],
            $countryId,
            $(if ($null -eq $legacyCountryId) { [System.DBNull]::Value } else { $legacyCountryId }),
            $(if ($row["COUNTRYPERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["COUNTRYPERCENT"] }),
            $(if ($row["AMOUNT_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_RM"] }),
            $(if ($row["PERCENTOVERTOTAL"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["PERCENTOVERTOTAL"] }),
            [System.DBNull]::Value,
            [System.DBNull]::Value,
            [System.DBNull]::Value,
            [System.DBNull]::Value
        )
    }

    Import-OneToOne @"
SELECT FINANCINGDETAILSID, TOTALRM_LOAN, TOTALLOAN_PERCENT, DOMESTICDESCRIPTION, FOREIGNDESCRIPTION, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_LOAN;
"@ "CompanyProfileLoans" @(
        "FinancialDetailsId","TotalRmLoan","TotalLoanPercent","DomesticDescription","ForeignDescription","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["FINANCINGDETAILSID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        return @(
            $financialDetailMap[$legacyId],
            $(if ($row["TOTALRM_LOAN"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALRM_LOAN"] }),
            $(if ($row["TOTALLOAN_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALLOAN_PERCENT"] }),
            $(if ($row["DOMESTICDESCRIPTION"] -is [System.DBNull]) { [System.DBNull]::Value } else { [string]$row["DOMESTICDESCRIPTION"] }),
            $(if ($row["FOREIGNDESCRIPTION"] -is [System.DBNull]) { [System.DBNull]::Value } else { [string]$row["FOREIGNDESCRIPTION"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    Import-OneToOne @"
SELECT LOANID, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_LOAN_DOMESTIC;
"@ "CompanyProfileLoanDomestics" @(
        "FinancialDetailsId","AmountRm","AmountPercent","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["LOANID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        return @(
            $financialDetailMap[$legacyId],
            $(if ($row["AMOUNT_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_RM"] }),
            $(if ($row["AMOUNT_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_PERCENT"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    Import-Owned @"
SELECT LOANID, ID, COUNTRYOFORIGIN, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_LOAN_FOREIGN;
"@ "CompanyProfileLoanForeigns" @(
        "FinancialDetailsId","MigratedId","CountryId","LegacyCountryId","AmountRm","AmountPercent","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["LOANID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        $legacyCountryId = $(if ($row["COUNTRYOFORIGIN"] -is [System.DBNull]) { $null } else { [int64]$row["COUNTRYOFORIGIN"] })
        $countryId = if ($null -ne $legacyCountryId -and $countryMap.ContainsKey($legacyCountryId)) { $countryMap[$legacyCountryId] } else { [System.DBNull]::Value }
        return @(
            $financialDetailMap[$legacyId],
            [int64]$row["ID"],
            $countryId,
            $(if ($null -eq $legacyCountryId) { [System.DBNull]::Value } else { $legacyCountryId }),
            $(if ($row["AMOUNT_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_RM"] }),
            $(if ($row["AMOUNT_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_PERCENT"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    Import-OneToOne @"
SELECT FINANCINGDETAILSID, TOTALPAIDUPCAPITAL, TOTALRESERVE, TOTALLOAN, TOTALRM_OTHERSOURCES, TOTALPERCENT_OTHERSOURCES, TOTALFINANCING_RM, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_TOTALFINANCING;
"@ "CompanyProfileTotalFinancings" @(
        "FinancialDetailsId","TotalPaidUpCapital","TotalReserve","TotalLoan","TotalRmOtherSources","TotalPercentOtherSources","TotalFinancingRm","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["FINANCINGDETAILSID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        return @(
            $financialDetailMap[$legacyId],
            $(if ($row["TOTALPAIDUPCAPITAL"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALPAIDUPCAPITAL"] }),
            $(if ($row["TOTALRESERVE"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALRESERVE"] }),
            $(if ($row["TOTALLOAN"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALLOAN"] }),
            $(if ($row["TOTALRM_OTHERSOURCES"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALRM_OTHERSOURCES"] }),
            $(if ($row["TOTALPERCENT_OTHERSOURCES"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALPERCENT_OTHERSOURCES"] }),
            $(if ($row["TOTALFINANCING_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["TOTALFINANCING_RM"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    Import-Owned @"
SELECT TOTALFINANCINGID, ID, OTHERSOURCES, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_OTHERSOURCES;
"@ "CompanyProfileOtherSources" @(
        "FinancialDetailsId","MigratedId","OtherSources","AmountRm","AmountPercent","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["TOTALFINANCINGID"]
        if (-not $financialDetailMap.ContainsKey($legacyId)) { return $null }
        return @(
            $financialDetailMap[$legacyId],
            [int64]$row["ID"],
            $(if ($row["OTHERSOURCES"] -is [System.DBNull]) { [System.DBNull]::Value } else { [string]$row["OTHERSOURCES"] }),
            $(if ($row["AMOUNT_RM"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_RM"] }),
            $(if ($row["AMOUNT_PERCENT"] -is [System.DBNull]) { [System.DBNull]::Value } else { [decimal]$row["AMOUNT_PERCENT"] }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    $foreignCompanyLookup = Invoke-QueryTable $targetConnection @"
SELECT Id, MigratedId
FROM dbo.CompanyProfilePaidUpCapitalForeignCompanies;
"@
    $foreignCompanyMap = @{}
    foreach ($row in $foreignCompanyLookup.Rows) { $foreignCompanyMap[[int64]$row["MigratedId"]] = [guid]$row["Id"] }

    Import-Owned @"
SELECT ID, FINANCINGDETAILSID, PUC_FOREIGNCOMPANYID, ULTIMATECOMPANY, COUNTRYOFORIGIN, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME
FROM dbo.OSUSR_LPP_COMPFIN_ULTIMATEPARENTHOLDINGCOMPANY;
"@ "CompanyProfileUltimateParentHoldingCompanies" @(
        "FinancialDetailsId","MigratedId","PaidUpCapitalForeignCompanyEntryId","UltimateCompany","CountryId","LegacyCountryId","SourceCreatedByLegacyUserId","SourceCreatedAt","SourceUpdatedByLegacyUserId","SourceUpdatedAt"
    ) {
        param($row)
        $legacyId = [int64]$row["FINANCINGDETAILSID"]
        $foreignCompanySourceId = [int64]$row["PUC_FOREIGNCOMPANYID"]
        if (-not $financialDetailMap.ContainsKey($legacyId) -or -not $foreignCompanyMap.ContainsKey($foreignCompanySourceId)) { return $null }
        $legacyCountryId = $(if ($row["COUNTRYOFORIGIN"] -is [System.DBNull]) { $null } else { [int64]$row["COUNTRYOFORIGIN"] })
        $countryId = if ($null -ne $legacyCountryId -and $countryMap.ContainsKey($legacyCountryId)) { $countryMap[$legacyCountryId] } else { [System.DBNull]::Value }
        return @(
            $financialDetailMap[$legacyId],
            [int64]$row["ID"],
            $foreignCompanyMap[$foreignCompanySourceId],
            $(if ($row["ULTIMATECOMPANY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [string]$row["ULTIMATECOMPANY"] }),
            $countryId,
            $(if ($null -eq $legacyCountryId) { [System.DBNull]::Value } else { $legacyCountryId }),
            $(if ($row["CREATEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["CREATEDBY"] }),
            (Normalize-DateValue $row["CREATEDDATETIME"]),
            $(if ($row["MODIFIEDBY"] -is [System.DBNull]) { [System.DBNull]::Value } else { [int]$row["MODIFIEDBY"] }),
            (Normalize-DateValue $row["MODIFIEDDATETIME"])
        )
    }

    Write-Host "One-time company financial migration completed."
}
finally {
    if ($null -ne $sourceConnection) { $sourceConnection.Dispose() }
    if ($null -ne $targetConnection) { $targetConnection.Dispose() }
}
