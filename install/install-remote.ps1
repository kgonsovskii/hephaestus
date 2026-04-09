<#
.SYNOPSIS
  Uploads the install/ folder over SSH and runs install.sh on the remote host.

.DESCRIPTION
  If sshpass is not on PATH, installs the Windows port via Chocolatey (package sshpass-win64),
  then continues. Requires Chocolatey (choco) and Administrator rights for that one-time install.

.PARAMETER Server
  SSH host (default: 216.203.21.239).

.PARAMETER Login
  SSH user (default: root).

.PARAMETER Password
  SSH password (default: set in script).

.EXAMPLE
  .\install\install-remote.ps1
.EXAMPLE
  .\install\install-remote.ps1 -Server 10.0.0.5 -Login deploy -Password 'secret'
#>
[CmdletBinding()]
param(
    [string] $Server = "216.203.21.239",
    [string] $Login = "root",
    [string] $Password = "1!Ogviobhuetly"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path }

$remoteDir = "/tmp/hephaestus-install"
$remoteBundle = "/tmp/hephaestus-install-bundle.tgz"

function Update-PathFromEnvironment {
    $machine = [Environment]::GetEnvironmentVariable("Path", "Machine")
    $user = [Environment]::GetEnvironmentVariable("Path", "User")
    $env:Path = "$machine;$user"
    $chocoBin = Join-Path $env:ProgramData "chocolatey\bin"
    if (Test-Path $chocoBin) { $env:Path = "$chocoBin;$env:Path" }
}

function Get-SshPassExecutable {
    foreach ($name in @("sshpass.exe", "sshpass")) {
        $cmd = Get-Command $name -ErrorAction SilentlyContinue
        if ($cmd) { return $cmd.Source }
    }
    $libRoot = if ($env:ChocolateyInstall) { Join-Path $env:ChocolateyInstall "lib" } else { Join-Path $env:ProgramData "chocolatey\lib" }
    $found = Get-ChildItem -Path $libRoot -Filter "sshpass.exe" -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($found) { return $found.FullName }
    return $null
}

function Ensure-SshPass {
    $exe = Get-SshPassExecutable
    if ($exe) {
        Write-Verbose "Using sshpass: $exe"
        return $exe
    }

    $choco = Get-Command choco.exe -ErrorAction SilentlyContinue
    if (-not $choco) {
        throw "sshpass not found and Chocolatey (choco) is not on PATH. Install Chocolatey: https://chocolatey.org/install — or install sshpass manually and re-run."
    }

    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "sshpass is missing. Installing it via Chocolatey requires Administrator. Right-click PowerShell -> Run as administrator, then re-run this script."
    }

    Write-Host "Installing sshpass (sshpass-win64) via Chocolatey..."
    $chocoArgs = @("install", "sshpass-win64", "-y", "--no-progress", "--limit-output")
    & choco.exe @chocoArgs
    if ($LASTEXITCODE -ne 0) {
        throw "choco install sshpass-win64 failed (exit $LASTEXITCODE)."
    }

    Update-PathFromEnvironment

    $exe = Get-SshPassExecutable
    if (-not $exe) {
        throw "sshpass.exe still not found after Chocolatey install. Restart the shell or add Chocolatey bin to PATH."
    }
    Write-Host "sshpass ready: $exe"
    return $exe
}

if (-not (Get-Command tar.exe -ErrorAction SilentlyContinue)) {
    Write-Error "tar.exe is required (Windows 10+ includes it)."
}

$sshpassExe = Ensure-SshPass

$env:SSHPASS = $Password

Write-Host "Remote install -> ${Login}@${Server} ($remoteDir)"

$bundle = Join-Path ([System.IO.Path]::GetTempPath()) ("hephaestus-install-" + [Guid]::NewGuid().ToString("n") + ".tar.gz")
try {
    & tar.exe czf $bundle -C $scriptDir .
    if ($LASTEXITCODE -ne 0) { throw "tar failed with exit $LASTEXITCODE" }

    & $sshpassExe -e scp -o StrictHostKeyChecking=accept-new $bundle "${Login}@${Server}:${remoteBundle}"
    if ($LASTEXITCODE -ne 0) { throw "scp failed with exit $LASTEXITCODE" }

    $unpack = "rm -rf $remoteDir && mkdir -p $remoteDir && tar xzf $remoteBundle -C $remoteDir && rm -f $remoteBundle"
    & $sshpassExe -e ssh -o StrictHostKeyChecking=accept-new "${Login}@${Server}" $unpack
    if ($LASTEXITCODE -ne 0) { throw "remote unpack failed with exit $LASTEXITCODE" }

    & $sshpassExe -e ssh -o StrictHostKeyChecking=accept-new "${Login}@${Server}" "bash $remoteDir/install.sh"
    if ($LASTEXITCODE -ne 0) { throw "remote install.sh failed with exit $LASTEXITCODE" }
}
finally {
    Remove-Item -LiteralPath $bundle -Force -ErrorAction SilentlyContinue
}
