param (
    [string]$serverName, [string]$reboot="true"
)

. ".\install-x.ps1"

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"

if ($serverName -eq "") {
    $serverName = detectServer
} 

. ".\current.ps1" -serverName $serverName
. ".\install-lib.ps1" -serverName $serverName

$serverIp = $server.clone.cloneServerIp

Write-Host "Install-Pre $serverName, serverIp $serverIp, rebooting $reboot"


if ([string]::IsNullOrEmpty($serverIp))
{
    throw "No Server Ip defined"
}

function AddTrusted {
    param ($hostname)

    $currentTrustedHosts = (Get-Item WSMan:\localhost\Client\TrustedHosts).Value
    if ([string]::IsNullOrEmpty($currentTrustedHosts)) {
        $newTrustedHosts = $hostname
    } else {
        if ($currentTrustedHosts -notmatch [regex]::Escape($hostname)) {
            $newTrustedHosts = "$currentTrustedHosts,$hostname"
        } else {
            $newTrustedHosts = $currentTrustedHosts
        }
    }
    if ($currentTrustedHosts -ne $newTrustedHosts) {
        Set-Item WSMan:\localhost\Client\TrustedHosts -Value $newTrustedHosts -Force
    }
    Get-Item WSMan:\localhost\Client\TrustedHosts
    Set-Item WSMan:\localhost\Client\AllowUnencrypted -Value $true
}

AddTrusted -hostname $serverIp

if ($reboot -eq "true")
{
    WaitRestart -once $true
}