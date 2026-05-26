#Requires -RunAsAdministrator
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\install-common.ps1"

$paths = Get-HephaestusInstallPaths
$installDir = $paths.TechniDnsDir
$buildDir = $paths.TechniBuildDir
$installProj = $paths.InstallProj

if (-not (Test-CommandExists 'git')) {
    throw 'git not found. Run install\win\install-git.ps1 first (or install\install.bat).'
}
if (-not (Test-DotNet10Sdk)) {
    throw 'dotnet not found. Run install\win\install-net.ps1 first (or install\install.bat).'
}
if (-not (dotnet --info 2>$null)) {
    throw 'dotnet is not runnable. Re-run install\win\install-net.ps1.'
}
if (-not (Test-Path -LiteralPath $installProj)) {
    throw "Missing install project: $installProj"
}

Write-Host '[dns 1] Build hephaestus-install (Technitium password from panel/Commons/appsettings.json)'
& dotnet build $installProj -c Release -v minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
Push-Location $buildDir
try {
    $libDir = Join-Path $buildDir 'TechnitiumLibrary'
    $dnsDir = Join-Path $buildDir 'DnsServer'

    if (-not (Test-Path -LiteralPath (Join-Path $libDir '.git'))) {
        if (Test-Path -LiteralPath $libDir) { Remove-Item -LiteralPath $libDir -Recurse -Force }
        Write-Host '[dns] git clone TechnitiumLibrary...'
        & git clone --depth 1 https://github.com/TechnitiumSoftware/TechnitiumLibrary.git TechnitiumLibrary
        if ($LASTEXITCODE -ne 0) { throw 'git clone TechnitiumLibrary failed' }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $dnsDir '.git'))) {
        if (Test-Path -LiteralPath $dnsDir) { Remove-Item -LiteralPath $dnsDir -Recurse -Force }
        Write-Host '[dns] git clone DnsServer...'
        & git clone --depth 1 https://github.com/TechnitiumSoftware/DnsServer.git DnsServer
        if ($LASTEXITCODE -ne 0) { throw 'git clone DnsServer failed' }
    }

    Write-Host '[dns] dotnet build TechnitiumLibrary dependencies...'
    & dotnet build TechnitiumLibrary/TechnitiumLibrary.ByteTree/TechnitiumLibrary.ByteTree.csproj -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet build TechnitiumLibrary/TechnitiumLibrary.Net/TechnitiumLibrary.Net.csproj -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet build TechnitiumLibrary/TechnitiumLibrary.Security.OTP/TechnitiumLibrary.Security.OTP.csproj -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host '[dns] dotnet publish DnsServerApp...'
    & dotnet publish DnsServer/DnsServerApp/DnsServerApp.csproj -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $publish = Find-TechnitiumPublishDir -BuildDir $buildDir
    if (-not $publish) {
        throw 'Could not find publish output under DnsServer/DnsServerApp/bin/Release'
    }

    Write-Host "[dns] Copy publish -> $installDir"
    Stop-HephaestusWindowsService -Name 'hephaestus-dns'
    if (Test-Path -LiteralPath $installDir) {
        Remove-Item -LiteralPath $installDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
    Copy-Item -Path (Join-Path $publish '*') -Destination $installDir -Recurse -Force
    Grant-HephaestusDataDirectoryAccess -Directory $installDir

    $dnsExePath = Join-Path $installDir 'DnsServerApp.exe'
    if (-not (Test-Path -LiteralPath $dnsExePath)) {
        $dll = Join-Path $installDir 'DnsServerApp.dll'
        if (-not (Test-Path -LiteralPath $dll)) {
            throw "No DnsServerApp.exe or DnsServerApp.dll in $installDir"
        }
        throw 'DnsServerApp.exe missing after publish; Technitium must run as a native exe (NSSM cannot host dotnet.dll alone). Re-run publish or install-net.'
    }

    # Technitium DnsServerApp is a console host (no SCM handshake). Raw New-Service -> error 1053.
    Write-Host '[dns] Register Windows service hephaestus-dns (NSSM)'
    Install-HephaestusNssmService `
        -Name 'hephaestus-dns' `
        -DisplayName 'Hephaestus Technitium DNS' `
        -Application $dnsExePath `
        -AppDirectory $installDir `
        -Description 'Technitium DNS Server (local resolver)'

    Set-LocalDnsToLoopback
}
finally {
    Pop-Location
}

Write-Host '[dns] Apply Technitium admin password + forwarders/recursion (install/Install)'
$installExit = 0
try {
    & dotnet run --project $installProj -c Release -v minimal
    $installExit = $LASTEXITCODE
}
catch {
    Write-Warning $_.Exception.Message
    $installExit = 1
}
if ($installExit -ne 0) {
    Write-Warning "hephaestus-install exited $installExit; set Technitium admin password in web UI if needed."
}

Write-Host "Technitium DNS Server installed under $installDir."
Write-Host 'Web console: http://localhost:5380/'
Write-Host 'See also: https://github.com/TechnitiumSoftware/DnsServer/blob/master/build.md'
