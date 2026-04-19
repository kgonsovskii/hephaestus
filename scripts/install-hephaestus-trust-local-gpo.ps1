<#
.SYNOPSIS
  Silently installs the Hephaestus public root .cer into the Local Computer Group Policy
  "Trusted Root Certification Authorities" store (policy-backed), then refreshes policy.

.DESCRIPTION
  Uses certutil -GroupPolicy -addstore Root, which targets the same store as:
  gpedit.msc -> Computer Configuration -> Windows Settings -> Security Settings ->
  Public Key Policies -> Trusted Root Certification Authorities.

  Requires Administrator. Intended for automation (no prompts when run elevated).

  Contrast: install-hephaestus-trust-cer.ps1 adds to the plain LocalMachine\Root store;
  this script uses the Group Policy certificate store under HKLM\SOFTWARE\Policies\...

.PARAMETER CerPath
  Path to hephaestus-trusted-root.cer (default: repo cert\hephaestus-trusted-root.cer).

.PARAMETER SkipGpUpdate
  Do not run gpupdate /force after importing (not recommended).
#>
[CmdletBinding()]
param(
    [string]$CerPath = "",
    [switch]$SkipGpUpdate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-ProcessQuiet {
    param(
        [string]$FilePath,
        [string[]]$ArgumentList
    )
    $stdout = [System.IO.Path]::GetTempFileName()
    $stderr = [System.IO.Path]::GetTempFileName()
    try {
        $p = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -Wait -PassThru -NoNewWindow `
            -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        if ($p.ExitCode -ne 0) {
            $e = Get-Content -LiteralPath $stderr -Raw -ErrorAction SilentlyContinue
            $o = Get-Content -LiteralPath $stdout -Raw -ErrorAction SilentlyContinue
            throw ("{0} failed (exit {1}). stderr: {2} stdout: {3}" -f $FilePath, $p.ExitCode, $e, $o)
        }
    }
    finally {
        Remove-Item -LiteralPath $stdout, $stderr -Force -ErrorAction SilentlyContinue
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($CerPath)) {
    $CerPath = Join-Path $repoRoot "cert\hephaestus-trusted-root.cer"
}
if (-not (Test-Path -LiteralPath $CerPath)) {
    throw ("CER not found: " + $CerPath + " - run CertTool first.")
}
$CerPath = (Resolve-Path -LiteralPath $CerPath).Path

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())


$certutil = Join-Path $env:WINDIR "System32\certutil.exe"
if (-not (Test-Path -LiteralPath $certutil)) {
    throw "certutil.exe not found."
}

# -f overwrite if present; -GroupPolicy = policy certificate store (same subtree as gpedit Trusted Root).
Invoke-ProcessQuiet -FilePath $certutil -ArgumentList @("-f", "-GroupPolicy", "-addstore", "Root", $CerPath)

if (-not $SkipGpUpdate) {
    $gp = Join-Path $env:WINDIR "System32\gpupdate.exe"
    if (-not (Test-Path -LiteralPath $gp)) {
        throw "gpupdate.exe not found: $gp"
    }
    Invoke-ProcessQuiet -FilePath $gp -ArgumentList @("/force")
}
