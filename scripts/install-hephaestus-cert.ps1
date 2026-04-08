param(
    [string]$PfxPassword = "123"
)

$ErrorActionPreference = "Stop"

# Personal store: X509Certificate2(path, password, UserKeySet|PersistKeySet or MachineKeySet|PersistKeySet); Import() is obsolete on newer .NET.

$repoRoot = Split-Path -Parent $PSScriptRoot
$pfxPath = Join-Path $repoRoot "cert\hephaestus.pfx"
if (-not (Test-Path -LiteralPath $pfxPath)) {
    Write-Error ("PFX not found: " + $pfxPath + " - run CertTool first (same file DomainHost uses).")
}
$pfxPath = (Resolve-Path -LiteralPath $pfxPath).Path

$pfxBytes = [System.IO.File]::ReadAllBytes($pfxPath)
$ephemeral = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet
$full = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfxBytes, $PfxPassword, $ephemeral)
try {
    $publicDer = $full.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
}
finally {
    $full.Dispose()
}

function TrustedRootAppendPublic {
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

TrustedRootAppendPublic -Location CurrentUser
Write-Host "Installed public certificate to CurrentUser\Root."

$userMyFlags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet -bor [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet
MyStoreAppendFromPfx -Location CurrentUser -KeyFlags $userMyFlags
Write-Host "Installed PFX to CurrentUser\My via .NET (UserKeySet, PersistKeySet)."

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    try {
        TrustedRootAppendPublic -Location LocalMachine
        Write-Host "Installed public certificate to LocalMachine\Root."
    }
    catch {
        Write-Warning ("LocalMachine\Root failed: {0}" -f $_)
    }
    try {
        $machineMyFlags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::MachineKeySet -bor [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet
        MyStoreAppendFromPfx -Location LocalMachine -KeyFlags $machineMyFlags
        Write-Host "Installed PFX to LocalMachine\My via .NET (MachineKeySet, PersistKeySet)."
    }
    catch {
        Write-Warning ("LocalMachine\My failed: {0}" -f $_)
    }
}
else {
    Write-Host "Not elevated; skipped LocalMachine\Root and LocalMachine\My."
}
