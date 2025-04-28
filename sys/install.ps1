param (
    [string]$serverName
)

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir

function WaitSql {
    Write-Host "Waiting for sql"
    [string]$ServerInstance = "localhost\SQLEXPRESS"
    [string]$SqlCmdPath = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
    [int]$IntervalSeconds = 5
    while ($true) 
    {
        $result = & "$SqlCmdPath" -S $ServerInstance -Q "SELECT 1" -b -h -1 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "SQL Server '$ServerInstance' is responsive."
            break
        } else {
            Write-Host "Waiting for SQL Server '$ServerInstance'... retrying in $IntervalSeconds seconds."
            Start-Sleep -Seconds $IntervalSeconds
        }
    }
    Write-Host "SQL SETUPED"
    Start-Sleep -Seconds 1
}

################

. ".\install0.ps1" 
. ".\installSql.ps1"
. ".\installSqlTools.ps1"
WaitSql
. ".\installWeb.ps1"
. ".\installTrigger.ps1"

# WaitRestart
# . ".\publish.ps1" -serverIp $serverIp -user $user -password $password -direct $true

Write-Host "----------- THE END --------------"