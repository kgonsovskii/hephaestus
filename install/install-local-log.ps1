$ErrorActionPreference = 'Continue'
$ConfirmPreference = 'None'

$runEntryName = '_HephaestusBootInstall'
$runKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
Remove-ItemProperty -LiteralPath $runKey -Name $runEntryName -ErrorAction SilentlyContinue

$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$installPs1 = Join-Path $here 'install.ps1'
$deltaRoot = Split-Path (Split-Path $here -Parent) -Parent
$logPath = Join-Path $deltaRoot 'log.txt'

if (-not (Test-Path -LiteralPath $installPs1)) {
    throw "Missing $installPs1"
}

# Prefer transcript (captures Write-Host); many hosts launched from .cmd / Run reject it — then Tee-Object.
if (Test-Path -LiteralPath $logPath) {
    Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
}

$transcriptOn = $false
try {
    $null = Start-Transcript -LiteralPath $logPath -Force -ErrorAction Stop
    $transcriptOn = $true
} catch {
    $transcriptOn = $false
}

try {
    if ($transcriptOn) {
        & $installPs1 *>&1 | ForEach-Object { $_ }
    } else {
        & $installPs1 *>&1 | Tee-Object -LiteralPath $logPath
    }
} finally {
    if ($transcriptOn) {
        Stop-Transcript -ErrorAction SilentlyContinue
    }
}
