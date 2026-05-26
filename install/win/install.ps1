#Requires -RunAsAdministrator
# Full local Hephaestus install on Windows (Chocolatey ≈ apt). Mirrors install/linux/install.sh.
param([switch]$Force)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Force) {
    $env:INSTALL_FORCE = '1'
    $env:INSTALL_DNS_FORCE = '1'
}

$scriptDir = $PSScriptRoot
if (-not $scriptDir) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}

function Invoke-InstallScript {
    param(
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Name
    )
    $full = Join-Path $scriptDir $Name
    if (-not (Test-Path -LiteralPath $full)) {
        throw "Missing install script: $full"
    }
    Write-Host ""
    Write-Host "========== $Title =========="
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $full
    if ($LASTEXITCODE -ne 0) {
        Write-Host "========== $Title FAILED (exit $LASTEXITCODE) ==========" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "========== $Title OK =========="
}

Invoke-InstallScript 'Uninstall (clean release)' 'uninstall.ps1'
Invoke-InstallScript 'Git' 'install-git.ps1'
Invoke-InstallScript '.NET 10 SDK' 'install-net.ps1'
Invoke-InstallScript 'PostgreSQL' 'install-postgres.ps1'
Invoke-InstallScript 'Technitium DNS' 'install-dns.ps1'
Invoke-InstallScript 'DomainHost (build + service)' 'install-soft.ps1'

Write-Host ""
Write-Host 'Install finished.'
