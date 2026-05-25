#Requires -RunAsAdministrator
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\install-common.ps1"

$force = $env:INSTALL_FORCE -eq '1'

if (-not $force -and (Test-DotNet10Sdk)) {
    Write-Host '.NET 10 SDK already present:'
    & dotnet --list-sdks
    & dotnet --version
    exit 0
}

Write-Host '[install-net] .NET 10 SDK via Chocolatey (dotnet-10.0-sdk)'
Invoke-ChocoInstall -Packages @('dotnet-10.0-sdk') -Force:$force

if (-not (Test-DotNet10Sdk)) {
    throw '.NET 10 SDK not detected after install. Open a new elevated prompt and re-run install\win\install-net.ps1'
}

if (-not (dotnet --info 2>$null)) {
    throw 'dotnet --info failed. Reinstall or repair dotnet-10.0-sdk.'
}

& dotnet --list-sdks
& dotnet --version
