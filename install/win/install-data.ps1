#Requires -RunAsAdministrator
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\install-common.ps1"
Ensure-HephaestusProfileEnv

if (-not (Test-CommandExists 'git')) {
    throw 'git not on PATH. Run install\win\install-git.ps1 first.'
}

. "$PSScriptRoot\..\shared\crypt-git-pat.ps1"

$paths = Get-HephaestusInstallPaths
$dataDir = Get-HephaestusDataDirectory
$pat = Read-GitPatFromEncryptedFile
$cloneUrl = "https://x-access-token:${pat}@github.com/kgonsovskii/hephaestus_data.git"

Write-Host "[install-data] Repo root: $($paths.RepoRoot)"
Write-Host "[install-data] Data dir (sibling): $dataDir"
Write-Host "[install-data] Remove existing $dataDir"
if (Test-Path -LiteralPath $dataDir) {
    Remove-Item -LiteralPath $dataDir -Recurse -Force
}

Write-Host '[install-data] Clone https://github.com/kgonsovskii/hephaestus_data.git'
$parent = Split-Path -Parent $dataDir
if (-not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}
& git clone $cloneUrl $dataDir
if ($LASTEXITCODE -ne 0) {
    throw "git clone hephaestus_data failed (exit $LASTEXITCODE)."
}

Write-Host '[install-data] Done.'
