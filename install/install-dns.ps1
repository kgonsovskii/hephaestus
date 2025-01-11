$ErrorActionPreference = 'Stop'

Import-Module ServerManager -ErrorAction SilentlyContinue

if (Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue) {
    Install-WindowsFeature -Name DNS -IncludeManagementTools
    return
}

if (-not (Get-Command Enable-WindowsOptionalFeature -ErrorAction SilentlyContinue)) {
    throw 'install-dns: Enable-WindowsOptionalFeature not available. Use Windows Server with elevated PowerShell.'
}

$dnsFeat = $null
try {
    $dnsFeat = Get-WindowsOptionalFeature -Online -FeatureName DNS-Server-Full-Role -ErrorAction Stop
} catch {
    $dnsFeat = $null
}

if ($null -eq $dnsFeat) {
    Write-Host 'install-dns: Skipping. DNS-Server-Full-Role is not in this Windows image (Microsoft DNS server needs Windows Server).'
    return
}

if ($dnsFeat.State -ne 'Enabled') {
    Enable-WindowsOptionalFeature -Online -FeatureName DNS-Server-Full-Role -All -NoRestart
}

try {
    $rsat = Get-WindowsOptionalFeature -Online -FeatureName RSAT-DNS-Server -ErrorAction Stop
    if ($rsat.State -ne 'Enabled') {
        Enable-WindowsOptionalFeature -Online -FeatureName RSAT-DNS-Server -NoRestart
    }
} catch {
}
