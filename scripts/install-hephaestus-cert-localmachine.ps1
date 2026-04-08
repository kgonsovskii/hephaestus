$ErrorActionPreference = "Stop"

$admin = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $admin.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error 'Run elevated (Administrator). This script installs the root into LocalMachine (all users).'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$cerCandidate = Join-Path $repoRoot "cert\hephaestus-root.cer"
if (-not (Test-Path -LiteralPath $cerCandidate)) {
    Write-Error ("Root certificate not found: " + $cerCandidate + " - run CertTool first.")
}
$cerPath = (Resolve-Path -LiteralPath $cerCandidate).Path

$certutil = Join-Path $env:WINDIR "System32\certutil.exe"
if (-not (Test-Path -LiteralPath $certutil)) {
    Write-Error "certutil.exe not found: $certutil"
}

function Install-RootViaSystemCertutil {
    $taskName = "HephaestusRoot_" + [guid]::NewGuid().ToString("N")
    $argLine = '-addstore -f Root "' + $cerPath.Replace('"', '""') + '"'
    $lastResult = 999
    try {
        $action = New-ScheduledTaskAction -Execute $certutil -Argument $argLine
        $principal = New-ScheduledTaskPrincipal -UserId "NT AUTHORITY\SYSTEM" -LogonType ServiceAccount -RunLevel Highest
        $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Minutes 2)
        Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings -Force | Out-Null
        Start-ScheduledTask -TaskName $taskName
        $deadline = (Get-Date).AddSeconds(60)
        do {
            Start-Sleep -Milliseconds 250
            $t = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
        } while ($null -ne $t -and $t.State -eq 'Running' -and (Get-Date) -lt $deadline)
        $info = Get-ScheduledTaskInfo -TaskName $taskName
        $lastResult = $info.LastTaskResult
    }
    catch {
        $lastResult = 998
    }
    finally {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    }
    return $lastResult
}

$code = Install-RootViaSystemCertutil
if ($code -eq 0) {
    Write-Host 'Trusted root installed (LocalMachine\Root) via SYSTEM certutil — Hephaestus Development Root CA'
    exit 0
}

Write-Warning ("SYSTEM certutil exit {0}; trying interactive certutil." -f $code)
$p = Start-Process -FilePath $certutil -ArgumentList @("-addstore", "-f", "Root", $cerPath) -Wait -PassThru -NoNewWindow
if ($p.ExitCode -eq 0) {
    Write-Host 'Trusted root installed (LocalMachine\Root) via certutil — Hephaestus Development Root CA'
    exit 0
}

Import-Certificate -FilePath $cerPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
Write-Host 'Trusted root installed (LocalMachine\Root) via Import-Certificate — Hephaestus Development Root CA'
