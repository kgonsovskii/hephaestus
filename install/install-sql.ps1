$ErrorActionPreference = 'Stop'

$sqlPkg = 'sql-server-express'
$sqlVer = '2022.16.0.20260305'
$instanceName = 'SQLEXPRESS'

function Refresh-PathEnv {
    $m = [System.Environment]::GetEnvironmentVariable('Path', 'Machine')
    $u = [System.Environment]::GetEnvironmentVariable('Path', 'User')
    $env:Path = "$m;$u"
}

function Write-SqlInstallDiagnostics {
    Write-Host ''
    Write-Host '=== SQL install diagnostics (retry path runs unattended removal before reinstall) ===' -ForegroundColor Yellow
    $chLog = Join-Path $env:ProgramData 'chocolatey\logs\chocolatey.log'
    if (Test-Path -LiteralPath $chLog) {
        Write-Host "Chocolatey log: $chLog"
    }
    $sqlRoot = Join-Path $env:ProgramFiles 'Microsoft SQL Server'
    if (Test-Path -LiteralPath $sqlRoot) {
        Get-ChildItem -LiteralPath $sqlRoot -Recurse -Filter 'Summary.txt' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 6 -ExpandProperty FullName |
            ForEach-Object { Write-Host "SQL Summary.txt: $_" }
    }
}

function Invoke-Choco {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments,
        [switch] $AllowNonZero
    )
    & choco @Arguments
    $code = $LASTEXITCODE
    if (-not $AllowNonZero -and $code -ne 0) {
        Write-SqlInstallDiagnostics
        throw "Chocolatey failed (exit $code). Command: choco $($Arguments -join ' ')"
    }
    return $code
}

function Stop-SqlExpressServices {
    $svcNames = @(
        "MSSQL`$$instanceName"
        "SQLTELEMETRY`$$instanceName"
        "SQLAgent`$$instanceName"
        "ReportServer`$$instanceName"
    )
    foreach ($n in $svcNames) {
        $s = Get-Service -Name $n -ErrorAction SilentlyContinue
        if ($s -and $s.Status -ne 'Stopped') {
            Write-Host "Stopping service: $n"
            Stop-Service -Name $n -Force -ErrorAction SilentlyContinue
        }
    }
    Start-Sleep -Seconds 3
}

function Get-SqlBootstrapSetupExe {
    $pf = ${env:ProgramFiles}
    $prefer = @(
        (Join-Path $pf 'Microsoft SQL Server\160\Setup Bootstrap\SQLServer2022\setup.exe')
        (Join-Path $pf 'Microsoft SQL Server\150\Setup Bootstrap\SQLServer2019\setup.exe')
    )
    foreach ($p in $prefer) {
        if (Test-Path -LiteralPath $p) {
            return $p
        }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $pf 'Microsoft SQL Server'))) {
        return $null
    }
    $hit = Get-ChildItem -LiteralPath (Join-Path $pf 'Microsoft SQL Server') -Recurse -Filter 'setup.exe' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match 'Setup Bootstrap' } |
        Sort-Object -Property FullName -Descending |
        Select-Object -First 1
    if ($hit) {
        return $hit.FullName
    }
    return $null
}

function Remove-SqlExpressUnattended {
    Write-Host '=== Removing existing SQL Server Express (unattended) ===' -ForegroundColor Cyan
    Stop-SqlExpressServices

    $setup = Get-SqlBootstrapSetupExe
    if ($setup) {
        Write-Host "Running SQL setup uninstall: $setup"
        $args = @(
            '/Q'
            '/ACTION=Uninstall'
            '/FEATURES=SQL'
            "/INSTANCENAME=$instanceName"
            '/IACCEPTSQLSERVERLICENSETERMS'
        )
        $p = Start-Process -FilePath $setup -ArgumentList $args -Wait -PassThru -NoNewWindow
        Write-Host "SQL setup.exe uninstall exit code: $($p.ExitCode)"
    } else {
        Write-Host 'No SQL Setup Bootstrap setup.exe found (nothing to uninstall via setup, or not yet installed).'
    }

    Write-Host 'Chocolatey uninstall sql-server-express (cleanup package)...'
    $null = Invoke-Choco -Arguments @('uninstall', $sqlPkg, '-y', '--ignore-checksums', '--ignore-package-exit-codes') -AllowNonZero

    Start-Sleep -Seconds 10
}

function Test-SqlExpressOk {
    param(
        [string] $ServerInstance = "localhost\$instanceName"
    )
    $svc = Get-Service -Name "MSSQL`$$instanceName" -ErrorAction SilentlyContinue
    if (-not $svc -or $svc.Status -ne 'Running') {
        return $false
    }
    Refresh-PathEnv
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        return $false
    }
    & sqlcmd.exe -S $ServerInstance -Q "SET NOCOUNT ON; SELECT 1" -b 2>&1 | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function Wait-SqlExpressOk {
    param([int] $TimeoutSec = 120)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if (Test-SqlExpressOk) {
            return $true
        }
        Start-Sleep -Seconds 5
    }
    Test-SqlExpressOk
}

Invoke-Choco -Arguments @('install', $sqlPkg, "--version=$sqlVer", '--yes', '--ignore-checksums')
Invoke-Choco -Arguments @('install', 'sqlserver-cmdlineutils', '--yes', '--ignore-checksums')

Refresh-PathEnv

if (-not (Wait-SqlExpressOk)) {
    Write-Host 'SQL not healthy after install; unattended remove then reinstall...' -ForegroundColor Yellow
    Remove-SqlExpressUnattended
    Invoke-Choco -Arguments @('install', $sqlPkg, "--version=$sqlVer", '--yes', '--ignore-checksums')
    Invoke-Choco -Arguments @('install', 'sqlserver-cmdlineutils', '--yes', '--ignore-checksums')
    Refresh-PathEnv
    if (-not (Wait-SqlExpressOk -TimeoutSec 180)) {
        Write-SqlInstallDiagnostics
        throw 'SQL Server Express still not reachable after automated remove/reinstall cycles.'
    }
}

Write-Host 'SQL Server Express is reachable.' -ForegroundColor Green
