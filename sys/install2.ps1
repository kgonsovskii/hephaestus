function Download-File {
    param (
        [string]$Uri,
        [string]$OutFile
    )

    $webClient = New-Object System.Net.WebClient
    try {
        $webClient.DownloadFile($Uri, $OutFile)
        Write-Host "Download completed: $OutFile"
    } catch {
        Write-Host "Error downloading file: $_"
    } finally {
        $webClient.Dispose()
    }
}

function Install-SQLServer {
    $installedSQL = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "Microsoft SQL Server 2019*" }

    if ($installedSQL) {
        Write-Host "SQL Server 2019 is already installed."
    } else {
        Write-Host "SQL Server 2019 is not installed. Proceeding with installation."

        # Define paths
        $installerUrl = "https://download.microsoft.com/download/7/c/1/7c14e92e-bdcb-4f89-b7cf-93543e7112d1/SQLEXPR_x64_ENU.exe"
        $downloadPath = "C:\Temp\SQLEXPR_x64_ENU.exe"
        $extractPath = "C:\Temp\SQLServerExtracted"

        # Ensure the temp directory exists
        if (-not (Test-Path "C:\Temp")) {
            New-Item -ItemType Directory -Path "C:\Temp" | Out-Null
        }

        # Download the installer
        Write-Host "Downloading SQL Server installer..."
        Download-File -Uri $installerUrl -OutFile $downloadPath

        # Extract the installer
        Write-Host "Extracting SQL Server installer..."
        Start-Process -FilePath $downloadPath -ArgumentList "/Q /X:$extractPath" -Wait

        # Run the actual SQL Server setup
        Write-Host "Running SQL Server installation..."
        $setupPath = "$extractPath\Setup.exe"
        Start-Process -FilePath $setupPath -ArgumentList "/Q /IACCEPTSQLSERVERLICENSETERMS /ACTION=Install /FEATURES=SQL /INSTANCENAME=SQLEXPRESS /SQLSVCSTARTUPTYPE=Automatic /SQLSYSADMINACCOUNTS=Administrator /TCPENABLED=1 /NPENABLED=1" -Wait

        Write-Host "SQL Server 2019 installation is complete."
    }
}



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

function Install-SqlCmd {
    # Check if sqlcmd is installed
    $sqlcmdPath = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
    
    if (Test-Path $sqlcmdPath) {
        Write-Host "sqlcmd is already installed."
    } else {
        Write-Host "sqlcmd is not installed. Proceeding with installation."

        # Install sqlcmd utilities
        choco install mssql-tools --yes --ignore-checksums --no-progress

        Write-Host "sqlcmd installation is complete."
    }
}

function Refresh-EnvVars {
    # Restart Windows Explorer to refresh environment variables and UNC paths
    Write-Host "Restarting Windows Explorer to refresh environment variables and UNC paths..."
    Stop-Process -Name explorer -Force
    Start-Process explorer

    # Refresh the system environment variables for the current session
    Write-Host "Refreshing system environment variables..."
    [System.Environment]::SetEnvironmentVariable('PATH', $env:PATH, [System.EnvironmentVariableTarget]::Machine)


    Write-Host "Environment variables and UNC paths refreshed!"
}

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
Install-SQLServer
Install-SSMS
Install-SqlCmd 
Refresh-EnvVars
Run-SQLScript
