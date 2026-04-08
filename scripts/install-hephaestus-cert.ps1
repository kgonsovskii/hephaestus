param(
    [string]$PfxPassword = "123"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$pfxPath = Join-Path $repoRoot "cert\hephaestus.pfx"
if (-not (Test-Path -LiteralPath $pfxPath)) {
    Write-Error ("PFX not found: " + $pfxPath + " - run CertTool first (same file DomainHost uses).")
}
$pfxPath = (Resolve-Path -LiteralPath $pfxPath).Path

$pfxBytes = [System.IO.File]::ReadAllBytes($pfxPath)
$flags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet
$full = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfxBytes, $PfxPassword, $flags)
try {
    $publicDer = $full.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
}
finally {
    $full.Dispose()
}

function Add-PublicCertToTrustedRoot {
    param(
        [System.Security.Cryptography.X509Certificates.StoreLocation]$Location
    )
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(,$publicDer)
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
        [System.Security.Cryptography.X509Certificates.StoreName]::Root,
        $Location)
    $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    try {
        $store.Add($cert)
    }
    finally {
        $store.Close()
        $cert.Dispose()
    }
}

Add-PublicCertToTrustedRoot -Location CurrentUser
Write-Host "Installed public certificate to CurrentUser\Root (no private key; avoids Import-PfxCertificate root prompt)."

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    try {
        Add-PublicCertToTrustedRoot -Location LocalMachine
        Write-Host "Installed public certificate to LocalMachine\Root."
    }
    catch {
        Write-Warning ("LocalMachine\Root install failed: {0}" -f $_)
    }
}
else {
    Write-Host "Not elevated; skipped LocalMachine\Root. CurrentUser\Root is enough for this user for HTTPS trust."
}
