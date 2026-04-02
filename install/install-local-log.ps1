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

# Log everything that goes to the console (including Write-Host) to $logPath, while still showing on screen.
if (Test-Path -LiteralPath $logPath) {
    Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
}
Start-Transcript -LiteralPath $logPath -Force -ErrorAction Stop
try {
    & $installPs1 *>&1 | ForEach-Object { $_ }
} finally {
    Stop-Transcript -ErrorAction SilentlyContinue
}
