param(
    [string]$PfxPassword = "123"
)

$ErrorActionPreference = "Stop"

<#
  1) Trust: uses cert/hephaestus-trusted-root.cer via certutil when possible (same as install-hephaestus-trust-cer.ps1).
  2) Personal (My): loads cert/hephaestus.pfx with .NET (UserKeySet|PersistKeySet / MachineKeySet|PersistKeySet).

  Root trust-only without PFX: you can run only install-hephaestus-trust-cer.ps1.

  Microsoft does not document a way to disable root-trust consent dialogs via registry.
  Domain: use GPO + .cer — scripts/deploy-trust-ad-gpo.txt
#>

$repoRoot = Split-Path -Parent $PSScriptRoot
$pfxPath = Join-Path $repoRoot "cert\hephaestus.pfx"
$cerPath = Join-Path $repoRoot "cert\hephaestus-trusted-root.cer"

if (-not (Test-Path -LiteralPath $pfxPath)) {
    Write-Error ("PFX not found: " + $pfxPath + " - run CertTool first.")
}
if (-not (Test-Path -LiteralPath $cerPath)) {
    Write-Error ("CER not found: " + $cerPath + " - run CertTool first (creates hephaestus-trusted-root.cer).")
}
$pfxPath = (Resolve-Path -LiteralPath $pfxPath).Path
$cerPath = (Resolve-Path -LiteralPath $cerPath).Path

$certutil = Join-Path $env:WINDIR "System32\certutil.exe"

function TrustedRootAppendFromCerFile {
    param(
        [string]$Path,
        [System.Security.Cryptography.X509Certificates.StoreLocation]$Location
    )
    $der = [System.IO.File]::ReadAllBytes($Path)
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(,$der)
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

function TrustedRootTryCertutil {
    param([string[]]$ArgumentList)
    if (-not (Test-Path -LiteralPath $certutil)) {
        return $false
    }
    $p = Start-Process -FilePath $certutil -ArgumentList $ArgumentList -Wait -PassThru -NoNewWindow
    return ($p.ExitCode -eq 0)
}

if (TrustedRootTryCertutil -ArgumentList @("-user", "-addstore", "Root", $cerPath)) {
    Write-Host "Trusted Root (CurrentUser): certutil"
}
else {
    TrustedRootAppendFromCerFile -Path $cerPath -Location CurrentUser
    Write-Host "Trusted Root (CurrentUser): X509Store.Add (fallback)"
}

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    if (TrustedRootTryCertutil -ArgumentList @("-addstore", "-f", "Root", $cerPath)) {
        Write-Host "Trusted Root (LocalMachine): certutil"
    }
    else {
        try {
            TrustedRootAppendFromCerFile -Path $cerPath -Location LocalMachine
            Write-Host "Trusted Root (LocalMachine): X509Store.Add (fallback)"
        }
        catch {
            Write-Warning ("LocalMachine\Root failed: {0}" -f $_)
        }
    }
}
else {
    Write-Host "Not elevated; skipped LocalMachine\Root."
}

# PFX bags can carry CSP flags (e.g. strong private key protection / UserProtected). Import-PfxCertificate
# and X509Certificate2(..., flags) honor them. We never pass UserProtected; Exportable matches -Exportable
# and avoids some import-time prompts. Org policy can still force prompts (see deploy-trust-ad-gpo.txt).
function MyStoreAppendFromPfx {
    param(
        [System.Security.Cryptography.X509Certificates.StoreLocation]$Location,
        [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]$KeyFlags
    )
    $cert = $null
    try {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfxPath, $PfxPassword, $KeyFlags)
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
            [System.Security.Cryptography.X509Certificates.StoreName]::My,
            $Location)
        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        try {
            $store.Add($cert)
        }
        finally {
            $store.Close()
        }
    }
    finally {
        if ($null -ne $cert) { $cert.Dispose() }
    }
}

$userMyFlags = [int](
    [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable -bor
    [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet -bor
    [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet) -band (-bnot [int][System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserProtected)
MyStoreAppendFromPfx -Location CurrentUser -KeyFlags $userMyFlags
Write-Host "PFX to CurrentUser\My."

if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    try {
        $machineMyFlags = [int](
            [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable -bor
            [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::MachineKeySet -bor
            [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet) -band (-bnot [int][System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserProtected)
        MyStoreAppendFromPfx -Location LocalMachine -KeyFlags $machineMyFlags
        Write-Host "PFX to LocalMachine\My."
    }
    catch {
        Write-Warning ("LocalMachine\My failed: {0}" -f $_)
    }
}
else {
    Write-Host "Skipped LocalMachine\My (not elevated)."
}
