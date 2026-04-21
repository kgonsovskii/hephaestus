# Windows: silent PostgreSQL + setup-postgres.sql (same schema as install-postgres.sh).
# Fixed superuser password "postgres" (not configurable). If login fails, Chocolatey reinstall resets it.
# Uses Chocolatey + psql / pg_isready. No prompts, no parameters.
#Requires -RunAsAdministrator

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Single dev password — used for choco /Password and PGPASSWORD (not passed on the command line).
$PostgresPassword = 'postgres'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SqlFile = Join-Path $ScriptDir 'setup-postgres.sql'

if (-not (Test-Path -LiteralPath $SqlFile)) {
    Write-Error "Missing: $SqlFile"
    exit 1
}

function Test-CommandExists([string]$Name) {
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Ensure-Chocolatey {
    if (Test-CommandExists 'choco') {
        return
    }
    Write-Host 'Installing Chocolatey...'
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
    $machinePath = [Environment]::GetEnvironmentVariable('Path', 'Machine')
    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $env:Path = "$machinePath;$userPath"
    if (-not (Test-CommandExists 'choco')) {
        Write-Error 'Chocolatey not on PATH after install. Open a new elevated prompt and re-run.'
        exit 1
    }
}

function Refresh-Path {
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
}

function Get-PsqlExecutable {
    Refresh-Path
    $which = Get-Command psql.exe -ErrorAction SilentlyContinue
    if ($which) {
        return $which.Source
    }
    $roots = @(
        (Join-Path $env:ProgramFiles 'PostgreSQL')
    )
    $pf86 = ${env:ProgramFiles(x86)}
    if ($pf86) {
        $roots += (Join-Path $pf86 'PostgreSQL')
    }
    foreach ($r in $roots) {
        if (-not $r -or -not (Test-Path -LiteralPath $r)) { continue }
        $found = Get-ChildItem -Path $r -Recurse -Filter 'psql.exe' -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($found) {
            return $found.FullName
        }
    }
    return $null
}

function Get-PgIsReadyExecutable([string]$PsqlPath) {
    $bin = Split-Path -Parent $PsqlPath
    $ready = Join-Path $bin 'pg_isready.exe'
    if (Test-Path -LiteralPath $ready) {
        return $ready
    }
    return $null
}

function Stop-PostgreSqlServices {
    Get-Service -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match 'postgresql' -or $_.DisplayName -match 'PostgreSQL' } |
        ForEach-Object {
            Write-Host "Stopping service $($_.Name)..."
            Stop-Service -InputObject $_ -Force -ErrorAction SilentlyContinue
        }
}

function Test-PsqlLogin {
    param ([string]$PsqlPath, [string]$Password)
    $env:PGPASSWORD = $Password
    $env:PGHOST = '127.0.0.1'
    $env:PGPORT = if ($env:PGPORT) { $env:PGPORT } else { '5432' }
    $env:PGUSER = 'postgres'
    $p = Start-Process -FilePath $PsqlPath `
        -ArgumentList @(
            '-w',
            '-h', '127.0.0.1',
            '-p', $env:PGPORT,
            '-U', 'postgres',
            '-d', 'postgres',
            '-c', 'SELECT 1;'
        ) `
        -NoNewWindow -Wait -PassThru
    return ($p.ExitCode -eq 0)
}

function Wait-PostgreSqlReady {
    param (
        [string] $PgIsReadyPath,
        [string] $PsqlPath,
        [string] $Password
    )
    $env:PGPASSWORD = $Password
    $env:PGHOST = '127.0.0.1'
    $env:PGPORT = if ($env:PGPORT) { $env:PGPORT } else { '5432' }
    $env:PGUSER = 'postgres'

    $deadline = (Get-Date).AddMinutes(3)
    while ((Get-Date) -lt $deadline) {
        if ($PgIsReadyPath) {
            $pr = Start-Process -FilePath $PgIsReadyPath `
                -ArgumentList @('-h', '127.0.0.1', '-p', $env:PGPORT, '-U', 'postgres') `
                -NoNewWindow -Wait -PassThru
            if ($pr.ExitCode -eq 0) {
                return
            }
        }
        else {
            if (Test-PsqlLogin -PsqlPath $PsqlPath -Password $Password) {
                return
            }
        }
        Start-Sleep -Seconds 2
    }
    Write-Error 'PostgreSQL did not become ready in time.'
    exit 1
}

function Invoke-SetupSql {
    param ([string]$PsqlPath, [string]$SqlPath, [string]$Password)
    $env:PGPASSWORD = $Password
    $env:PGHOST = '127.0.0.1'
    $env:PGPORT = if ($env:PGPORT) { $env:PGPORT } else { '5432' }
    $env:PGUSER = 'postgres'
    $env:PGDATABASE = 'postgres'
    $p = Start-Process -FilePath $PsqlPath `
        -ArgumentList @(
            '-w',
            '-v', 'ON_ERROR_STOP=1',
            '-h', '127.0.0.1',
            '-p', $env:PGPORT,
            '-U', 'postgres',
            '-d', 'postgres',
            '-f', $SqlPath
        ) `
        -NoNewWindow -Wait -PassThru
    if ($p.ExitCode -ne 0) {
        Write-Error "psql failed with exit code $($p.ExitCode)."
        exit $p.ExitCode
    }
}

function Install-PostgreSqlChocolatey {
    Write-Host 'Stopping PostgreSQL services (if any)...'
    Stop-PostgreSqlServices

    Write-Host 'Removing Chocolatey postgresql package (clean superuser password for silent setup)...'
    & choco.exe uninstall postgresql -y 2>$null | Out-Null

    Write-Host "Installing PostgreSQL via Chocolatey (superuser password: $PostgresPassword)..."
    & choco.exe install postgresql -y --no-progress --params "/Password:$PostgresPassword"
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 3010) {
        Write-Error "choco install postgresql failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

Ensure-Chocolatey
Refresh-Path

$psql = Get-PsqlExecutable
$canLogin = $false
if ($psql) {
    Write-Host 'Checking postgres login with fixed password...'
    $canLogin = Test-PsqlLogin -PsqlPath $psql -Password $PostgresPassword
}

if (-not $canLogin) {
    if (-not $psql) {
        Write-Host 'psql not found or cluster missing — installing PostgreSQL...'
    }
    else {
        Write-Host 'Could not authenticate as postgres with fixed password — reinstalling PostgreSQL via Chocolatey to reset it...'
    }
    Install-PostgreSqlChocolatey
    Refresh-Path
    $psql = Get-PsqlExecutable
    if (-not $psql) {
        Write-Error 'psql.exe not found after install. Re-open an elevated shell and re-run.'
        exit 1
    }
}

Write-Host "Using psql: $psql"

$pgReady = Get-PgIsReadyExecutable -PsqlPath $psql
Write-Host 'Waiting for PostgreSQL (pg_isready / psql)...'
Wait-PostgreSqlReady -PgIsReadyPath $pgReady -PsqlPath $psql -Password $PostgresPassword

Write-Host 'Applying setup-postgres.sql...'
Invoke-SetupSql -PsqlPath $psql -SqlPath $SqlFile -Password $PostgresPassword

Write-Host 'Done: hephaestus database and objects installed.'
exit 0
