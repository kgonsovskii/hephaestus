choco install mssql-tools --yes --ignore-checksums --no-progress
choco install sqlserver-cmdlineutils --yes --ignore-checksums --no-progress
choco install sql-server-management-studio --yes --ignore-checksums --no-progress

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$primaryPath = Join-Path $scriptDir "install.sql"
$fallbackPath = "C:\install\install.sql"
$resultPath = if (Test-Path $primaryPath) { $primaryPath } elseif (Test-Path $fallbackPath) { $fallbackPath } else { $null }
function Run-SQLScript {
    param(
        [string]$serverInstance = "localhost\SQLEXPRESS"
    )

    $sqlCmdPath = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"

    if (-not (Test-Path $sqlCmdPath)) {
        throw "sqlcmd not found at expected path: $sqlCmdPath"
    }

    if (-not (Test-Path $resultPath)) {
        throw "SQL script file not found at: $resultPath"
    }

    Write-Host "Running SQL script..."
    & "$sqlCmdPath" -S "$serverInstance" -i "$resultPath" -E
    Write-Host "SQL script execution complete."
}
Run-SQLScript
