param(
    [Parameter(Mandatory = $true)]
    [string]$AppSettingsPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputFile,

    [string]$TenantId = "11111111-1111-1111-1111-111111111111",

    [string]$ConnectionString = ""
)

$ErrorActionPreference = 'Stop'

function Convert-ToSafeString {
    param(
        $Value
    )

    if ($null -eq $Value) {
        return ''
    }

    return [string]$Value
}

function Convert-ToPascalIdentifier {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $parts = $Value -split '[\.\-_ ]+' | Where-Object { $_ -and $_.Trim().Length -gt 0 }
    $builder = [System.Text.StringBuilder]::new()

    foreach ($part in $parts) {
        $characters = $part.ToCharArray() | Where-Object { [char]::IsLetterOrDigit($_) }
        $cleaned = -join $characters
        if ([string]::IsNullOrWhiteSpace($cleaned)) {
            continue
        }

        $builder.Append([char]::ToUpperInvariant($cleaned[0])) | Out-Null
        if ($cleaned.Length -gt 1) {
            $builder.Append($cleaned.Substring(1)) | Out-Null
        }
    }

    return $builder.ToString()
}

function Get-ConnectionString {
    param(
        [string]$ConfiguredConnectionString,
        [string]$SettingsPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredConnectionString)) {
        return $ConfiguredConnectionString
    }

    if (-not [string]::IsNullOrWhiteSpace($env:LF_FEATURE_DB_CONNECTION)) {
        return $env:LF_FEATURE_DB_CONNECTION
    }

    if (-not (Test-Path -LiteralPath $SettingsPath)) {
        throw "App settings file '$SettingsPath' was not found."
    }

    $settings = Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json
    $resolved = Convert-ToSafeString $settings.ConnectionStrings.DefaultConnection
    if ([string]::IsNullOrWhiteSpace($resolved)) {
        throw "DefaultConnection was not found in '$SettingsPath'."
    }

    return $resolved
}

$resolvedConnectionString = Get-ConnectionString -ConfiguredConnectionString $ConnectionString -SettingsPath $AppSettingsPath
$resolvedTenantId = if (-not [string]::IsNullOrWhiteSpace($env:LF_FEATURE_TENANT_ID)) { $env:LF_FEATURE_TENANT_ID } else { $TenantId }

try {
    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection($resolvedConnectionString)
    $command = $connection.CreateCommand()
    $command.CommandText = @"
SELECT [Code], [Name], [Description]
FROM [PlatformFeatures]
WHERE [IsDeleted] = 0
  AND [TenantId] = @TenantId
ORDER BY [DisplayOrder], [Code];
"@
    [void]$command.Parameters.Add("@TenantId", [System.Data.SqlDbType]::UniqueIdentifier)
    $command.Parameters["@TenantId"].Value = [Guid]::Parse($resolvedTenantId)

    $connection.Open()
    $reader = $command.ExecuteReader()
    $features = New-Object System.Collections.Generic.List[object]

    while ($reader.Read()) {
        $code = Convert-ToSafeString $reader["Code"]
        $name = Convert-ToSafeString $reader["Name"]
        $description = Convert-ToSafeString $reader["Description"]

        $code = $code.Trim()
        if ([string]::IsNullOrWhiteSpace($code)) {
            throw "PlatformFeatures contains a row with an empty Code."
        }

        $symbol = Convert-ToPascalIdentifier -Value $code
        if ([string]::IsNullOrWhiteSpace($symbol)) {
            throw "Platform feature '$code' produced an empty symbol name."
        }

        $features.Add([pscustomobject]@{
            Code = $code
            Symbol = $symbol
            Name = $name.Trim()
            Description = $description.Trim()
        }) | Out-Null
    }

    $reader.Close()
    $connection.Close()
}
catch {
    throw "Failed to generate feature constants from PlatformFeatures. $($_.Exception.Message)"
}
finally {
    if ($null -ne $reader) {
        $reader.Dispose()
    }

    if ($null -ne $command) {
        $command.Dispose()
    }

    if ($null -ne $connection) {
        $connection.Dispose()
    }
}

$duplicateCodes = $features | Group-Object Code | Where-Object Count -gt 1 | Select-Object -ExpandProperty Name
if ($duplicateCodes.Count -gt 0) {
    throw "Platform feature codes must be unique. Duplicates: $($duplicateCodes -join ', ')."
}

$duplicateSymbols = $features | Group-Object Symbol | Where-Object Count -gt 1 | Select-Object -ExpandProperty Name
if ($duplicateSymbols.Count -gt 0) {
    throw "Platform feature symbols must be unique. Duplicates: $($duplicateSymbols -join ', ')."
}

$orderedFeatures = @($features | Sort-Object Code)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('namespace LogicFlowEnterpriseFramework.Shared.Constants;')
$lines.Add('')
$lines.Add('public static partial class PlatformFeatureCodes')
$lines.Add('{')
foreach ($feature in $orderedFeatures) {
    $lines.Add("    public const string $($feature.Symbol) = ""$($feature.Code)"";")
}
$lines.Add('')
$lines.Add('    public static readonly string[] All =')
$lines.Add('    [')
foreach ($feature in $orderedFeatures) {
    $lines.Add("        $($feature.Symbol),")
}
$lines.Add('    ];')
$lines.Add('}')
$lines.Add('')
$lines.Add('public static partial class Permissions')
$lines.Add('{')
foreach ($feature in $orderedFeatures) {
    $lines.Add("    public const string $($feature.Symbol) = PlatformFeatureCodes.$($feature.Symbol);")
}
$lines.Add('')
$lines.Add('    public static readonly string[] All = PlatformFeatureCodes.All;')
$lines.Add('    public static readonly string[] Admin = PlatformFeatureCodes.All;')
$lines.Add('    public static readonly string[] User = PlatformFeatureCodes.All;')
$lines.Add('}')

$outputDirectory = Split-Path -Parent $OutputFile
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$content = ($lines -join [Environment]::NewLine) + [Environment]::NewLine
if ((Test-Path -LiteralPath $OutputFile) -and ([System.IO.File]::ReadAllText($OutputFile) -eq $content)) {
    return
}

[System.IO.File]::WriteAllText($OutputFile, $content, [System.Text.UTF8Encoding]::new($false))
