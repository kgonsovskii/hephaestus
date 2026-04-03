$ErrorActionPreference = "Stop"

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole] "Administrator")) {
    exit 1
}

$chocoPath = "C:\ProgramData\chocolatey\bin\choco.exe"
if (-not (Test-Path $chocoPath)) {
    exit 1
}

& choco feature enable --name=showDownloadProgress
& choco install postgresql --yes
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $PSScriptRoot) {
    $PSScriptRoot = Split-Path -Parent -LiteralPath $MyInvocation.MyCommand.Path
}

$sqlPath = Join-Path $PSScriptRoot "install.sql"
if (-not (Test-Path -LiteralPath $sqlPath)) { exit 1 }

$psql = $null
foreach ($ver in @(18, 17, 16, 15, 14)) {
    $c = "C:\Program Files\PostgreSQL\$ver\bin\psql.exe"
    if (Test-Path -LiteralPath $c) {
        $psql = $c
        break
    }
}
if (-not $psql) {
    $g = Get-Command psql -ErrorAction SilentlyContinue
    if ($g) { $psql = $g.Source }
}
if (-not $psql) { exit 1 }

$psqlNorm = $psql.TrimEnd('\')
$binDir = Split-Path -Parent $psql
$pgCtl = Join-Path $binDir "pg_ctl.exe"

$installRoots = @(
    "HKLM:\SOFTWARE\PostgreSQL\Installations",
    "HKLM:\SOFTWARE\WOW6432Node\PostgreSQL\Installations"
)

$dataDir = $null
$installs = @()
foreach ($reg in $installRoots) {
    if (-not (Test-Path -LiteralPath $reg)) { continue }
    foreach ($key in Get-ChildItem -LiteralPath $reg -ErrorAction SilentlyContinue) {
        $p = Get-ItemProperty -LiteralPath $key.PSPath -ErrorAction SilentlyContinue
        $dd = $p.'Data Directory'
        if ($dd -and (Test-Path -LiteralPath $dd)) {
            $installs += [pscustomobject]@{ Base = $p.'Base Directory'; Data = $dd }
        }
    }
}
foreach ($i in $installs) {
    if ($i.Base) {
        $b = $i.Base.TrimEnd('\')
        if ($psqlNorm.StartsWith($b, [StringComparison]::OrdinalIgnoreCase)) {
            $dataDir = $i.Data
            break
        }
    }
}
if (-not $dataDir -and $installs.Count -gt 0) { $dataDir = $installs[0].Data }
if (-not $dataDir -and $psql -match 'PostgreSQL\\(\d+)\\') {
    $d = "C:\Program Files\PostgreSQL\$($Matches[1])\data"
    if (Test-Path -LiteralPath $d) { $dataDir = $d }
}
if (-not $dataDir) { exit 1 }

$dataDirNorm = [System.IO.Path]::GetFullPath($dataDir).TrimEnd('\')
$hbaPath = Join-Path $dataDir "pg_hba.conf"
if (-not (Test-Path -LiteralPath $hbaPath)) { exit 1 }

function Get-PostgresWindowsService {
    $ddLower = $dataDirNorm.ToLowerInvariant()
    $binLower = $binDir.ToLowerInvariant()
    $candidates = Get-CimInstance Win32_Service -ErrorAction SilentlyContinue | Where-Object { $_.PathName }
    foreach ($svc in $candidates) {
        $pn = $svc.PathName.ToLowerInvariant()
        if ($pn.Contains($ddLower)) { return $svc }
    }
    foreach ($svc in $candidates) {
        $pn = $svc.PathName.ToLowerInvariant()
        if ($pn.Contains($binLower) -and ($svc.Name -match 'postgres')) { return $svc }
    }
    return $null
}

function Restart-PostgresWindowsService {
    try {
        $svc = Get-PostgresWindowsService
        if (-not $svc) { return }
        Restart-Service -Name $svc.Name -Force -ErrorAction Stop
        $deadline = (Get-Date).AddSeconds(30)
        while ((Get-Service -Name $svc.Name -ErrorAction SilentlyContinue).Status -ne 'Running') {
            if ((Get-Date) -gt $deadline) { break }
            Start-Sleep -Milliseconds 30
        }
    } catch { }
}

function Invoke-PgCtlReload {
    if (-not (Test-Path -LiteralPath $pgCtl)) { return }
    try {
        & $pgCtl reload -D $dataDir -w 2>$null | Out-Null
    } catch { }
}

$beginMark = "# BEGIN_ACS_SETUP_TRUST"
$trustBlock = "$beginMark`r`nhost all all 127.0.0.1/32 trust`r`nhost all all ::1/128 trust`r`n# END_ACS_SETUP_TRUST`r`n"

$utf8 = [System.Text.UTF8Encoding]::new($false)
function Read-HbaText { [System.IO.File]::ReadAllText($hbaPath, $utf8) }
function Write-HbaText([string]$text) { [System.IO.File]::WriteAllText($hbaPath, $text, $utf8) }

function Psql-Scalar([string]$sql) {
    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        $args = @(
            "-U", "postgres",
            "-h", "127.0.0.1",
            "-d", "postgres",
            "-w", "-q", "-t", "-A",
            "-o", $tmp,
            "-c", $sql
        )
        & $psql @args 2>$null
        if ($LASTEXITCODE -ne 0) { return $null }
        return [System.IO.File]::ReadAllText($tmp).Trim()
    } finally {
        Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
    }
}

$hbaOriginal = $null
$hbaTouched = $false
try {
    $hbaOriginal = Read-HbaText
    if ($hbaOriginal -notmatch [regex]::Escape($beginMark)) {
        try {
            Write-HbaText ($trustBlock + $hbaOriginal)
            $hbaTouched = $true
        } catch {
            exit 1
        }
        Restart-PostgresWindowsService
        Invoke-PgCtlReload
        Start-Sleep -Milliseconds 500
    }

    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    & $psql -U postgres -h 127.0.0.1 -d postgres -q -v ON_ERROR_STOP=1 -w -f $sqlPath 2>$null
    if ($LASTEXITCODE -ne 0) { exit 1 }

    $row = Psql-Scalar "SELECT 1 FROM pg_database WHERE datname = 'hephaestus'"
    if ($null -eq $row) { exit 1 }

    if ($row -ne "1") {
        & $psql -U postgres -h 127.0.0.1 -d postgres -q -w -c "CREATE DATABASE hephaestus OWNER tss;" 2>$null
        if ($LASTEXITCODE -ne 0) { exit 1 }
    }
}
finally {
    if ($hbaTouched -and $null -ne $hbaOriginal) {
        try {
            Write-HbaText $hbaOriginal
            Restart-PostgresWindowsService
            Invoke-PgCtlReload
        } catch { }
    }
}
