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

& $installPs1 *>&1 | Tee-Object -FilePath $logPath
