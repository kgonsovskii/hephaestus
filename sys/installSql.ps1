# Stop IIS service if it exists
if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
    Stop-Service -Name W3SVC
} else {
    Write-Host "Service W3SVC does not exist."
}
function Install-SQLServer {
    # Check if SQL Server is installed using the registry instead of WMI for better accuracy
    $installedSQL = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "Microsoft SQL Server 2019*" }


    if ($installedSQL) {
        Write-Host "SQL Server is already installed."
    } else {
        Write-Host "SQL Server is not installed. Proceeding with installation."
        choco install sql-server-express --version=2019.20190106 --yes --ignore-checksums --no-progress
    }
}

function Install-SqlCmd {
    # Check if sqlcmd is installed by verifying the path
    $sqlcmdPath = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
    
    if (Test-Path $sqlcmdPath) {
        Write-Host "sqlcmd is already installed."
    } else {
        Write-Host "sqlcmd is not installed. Proceeding with installation."

        # Install sqlcmd utilities using Chocolatey (make sure choco is installed)
        choco install mssql-tools --yes --ignore-checksums --no-progress
        choco install sqlserver-cmdlineutils --yes --ignore-checksums --no-progress

        Write-Host "sqlcmd installation is complete."
    }
}

Install-SQLServer
Install-SqlCmd
