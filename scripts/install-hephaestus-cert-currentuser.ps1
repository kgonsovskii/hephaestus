$ErrorActionPreference = "Stop"

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

Write-Warning 'Windows may still show one Security Warning for CurrentUser\Root; Microsoft does not document a supported fully silent path for this store.'
Write-Warning 'For hands-off trust on this PC, run install-hephaestus-cert-localmachine.ps1 as Administrator once (machine-wide root).'

& $certutil @("-user", "-addstore", "Root", $cerPath)
if ($LASTEXITCODE -eq 0) {
    Write-Host 'Trusted root installed (CurrentUser\Root) via certutil — Hephaestus Development Root CA'
    exit 0
}

Import-Certificate -FilePath $cerPath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
Write-Host 'Trusted root installed (CurrentUser\Root) via Import-Certificate — Hephaestus Development Root CA'
