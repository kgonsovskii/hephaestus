#Requires -RunAsAdministrator
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\install-common.ps1"

Write-Host '[install-git] git via Chocolatey'
Invoke-ChocoInstall -Packages @('git') -Force:($env:INSTALL_FORCE -eq '1')

if (-not (Test-CommandExists 'git')) {
    throw 'git not on PATH after install.'
}

& git --version
