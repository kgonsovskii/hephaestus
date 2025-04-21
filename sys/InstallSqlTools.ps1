function Install-SSMS {
    # Check if SQL Server Management Studio (SSMS) is installed
    $ssmsInstalled = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "SQL Server Management Studio*" }

    if ($ssmsInstalled) {
        Write-Host "SQL Server Management Studio (SSMS) is already installed."
    } else {
        Write-Host "SQL Server Management Studio is not installed. Proceeding with installation."

        # Install SSMS using Chocolatey silently
        choco install sql-server-management-studio --yes --ignore-checksums --no-progress

        # Wait for SSMS installation to complete
        Write-Host "SQL Server Management Studio installation is complete."
    }
}
Install-SSMS

function Install-SqlCmd {
    # Check if sqlcmd is installed
    $sqlcmdPath = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
    
    if (Test-Path $sqlcmdPath) {
        Write-Host "sqlcmd is already installed."
    } else {
        Write-Host "sqlcmd is not installed. Proceeding with installation."

        # Install sqlcmd utilities
        choco install mssql-tools --yes --ignore-checksums --no-progress
        choco install sqlserver-cmdlineutils --yes --ignore-checksums --no-progress

        Write-Host "sqlcmd installation is complete."
    }
}
Install-SqlCmd


$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$primaryPath = Join-Path $scriptDir "install.sql"
$fallbackPath = "C:\install.sql"
$resultPath = if (Test-Path $primaryPath) { $primaryPath } elseif (Test-Path $fallbackPath) { $fallbackPath } else { $null }
function Run-SQLScript {
    param(
        [string]$serverInstance = "localhost\SQLEXPRESS",
        [string]$resultPath = "C:\install.sql"
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
