function Install-Chocolatey {
    # Check if Chocolatey is installed
    $chocoInstalled = Get-Command choco -ErrorAction SilentlyContinue
  
    if (-not $chocoInstalled) {
        Write-Host "Chocolatey is not installed. Installing Chocolatey..."
  
        # Install Chocolatey
        Set-ExecutionPolicy Bypass -Scope Process -Force; 
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;
        Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
  
        # Wait for Chocolatey installation to complete
        Write-Host "Chocolatey installation is complete."
    } else {
        Write-Host "Chocolatey is already installed."
    }
}
Install-Chocolatey
function Install-SQL {
    $sqlInstalled = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "Microsoft SQL Server 2019*" }

    if ($sqlInstalled) {
        Write-Host "SQL Server is already installed."
    } else {
        Write-Host "SQL Server is not installed. Proceeding with installation."
        choco install sql-server-express --version=2019.20190106 --yes --ignore-checksums --no-progress
        Write-Host "SQL Server is complete."
    }
}
Install-SQL

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


function Install-FarGit {
    # Install Far Manager if not installed
    $farInstalled = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "Far Manager*" }

    if ($farInstalled) {
        Write-Host "Far Manager is already installed."
    } else {
        Write-Host "Far Manager is not installed. Proceeding with installation."
        choco install far --yes --ignore-checksums --no-progress
        Write-Host "Far Manager installation is complete."
    }

    # Install Git if not installed
    $gitInstalled = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "Git*" }

    if ($gitInstalled) {
        Write-Host "Git is already installed."
    } else {
        Write-Host "Git is not installed. Proceeding with installation."
        choco install git --yes --ignore-checksums --no-progress
        Write-Host "Git installation is complete."
    }
}
Install-FarGit




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

