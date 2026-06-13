#Requires -RunAsAdministrator
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\install-common.ps1"

if (-not (Test-CommandExists 'git')) {
    throw 'git not on PATH. Run install\win\install-git.ps1 first.'
}

$paths = Get-HephaestusInstallPaths
$dataDir = Get-HephaestusDataDirectory
$cloneUrl = 'https://x-access-token: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@github.com/kgonsovskii/hephaestus_data.git'

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
