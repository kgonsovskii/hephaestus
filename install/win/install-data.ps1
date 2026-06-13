#Requires -RunAsAdministrator
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\install-common.ps1"

$paths = Get-HephaestusInstallPaths
$dataDir = Get-HephaestusDataDirectory

Write-Host "[install-data] Repo root: $($paths.RepoRoot)"
Write-Host "[install-data] Data dir (sibling): $dataDir"
Write-Host '[install-data] Skipped: hephaestus_data is cloned/synced by DomainHost Git maintenance on start.'
