# Windows: silent PostgreSQL + setup-postgres.sql (same schema as install-postgres.sh).
# Linux uses OS user postgres (peer). Windows: TCP password first; if that fails, temporary pg_hba trust on 127.0.0.1 + ALTER USER + SQL, then restore hba.
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

function Invoke-PsqlQuiet {
    param ([string]$PsqlPath, [array]$Arguments)
    # Native stderr becomes ErrorRecord; with $ErrorActionPreference Stop the script would terminate before we parse auth vs connection errors.
    $saved = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        [object[]]$lines = @(& $PsqlPath @Arguments 2>&1 | ForEach-Object { $_ })
        return [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Combined = ($lines | Out-String)
        }
    }
    finally {
        $ErrorActionPreference = $saved
    }
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

function Get-PostgreSqlDataDirectory {
    param ([string]$PsqlPath)
    $bin = Split-Path -Parent $PsqlPath
    $root = Split-Path $bin -Parent
    foreach ($rel in @('data', 'pgsql\data')) {
        $d = Join-Path $root $rel
        if ((Test-Path -LiteralPath $d) -and (Test-Path -LiteralPath (Join-Path $d 'PG_VERSION'))) {
            return $d
        }
    }
    return $null
}

function Invoke-PgCtl {
    param ([string]$PsqlPath, [array]$PgCtlArguments)
    $pgCtl = Join-Path (Split-Path -Parent $PsqlPath) 'pg_ctl.exe'
    if (-not (Test-Path -LiteralPath $pgCtl)) {
        return $false
    }
    $saved = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $pgCtl @PgCtlArguments | Out-Null
        return ($LASTEXITCODE -eq 0)
    }
    finally {
        $ErrorActionPreference = $saved
    }
}

function Get-PostgreSqlServices {
    # e.g. postgresql-x64-18, DisplayName "postgresql-x64-18 - PostgreSQL Server 18"
    Get-Service -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -match '(?i)postgres' -or $_.DisplayName -match '(?i)PostgreSQL'
        }
}

function Stop-PostgreSqlServices {
    Get-PostgreSqlServices | ForEach-Object {
        Write-Host "Stopping service $($_.Name)..."
        Stop-Service -InputObject $_ -Force -ErrorAction SilentlyContinue
    }
}

function Start-PostgreSqlServices {
    Get-PostgreSqlServices | ForEach-Object {
        $svc = $_
        if ($svc.Status -eq [System.ServiceProcess.ServiceControllerStatus]::Running) {
            return
        }
        Write-Host "Starting service $($svc.Name)..."
        try {
            Set-Service -InputObject $svc -StartupType Automatic -ErrorAction SilentlyContinue
            Start-Service -InputObject $svc -ErrorAction Stop
        }
        catch {
            Write-Host "WARNING: Could not start $($svc.Name): $($_.Exception.Message)"
        }
    }
}

function Start-PostgreSqlViaPgCtl {
    param ([string] $PsqlPath)
    if (-not $PsqlPath) { return }
    $bin = Split-Path -Parent $PsqlPath
    $pgCtl = Join-Path $bin 'pg_ctl.exe'
    $root = Split-Path $bin -Parent
    $dataCandidates = @(
        (Join-Path $root 'data'),
        (Join-Path $root 'pgsql\data')
    ) | Where-Object { Test-Path -LiteralPath $_ }

    if (-not (Test-Path -LiteralPath $pgCtl) -or $dataCandidates.Count -eq 0) {
        return
    }
    $dataDir = $dataCandidates[0]
    Write-Host "Starting cluster with pg_ctl (no / stopped service): $dataDir"
    $p = Start-Process -FilePath $pgCtl `
        -ArgumentList @('start', '-D', $dataDir, '-w', '-t', '120', '-l', (Join-Path $dataDir 'pg_ctl.log')) `
        -NoNewWindow -Wait -PassThru
    if ($p.ExitCode -ne 0) {
        Write-Host "WARNING: pg_ctl start exited with $($p.ExitCode) (see $dataDir\pg_ctl.log if present)."
    }
}

function Ensure-PostgreSqlRunning {
    param ([string] $PsqlPath)
    Start-PostgreSqlServices
    Start-Sleep -Seconds 2
    if (-not (Get-PostgreSqlServices | Where-Object { $_.Status -eq 'Running' })) {
        Start-PostgreSqlViaPgCtl -PsqlPath $PsqlPath
        Start-Sleep -Seconds 2
    }
}

function Test-PsqlLogin {
    param ([string]$PsqlPath, [string]$Password)
    # Use & not Start-Process -ArgumentList: Win32 quoting breaks `-c "SELECT …"` into multiple argv (you saw: extra argument "1;" ignored).
    $env:PGPASSWORD = $Password
    $env:PGHOST = '127.0.0.1'
    $env:PGPORT = if ($env:PGPORT) { $env:PGPORT } else { '5432' }
    $env:PGUSER = 'postgres'
    $r = Invoke-PsqlQuiet -PsqlPath $PsqlPath -Arguments @(
        '-w',
        '-h', '127.0.0.1',
        '-p', $env:PGPORT,
        '-U', 'postgres',
        '-d', 'postgres',
        '-c',
        'SELECT 1'
    )
    return ($r.ExitCode -eq 0)
}

function Test-PsqlOutputLooksLikeWrongPassword {
    param ([string]$Text)
    if (-not $Text) { return $false }
    # English + Russian — Cyrillic MUST be \u escapes so Windows PowerShell parses the script (ANSI default corrupts literals).
    # Matches e.g. "password authentication failed" or Russian "...(по паролю)" / "проверку подлинности".
    return [bool](
        $Text -match '(?i)password authentication failed|authentication failed|FATAL:\s+password|\(\u043f\u043e \u043f\u0430\u0440\u043e\u043b\u044e\)|\u043f\u0440\u043e\u0432\u0435\u0440\u043a\u0443 \u043f\u043e\u0434\u043b\u0438\u043d\u043d\u043e\u0441\u0442\u0438'
    )
}

function Wait-PostgreSqlReady {
    param (
        [string] $PgIsReadyPath,
        [string] $PsqlPath,
        [string] $Password
    )
    # Match Linux intent: wait until server listens (like pg_isready). Password is applied when running SQL (peer/trust equiv. on Windows below).
    $env:PGHOST = '127.0.0.1'
    $env:PGPORT = if ($env:PGPORT) { $env:PGPORT } else { '5432' }

    Ensure-PostgreSqlRunning -PsqlPath $PsqlPath

    Write-Host '  Listening check (pg_isready only), max ~3 minutes...'
    $waitStart = Get-Date
    $deadline = (Get-Date).AddMinutes(3)
    $attempt = 0
    while ((Get-Date) -lt $deadline) {
        # Chocolatey leaves the DB cluster stopped often; retry start periodically until listening.
        $attempt++
        if ($attempt % 8 -eq 0) {
            Start-PostgreSqlServices
            $elapsed = [int]([TimeSpan]::FromTicks((Get-Date).Ticks - $waitStart.Ticks)).TotalSeconds
            Write-Host "  ... still waiting for PostgreSQL (${elapsed}s, port $($env:PGPORT)) ..."
        }
        if ($PgIsReadyPath) {
            $saved = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            try {
                & $PgIsReadyPath @('-h', '127.0.0.1', '-p', $env:PGPORT, '-U', 'postgres') | Out-Null
                $readyCode = $LASTEXITCODE
            }
            finally {
                $ErrorActionPreference = $saved
            }
            if ($readyCode -eq 0) {
                return $true
            }
        }
        else {
            $env:PGPASSWORD = $Password
            $env:PGUSER = 'postgres'
            if (Test-PsqlLogin -PsqlPath $PsqlPath -Password $Password) {
                return $true
            }
        }
        Start-Sleep -Seconds 2
    }
    Write-Host 'PostgreSQL services (for troubleshooting):'
    Get-PostgreSqlServices | Format-Table Name, Status, StartType -AutoSize | Out-String | Write-Host
    Write-Warning ("PostgreSQL did not become ready in time (port {0})." -f $env:PGPORT)
    return $false
}

function Invoke-SetupSql {
    param ([string]$PsqlPath, [string]$SqlPath, [string]$Password)
    $env:PGPASSWORD = $Password
    $env:PGHOST = '127.0.0.1'
    $env:PGPORT = if ($env:PGPORT) { $env:PGPORT } else { '5432' }
    $env:PGUSER = 'postgres'
    $env:PGDATABASE = 'postgres'
    $r = Invoke-PsqlQuiet -PsqlPath $PsqlPath -Arguments @(
        '-w',
        '-v', 'ON_ERROR_STOP=1',
        '-h', '127.0.0.1',
        '-p', $env:PGPORT,
        '-U', 'postgres',
        '-d', 'postgres',
        '-f',
        $SqlPath
    )
    if ($r.ExitCode -ne 0) {
        Write-Warning "setup-postgres.sql failed (exit $($r.ExitCode))."
        return $false
    }
    return $true
}

function Invoke-SetupSqlViaTemporaryTrust {
    param ([string]$PsqlPath, [string]$SqlPath, [string]$Password)
    # Linux runs as OS user postgres (peer). Windows: temporary host trust on 127.0.0.1, reload, run SQL + set password, restore pg_hba.
    $dataDir = Get-PostgreSqlDataDirectory -PsqlPath $PsqlPath
    if (-not $dataDir) {
        Write-Warning 'Could not find PostgreSQL data directory for trust bootstrap.'
        return $false
    }
    $hba = Join-Path $dataDir 'pg_hba.conf'
    if (-not (Test-Path -LiteralPath $hba)) {
        Write-Warning "Missing pg_hba.conf: $hba"
        return $false
    }
    $bak = "$hba.pre_install_postgres_bak"
    Copy-Item -LiteralPath $hba -Destination $bak -Force
    try {
        $prepend = @(
            '# Temporary rule added by install-postgres.ps1 (removed after setup)',
            'host all all 127.0.0.1/32 trust'
        )
        $existing = Get-Content -LiteralPath $hba
        ($prepend + $existing) | Set-Content -LiteralPath $hba -Encoding ascii

        if (-not (Invoke-PgCtl -PsqlPath $PsqlPath -PgCtlArguments @('reload', '-D', $dataDir))) {
            Write-Warning 'pg_ctl reload failed.'
            return $false
        }

        Start-Sleep -Milliseconds 500
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
        $env:PGHOST = '127.0.0.1'
        $env:PGPORT = if ($env:PGPORT) { $env:PGPORT } else { '5432' }
        $env:PGUSER = 'postgres'
        $pwEsc = $Password.Replace("'", "''")
        $alter = "ALTER USER postgres WITH PASSWORD '$pwEsc';"
        $r = Invoke-PsqlQuiet -PsqlPath $PsqlPath -Arguments @(
            '-w',
            '-v', 'ON_ERROR_STOP=1',
            '-h', '127.0.0.1',
            '-p', $env:PGPORT,
            '-U', 'postgres',
            '-d', 'postgres',
            '-c', $alter,
            '-f',
            $SqlPath
        )
        if ($r.ExitCode -ne 0) {
            Write-Warning "Trust bootstrap SQL failed (exit $($r.ExitCode))."
            return $false
        }
        return $true
    }
    finally {
        if (Test-Path -LiteralPath $bak) {
            Copy-Item -LiteralPath $bak -Destination $hba -Force
            Remove-Item -LiteralPath $bak -Force -ErrorAction SilentlyContinue
            Invoke-PgCtl -PsqlPath $PsqlPath -PgCtlArguments @('reload', '-D', $dataDir) | Out-Null
        }
    }
}

function Invoke-SetupSqlLinuxStyle {
    param ([string]$PsqlPath, [string]$SqlPath, [string]$Password)
    foreach ($hostTry in @('127.0.0.1', 'localhost')) {
        $env:PGPASSWORD = $Password
        $env:PGHOST = $hostTry
        $env:PGPORT = if ($env:PGPORT) { $env:PGPORT } else { '5432' }
        $env:PGUSER = 'postgres'
        $env:PGDATABASE = 'postgres'
        $r = Invoke-PsqlQuiet -PsqlPath $PsqlPath -Arguments @(
            '-w',
            '-v', 'ON_ERROR_STOP=1',
            '-h', $hostTry,
            '-p', $env:PGPORT,
            '-U', 'postgres',
            '-d', 'postgres',
            '-f',
            $SqlPath
        )
        if ($r.ExitCode -eq 0) {
            return $true
        }
    }
    Write-Host 'TCP auth failed for setup-postgres.sql; trying temporary pg_hba trust on 127.0.0.1 (same goal as Linux run_as_postgres)...'
    return (Invoke-SetupSqlViaTemporaryTrust -PsqlPath $PsqlPath -SqlPath $SqlPath -Password $Password)
}

function Install-PostgreSqlChocolatey {
    Write-Host 'Stopping PostgreSQL services (if any)...'
    Stop-PostgreSqlServices

    Write-Host 'Removing Chocolatey postgresql package...'
    & choco.exe uninstall postgresql -y 2>$null | Out-Null

    Write-Host "Installing PostgreSQL via Chocolatey (superuser postgres / password: $PostgresPassword)..."
    & choco.exe install postgresql -y --no-progress --params "/Password:$PostgresPassword"
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 3010) {
        Write-Error "choco install postgresql failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    Start-Sleep -Seconds 3
    Start-PostgreSqlServices
}

Ensure-Chocolatey

Refresh-Path
$psql = Get-PsqlExecutable

if (-not $psql) {
    Write-Host 'psql.exe not found - installing PostgreSQL via Chocolatey...'
    Install-PostgreSqlChocolatey
    Refresh-Path
    $psql = Get-PsqlExecutable
    if (-not $psql) {
        Write-Error 'psql.exe still not found after install. Open a new elevated prompt and re-run.'
        exit 1
    }
}
else {
    Write-Host 'Ensuring PostgreSQL service is running...'
    Ensure-PostgreSqlRunning -PsqlPath $psql

    Write-Host 'Testing postgres/postgres (single probe)...'
    $env:PGPASSWORD = $PostgresPassword
    $env:PGHOST = '127.0.0.1'
    $env:PGPORT = if ($env:PGPORT) { $env:PGPORT } else { '5432' }
    $env:PGUSER = 'postgres'
    $probe = Invoke-PsqlQuiet -PsqlPath $psql -Arguments @(
        '-w', '-h', '127.0.0.1', '-p', $env:PGPORT,
        '-U', 'postgres', '-d', 'postgres', '-c', 'SELECT 1'
    )

    if ($probe.ExitCode -ne 0) {
        $looksWrongPw = Test-PsqlOutputLooksLikeWrongPassword -Text $probe.Combined
        $looksConn = $probe.Combined -match '(?i)could not connect|connection refused|no response|timeout|timed out'

        if (-not $looksWrongPw -and $looksConn) {
            Write-Host 'Server not reachable yet; one retry in 5s...'
            Start-Sleep -Seconds 5
            Ensure-PostgreSqlRunning -PsqlPath $psql
            $probe = Invoke-PsqlQuiet -PsqlPath $psql -Arguments @(
                '-w', '-h', '127.0.0.1', '-p', $env:PGPORT,
                '-U', 'postgres', '-d', 'postgres', '-c', 'SELECT 1'
            )
        }

        if ($probe.ExitCode -ne 0) {
            Write-Host 'Login failed (wrong password or broken cluster). Reinstalling PostgreSQL with postgres/postgres...'
            Install-PostgreSqlChocolatey
            Refresh-Path
            $psql = Get-PsqlExecutable
            if (-not $psql) {
                Write-Error 'psql.exe not found after reinstall.'
                exit 1
            }
        }
        else {
            Write-Host 'Login OK after retry.'
        }
    }
    else {
        Write-Host 'Login OK.'
    }
}

Write-Host "Using psql: $psql"

Ensure-PostgreSqlRunning -PsqlPath $psql
$pgReady = Get-PgIsReadyExecutable -PsqlPath $psql
Write-Host 'Waiting for PostgreSQL (shows progress every ~16s if slow)...'
if (-not (Wait-PostgreSqlReady -PgIsReadyPath $pgReady -PsqlPath $psql -Password $PostgresPassword)) {
    Write-Error "PostgreSQL did not become ready on port $($env:PGPORT). Check services and Chocolatey log."
    exit 1
}

Write-Host 'Applying setup-postgres.sql...'
if (-not (Invoke-SetupSqlLinuxStyle -PsqlPath $psql -SqlPath $SqlFile -Password $PostgresPassword)) {
    Write-Error 'setup-postgres.sql failed.'
    exit 1
}

Write-Host "Done: hephaestus database and objects installed."
exit 0
