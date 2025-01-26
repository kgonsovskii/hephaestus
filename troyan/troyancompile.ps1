param (
    [string]$serverName
)

if ($serverName -eq "") {
    $serverName = "127.0.0.1"
    $action = "exe"
} 

if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path -Path $scriptDir -ChildPath "../sys/current.ps1") -serverName $serverName
Set-Location -Path $scriptDir

Start-Process -Wait $server.troyanBuilder -ArgumentList $serverName

Copy-Item -Path $server.body -Destination $server.userBody -Force


Write-Host "Troyan Compile complete $serverName"