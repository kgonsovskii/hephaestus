$ErrorActionPreference = 'Stop'
$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
Set-Location -Path $here

$scripts = @(
    'install-1.ps1',
    'install-2.ps1',
    'install-misc.ps1',
    'install-dns.ps1',
    'install-sql.ps1',
    'install-sql2.ps1',
    'install-web.ps1',
    'install-web2.ps1',
    'install-trigger.ps1'
)

foreach ($name in $scripts) {
    $path = Join-Path -Path $here -ChildPath $name
    if (-not (Test-Path -Path $path)) {
        throw "Install script not found: $path"
    }
    Write-Host "=== Running $name ===" -ForegroundColor Cyan
    & $path
}

Write-Host '_INSTALL_COMPLETE_'
Write-Output '_INSTALL_COMPLETE_'