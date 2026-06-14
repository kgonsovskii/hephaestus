#Requires -RunAsAdministrator
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\install-common.ps1"
Ensure-HephaestusProfileEnv

$paths = Get-HephaestusInstallPaths

Write-Host '[uninstall] Stop DomainHost (service + processes)'
Stop-HephaestusWindowsService -Name 'domainhost'
Stop-HephaestusDotNetProcesses -Match 'DomainHost'

Write-Host '[uninstall] Stop Technitium DNS (service)'
Stop-HephaestusWindowsService -Name 'hephaestus-dns'

if (Test-Path -LiteralPath $paths.ReleaseDir) {
    Write-Host "[uninstall] Remove $($paths.ReleaseDir)"
    Remove-Item -LiteralPath $paths.ReleaseDir -Recurse -Force
}

Write-Host '[uninstall] Done.'
