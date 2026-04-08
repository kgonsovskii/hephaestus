$ErrorActionPreference = "Stop"

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Run this script elevated (Administrator) for LocalMachine stores."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$pfxPath = Join-Path $repoRoot "cert\hephaestus.pfx"

if (-not (Test-Path -LiteralPath $pfxPath)) {
    Write-Error "PFX not found: $pfxPath — run CertTool from the repo first."
}

$emptyPassword = New-Object System.Security.SecureString

Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation Cert:\LocalMachine\My -Password $emptyPassword -Exportable | Out-Null
Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation Cert:\LocalMachine\Root -Password $emptyPassword -Exportable | Out-Null

Write-Host "Installed hephaestus.pfx to LocalMachine My and Root (no password)."
