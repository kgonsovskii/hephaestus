# Shared helpers for install/win/*.ps1 (Chocolatey ≈ apt). Data files: install/shared/.
Set-StrictMode -Version Latest

function Get-HephaestusInstallPaths {
    $winDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    $installRoot = (Resolve-Path (Join-Path $winDir '..')).Path
    $repoRoot = (Resolve-Path (Join-Path $installRoot '..')).Path
    $sharedDir = Join-Path $installRoot 'shared'
    [pscustomobject]@{
        WinDir      = $winDir
        InstallRoot = $installRoot
        SharedDir   = $sharedDir
        RepoRoot    = $repoRoot
        ReleaseDir    = Join-Path $repoRoot 'release'
        DomainHostDir = Join-Path $repoRoot 'release'
        TechniRoot  = Join-Path ${env:ProgramData} 'hephaestus\technitium'
        TechniDnsDir = Join-Path ${env:ProgramData} 'hephaestus\technitium\dns'
        TechniBuildDir = Join-Path ${env:ProgramData} 'hephaestus\technitium\build'
        InstallProj = Join-Path $installRoot 'Install\Install.csproj'
        Solution    = Join-Path $repoRoot 'panel.sln'
        DeployProj  = Join-Path $repoRoot 'panel\Deploy\Deploy.csproj'
        DomainHostDll = Join-Path $repoRoot 'release\DomainHost.dll'
        DomainHostExe = Join-Path $repoRoot 'release\DomainHost.exe'
        SetupPostgresSql = Join-Path $sharedDir 'setup-postgres.sql'
    }
}

function Get-HephaestusDataDirectory {
    $repoRoot = (Get-HephaestusInstallPaths).RepoRoot
    $parent = Split-Path -Parent $repoRoot
    Join-Path $parent 'hephaestus_data'
}

function Get-HephaestusDataGitHubToken {
    $fromEnv = $env:HEPHAESTUS_DATA_GITHUB_TOKEN
    if (-not [string]::IsNullOrWhiteSpace($fromEnv)) {
        return $fromEnv.Trim()
    }
    $paths = Get-HephaestusInstallPaths
    $file = Join-Path $paths.SharedDir 'install-data-creds.txt'
    if (-not (Test-Path -LiteralPath $file)) {
        throw "Missing GitHub PAT: set HEPHAESTUS_DATA_GITHUB_TOKEN or create $file (one line: PAT with repo read access)."
    }
    $token = (Get-Content -LiteralPath $file -TotalCount 1 -ErrorAction Stop).Trim()
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "Empty GitHub PAT in $file."
    }
    return $token
}

function Test-CommandExists([string]$Name) {
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Refresh-InstallPath {
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' +
        [Environment]::GetEnvironmentVariable('Path', 'User')
}

function Ensure-Chocolatey {
    if (Test-CommandExists 'choco') {
        return
    }
    Write-Host '[install] Installing Chocolatey...'
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol =
        [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
    Refresh-InstallPath
    if (-not (Test-CommandExists 'choco')) {
        throw 'Chocolatey not on PATH after install. Open a new elevated prompt and re-run.'
    }
}

function Invoke-ChocoInstall {
    param([Parameter(Mandatory)][string[]]$Packages)
    Ensure-Chocolatey
    $args = @('install') + $Packages + @('-y', '--no-progress', '--force')
    & choco @args
    if ($LASTEXITCODE -ne 0) {
        throw "choco install failed (exit $LASTEXITCODE): $($Packages -join ', ')"
    }
    Refresh-InstallPath
}

function Test-DotNet10Sdk {
    if (-not (Test-CommandExists 'dotnet')) { return $false }
    $lines = @(& dotnet --list-sdks 2>$null)
    foreach ($line in $lines) {
        if ($line -match '^\s*10\.0\.') { return $true }
    }
    return $false
}

function Ensure-Nssm {
    if (Test-CommandExists 'nssm') {
        return
    }
    Write-Host '[install] Installing NSSM (console app -> Windows service wrapper)...'
    Invoke-ChocoInstall -Packages @('nssm')
    if (-not (Test-CommandExists 'nssm')) {
        throw 'nssm not on PATH after install. Open a new elevated prompt and re-run.'
    }
}

function Test-HephaestusNssmService {
    param([Parameter(Mandatory)][string]$Name)
    if (-not (Test-CommandExists 'nssm')) {
        return $false
    }
    $out = & nssm status $Name 2>&1
    return $LASTEXITCODE -eq 0 -and ($out -notmatch 'SERVICE_NOT_INSTALLED|Can''t open service')
}

function Grant-HephaestusDataDirectoryAccess {
    param([Parameter(Mandatory)][string]$Directory)
    if (-not (Test-Path -LiteralPath $Directory)) {
        return
    }
    Write-Host "[install] ACLs for $Directory (LocalSystem + Administrators)..."
    & icacls $Directory /inheritance:e | Out-Null
    & icacls $Directory /grant 'SYSTEM:(OI)(CI)F' 'Administrators:(OI)(CI)F' | Out-Null
    $config = Join-Path $Directory 'config'
    if (Test-Path -LiteralPath $config) {
        & icacls $config /inheritance:e | Out-Null
        & icacls $config /grant 'SYSTEM:(OI)(CI)F' 'Administrators:(OI)(CI)F' 'Users:(OI)(CI)M' | Out-Null
    }
}

function Stop-HephaestusWindowsService {
    param([Parameter(Mandatory)][string]$Name)
    $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $svc) { return }
    if ($svc.Status -ne 'Stopped') {
        Write-Host "[install] Stopping service $Name..."
        if (Test-HephaestusNssmService -Name $Name) {
            & nssm stop $Name | Out-Null
        }
        else {
            Stop-Service -Name $Name -Force -ErrorAction SilentlyContinue
        }
        $svc.WaitForStatus('Stopped', (New-TimeSpan -Seconds 30))
    }
}

function Remove-HephaestusWindowsService {
    param([Parameter(Mandatory)][string]$Name)
    Stop-HephaestusWindowsService -Name $Name
    $existing = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $existing) {
        return
    }
    Write-Host "[install] Removing service $Name..."
    if (Test-HephaestusNssmService -Name $Name) {
        & nssm remove $Name confirm | Out-Null
    }
    else {
        sc.exe delete $Name | Out-Null
    }
    for ($i = 0; $i -lt 20; $i++) {
        if (-not (Get-Service -Name $Name -ErrorAction SilentlyContinue)) {
            break
        }
        Start-Sleep -Seconds 1
    }
}

function Format-ServiceBinaryPathName {
    param([Parameter(Mandatory)][string[]]$Command)
    # SCM binary path: quote each segment (required when paths contain spaces).
    ($Command | ForEach-Object { "`"$_`"" }) -join ' '
}

function Install-HephaestusWindowsService {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$DisplayName,
        [Parameter(Mandatory)][string[]]$BinPathCommand,
        [string]$Description = '',
        [string]$AppDirectory = ''
    )
    if ($BinPathCommand.Count -lt 1) {
        throw 'BinPathCommand requires at least one executable path.'
    }

    Remove-HephaestusWindowsService -Name $Name

    $binaryPathName = Format-ServiceBinaryPathName -Command $BinPathCommand
    Write-Host "[install] Creating service $Name : $binaryPathName"

    try {
        New-Service -Name $Name -BinaryPathName $binaryPathName -DisplayName $DisplayName -StartupType Automatic `
            -ErrorAction Stop | Out-Null
    }
    catch {
        throw "Failed to create service ${Name}: $($_.Exception.Message). BinaryPathName=$binaryPathName"
    }

    if ($Description) {
        sc.exe description $Name $Description | Out-Null
    }
    if ($AppDirectory) {
        sc.exe config $Name AppDirectory= "`"$AppDirectory`"" | Out-Null
    }

    try {
        Start-Service -Name $Name -ErrorAction Stop
    }
    catch {
        $query = (& sc.exe query $Name 2>&1 | Out-String).Trim()
        $manual = if ($AppDirectory -and $BinPathCommand.Count -gt 0) {
            $tryExe = $BinPathCommand[0]
            "Manual test: cd `"$AppDirectory`"; & `"$tryExe`""
        } else {
            ''
        }
        throw @(
            "Start-Service '$Name' failed: $($_.Exception.Message)",
            $query,
            $manual,
            'Check Windows Event Viewer (Application) for .NET Runtime / DomainHost errors.'
        ) -join [Environment]::NewLine
    }

    $svc = Get-Service -Name $Name
    if ($svc.Status -ne 'Running') {
        throw "Service $Name did not reach Running state (status: $($svc.Status))."
    }
    Write-Host "[install] Service $Name is running."
}

function Install-HephaestusNssmService {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$DisplayName,
        [Parameter(Mandatory)][string]$Application,
        [string]$Description = '',
        [Parameter(Mandatory)][string]$AppDirectory
    )
    if (-not (Test-Path -LiteralPath $Application)) {
        throw "Application not found: $Application"
    }
    if (-not (Test-Path -LiteralPath $AppDirectory)) {
        throw "AppDirectory not found: $AppDirectory"
    }

    Ensure-Nssm
    Remove-HephaestusWindowsService -Name $Name

    Write-Host "[install] Creating NSSM service $Name : $Application"
    & nssm install $Name $Application | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "nssm install $Name failed (exit $LASTEXITCODE)"
    }
    & nssm set $Name AppDirectory $AppDirectory | Out-Null
    & nssm set $Name DisplayName $DisplayName | Out-Null
    & nssm set $Name Start SERVICE_AUTO_START | Out-Null
    if ($Description) {
        & nssm set $Name Description $Description | Out-Null
    }

    try {
        & nssm start $Name | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "nssm start exited $LASTEXITCODE"
        }
    }
    catch {
        $status = (& nssm status $Name 2>&1 | Out-String).Trim()
        throw @(
            "nssm start '$Name' failed: $($_.Exception.Message)",
            $status,
            "Manual test: cd `"$AppDirectory`"; & `"$Application`""
        ) -join [Environment]::NewLine
    }

    $deadline = (Get-Date).AddSeconds(45)
    while ((Get-Date) -lt $deadline) {
        $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
        if ($svc -and $svc.Status -eq 'Running') {
            Write-Host "[install] Service $Name is running (NSSM)."
            return
        }
        Start-Sleep -Seconds 2
    }
    throw "Service $Name did not reach Running state within 45s."
}

function Stop-HephaestusDotNetProcesses {
    param([string]$Match = 'DomainHost')
    Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and $_.CommandLine -like "*$Match*" } |
        ForEach-Object {
            Write-Host "[install] Stopping dotnet PID $($_.ProcessId) ($Match)..."
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }
}

function Find-TechnitiumPublishDir {
    param([Parameter(Mandatory)][string]$BuildDir)
    $candidates = @(
        (Join-Path $BuildDir 'DnsServer\DnsServerApp\bin\Release\net10.0\win-x64\publish')
        (Join-Path $BuildDir 'DnsServer\DnsServerApp\bin\Release\net10.0\publish')
        (Join-Path $BuildDir 'DnsServer\DnsServerApp\bin\Release\publish')
    )
    foreach ($dir in (Get-ChildItem -Path (Join-Path $BuildDir 'DnsServer\DnsServerApp\bin\Release') -Directory -Recurse -Filter 'publish' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)) {
        $candidates += $dir
    }
    foreach ($path in $candidates) {
        if ((Test-Path -LiteralPath $path) -and (Get-ChildItem -LiteralPath $path -Filter '*.dll' -ErrorAction SilentlyContinue)) {
            return $path
        }
    }
    return $null
}

function Set-LocalDnsToLoopback {
    Write-Host '[install] Setting DNS on active adapters to 127.0.0.1 ...'
    $adapters = Get-NetAdapter -Physical -ErrorAction SilentlyContinue |
        Where-Object { $_.Status -eq 'Up' }
    foreach ($a in $adapters) {
        try {
            Set-DnsClientServerAddress -InterfaceIndex $a.ifIndex -ServerAddresses @('127.0.0.1') -ErrorAction Stop
        }
        catch {
            Write-Warning "Could not set DNS on $($a.Name): $($_.Exception.Message)"
        }
    }
}
