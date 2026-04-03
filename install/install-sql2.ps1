
choco install sql-server-management-studio --yes --ignore-checksums

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$primaryPath = Join-Path $scriptDir "install.sql"
$resultPath = $primaryPath
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
