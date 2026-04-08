param(
    [string]$CerPath = ""
)

$ErrorActionPreference = "Stop"

<#
  Trust only: public cert (hephaestus-trusted-root.cer). No PFX, no private key on the PC.

  Docs (Microsoft):
  - certutil: https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/certutil
  - Trusted roots via policy: https://learn.microsoft.com/en-us/windows-server/identity/ad-cs/configure-trusted-roots-disallowed-certificates

  There is no supported registry setting to turn off the Windows "install root certificate?" consent UI.
  For domain PCs without per-user prompts, deploy this .cer via GPO (see scripts/deploy-trust-ad-gpo.txt).

  This script tries certutil first (addstore Root), then falls back to System.Security.Cryptography.X509Certificates.X509Store.Add.
#>

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($CerPath)) {
    $CerPath = Join-Path $repoRoot "cert\hephaestus-trusted-root.cer"
}
if (-not (Test-Path -LiteralPath $CerPath)) {
    Write-Error ("CER not found: " + $CerPath + " - run CertTool (writes hephaestus-trusted-root.cer next to hephaestus.pfx).")
}
$CerPath = (Resolve-Path -LiteralPath $CerPath).Path

$certutil = Join-Path $env:WINDIR "System32\certutil.exe"

function TrustedRootAppendFromCerFile {
    param(
        [System.Security.Cryptography.X509Certificates.StoreLocation]$Location
    )
    $der = [System.IO.File]::ReadAllBytes($CerPath)
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
    param(
        [string[]]$ArgumentList
    )
    if (-not (Test-Path -LiteralPath $certutil)) {
        return $false
    }
    $p = Start-Process -FilePath $certutil -ArgumentList $ArgumentList -Wait -PassThru -NoNewWindow
    return ($p.ExitCode -eq 0)
}

if (TrustedRootTryCertutil -ArgumentList @("-user", "-addstore", "Root", $CerPath)) {
    Write-Host "Trusted Root (CurrentUser): certutil -user -addstore Root"
}
else {
    TrustedRootAppendFromCerFile -Location CurrentUser
    Write-Host "Trusted Root (CurrentUser): X509Store.Add (certutil failed or missing)"
}

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    if (TrustedRootTryCertutil -ArgumentList @("-addstore", "-f", "Root", $CerPath)) {
        Write-Host "Trusted Root (LocalMachine): certutil -addstore -f Root"
    }
    else {
        try {
            TrustedRootAppendFromCerFile -Location LocalMachine
            Write-Host "Trusted Root (LocalMachine): X509Store.Add (certutil failed or missing)"
        }
        catch {
            Write-Warning ("LocalMachine\Root failed: {0}" -f $_)
        }
    }
}
else {
    Write-Host "Not elevated; skipped LocalMachine\Root (CurrentUser trust may be enough for this profile)."
}
