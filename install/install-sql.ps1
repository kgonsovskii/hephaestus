$ErrorActionPreference = 'Stop'

$sqlPkg = 'sql-server-express'
$sqlVer = '2022.16.0.20260305'

choco install $sqlPkg --version=$sqlVer --yes --ignore-checksums
choco install sqlserver-cmdlineutils --yes --ignore-checksums

function Refresh-PathEnv {
    $m = [System.Environment]::GetEnvironmentVariable('Path', 'Machine')
    $u = [System.Environment]::GetEnvironmentVariable('Path', 'User')
    $env:Path = "$m;$u"
}

function Test-SqlExpressOk {
    param(
        [string] $ServerInstance = 'localhost\SQLEXPRESS'
    )
    $svc = Get-Service -Name 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue
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
    param([int] $TimeoutSec = 15)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if (Test-SqlExpressOk) {
            return $true
        }
        Start-Sleep -Seconds 5
    }
    Test-SqlExpressOk
}

Refresh-PathEnv

if (-not (Wait-SqlExpressOk)) {
    Write-Host 'SQL Server Express not reachable; reinstalling via Chocolatey (--force)...' -ForegroundColor Yellow
    choco install $sqlPkg --version=$sqlVer --force --yes --ignore-checksums
    Refresh-PathEnv
    if (-not (Wait-SqlExpressOk -TimeoutSec 180)) {
        throw 'SQL Server Express still not reachable after Chocolatey reinstall.'
    }
}

Write-Host 'SQL Server Express is reachable.' -ForegroundColor Green
