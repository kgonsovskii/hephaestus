#Requires -RunAsAdministrator
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\install-common.ps1"

$paths = Get-HephaestusInstallPaths

if (-not (Test-DotNet10Sdk)) {
    throw 'dotnet not found. Run install\win\install-net.ps1 first (or install\install.bat).'
}
if (-not (Test-Path -LiteralPath $paths.Solution)) {
    throw "Missing solution: $($paths.Solution)"
}
if (-not (Test-Path -LiteralPath $paths.DeployProj)) {
    throw "Missing project: $($paths.DeployProj)"
}

Write-Host '[install-soft 1/6] Stop domainhost (service + processes)'
Stop-HephaestusWindowsService -Name 'domainhost'
Stop-HephaestusDotNetProcesses -Match 'DomainHost'
Start-Sleep -Seconds 1

Write-Host "[install-soft 2/6] Clean and create $($paths.ReleaseDir)"
if (Test-Path -LiteralPath $paths.ReleaseDir) {
    Remove-Item -LiteralPath $paths.ReleaseDir -Recurse -Force
}
New-Item -ItemType Directory -Path $paths.ReleaseDir -Force | Out-Null

Write-Host '[install-soft 3/6] dotnet restore (whole solution)'
& dotnet restore $paths.Solution --verbosity minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[install-soft 4/6] dotnet build Deploy -t:DeployDomain (win-x64) -> $($paths.ReleaseDir)"
& dotnet build $paths.DeployProj -c Release -t:DeployDomain -p:DeployRuntimeIdentifier=win-x64 --no-restore -v minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not (Test-Path -LiteralPath $paths.DomainHostDll)) {
    throw "Deploy did not produce DomainHost.dll at $($paths.DomainHostDll)"
}

$dotnetExe = (Get-Command dotnet -ErrorAction Stop).Source
$domainHostBin = "`"$dotnetExe`" `"$($paths.DomainHostDll)`""

Write-Host '[install-soft 5/6] Register Windows service domainhost (auto-start)'
Install-HephaestusWindowsService `
    -Name 'domainhost' `
    -DisplayName 'Hephaestus DomainHost' `
    -BinPath $domainHostBin `
    -AppDirectory $paths.ReleaseDir `
    -Description 'Hephaestus DomainHost (HTTP/HTTPS on 80/443)'

Write-Host '[install-soft 6/6] Verify domainhost service'
$svc = Get-Service -Name 'domainhost'
if ($svc.Status -ne 'Running') {
    throw "domainhost service is not Running (status: $($svc.Status))."
}
Write-Host 'domainhost service is active. Done.'
