param (
    [string]$serverName,  [string]$user="",  [string]$password="", [string]$direct=""
)

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"

if ($serverName -eq "") {
    $serverName = detectServer
} 


if ($direct -ne "true")
{
    . ".\current.ps1" -serverName $serverName
}
. ".\install-lib.ps1" -serverName $serverName -user $user -password $password -direct $direct

if ($direct -eq "true")
{
    $serverIp = $serverName
}
else
{
    $password = $server.clone.clonePassword
    $user=$server.clone.cloneUser
    $serverIp = $server.clone.cloneServerIp
}

Write-Host "Install-Reboot $serverName, serverIp $serverIp"

WaitRestart -once $false