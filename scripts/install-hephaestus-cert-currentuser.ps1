$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$pfxPath = Join-Path $repoRoot "cert\hephaestus.pfx"

if (-not (Test-Path -LiteralPath $pfxPath)) {
    Write-Error "PFX not found: $pfxPath — run CertTool from the repo first."
}

$emptyPassword = New-Object System.Security.SecureString

Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation Cert:\CurrentUser\My -Password $emptyPassword -Exportable | Out-Null
Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation Cert:\CurrentUser\Root -Password $emptyPassword -Exportable | Out-Null

Write-Host "Installed hephaestus.pfx to CurrentUser My and Root (no password)."
