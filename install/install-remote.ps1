<#
.SYNOPSIS
  Uploads the install/ folder over SSH and runs install.sh on the remote host.

.DESCRIPTION
  Resolves sshpass: PATH first, then Chocolatey (sshpass-win64) if elevated, then a portable
  download from the upstream GitHub release (no admin, no choco).

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
    $localTools = Join-Path $env:LOCALAPPDATA "hephaestus-tools"
    $found = Get-ChildItem -Path $localTools -Filter "sshpass.exe" -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($found) { return $found.FullName }
    return $null
}

function Install-SshPassPortable {
    param(
        [string] $Version = "1.10.0"
    )
    # https://github.com/sharpninja/sshpass-win64/releases (Win64 native; same as Chocolatey package source)
    $destRoot = Join-Path $env:LOCALAPPDATA "hephaestus-tools\sshpass-win64-$Version"
    $zipUrl = "https://github.com/sharpninja/sshpass-win64/releases/download/v$Version/sshpass-win64-$Version.zip"
    $tmpZip = Join-Path $env:TEMP "hephaestus-sshpass-$Version.zip"

    if (-not (Test-Path $destRoot)) {
        New-Item -ItemType Directory -Path $destRoot -Force | Out-Null
    }

    $existing = Get-ChildItem -Path $destRoot -Filter "sshpass.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($existing) {
        $env:Path = "$($existing.DirectoryName);$env:Path"
        return $existing.FullName
    }

    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Write-Host "Downloading portable sshpass-win64 v$Version from GitHub..."
    try {
        Invoke-WebRequest -Uri $zipUrl -OutFile $tmpZip -UseBasicParsing
        Expand-Archive -Path $tmpZip -DestinationPath $destRoot -Force
    }
    finally {
        Remove-Item -LiteralPath $tmpZip -Force -ErrorAction SilentlyContinue
    }

    $exeFile = Get-ChildItem -Path $destRoot -Filter "sshpass.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $exeFile) {
        throw "sshpass.exe not found after extracting portable zip. URL: $zipUrl"
    }
    $env:Path = "$($exeFile.DirectoryName);$env:Path"
    return $exeFile.FullName
}

function Ensure-SshPass {
    $exe = Get-SshPassExecutable
    if ($exe) {
        Write-Verbose "Using sshpass: $exe"
        return $exe
    }

    $choco = Get-Command choco.exe -ErrorAction SilentlyContinue
    $isAdmin = (New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)

    if ($choco -and $isAdmin) {
        Write-Host "Trying Chocolatey package sshpass-win64 (optional)..."
        $chocoArgs = @("install", "sshpass-win64", "-y", "--no-progress", "--limit-output")
        $chocoLog = & choco.exe @chocoArgs 2>&1 | Out-String
        $chocoExit = $LASTEXITCODE
        # 0 = success; 3010 = success, reboot pending
        if ($chocoExit -eq 0 -or $chocoExit -eq 3010) {
            Update-PathFromEnvironment
            $exe = Get-SshPassExecutable
            if ($exe) {
                Write-Host "sshpass (Chocolatey): $exe"
                return $exe
            }
        }
        Write-Warning "Chocolatey sshpass-win64 did not leave sshpass.exe on PATH (exit $chocoExit). Falling back to portable download."
        if ($chocoLog -and $VerbosePreference -ne 'SilentlyContinue') {
            Write-Verbose $chocoLog
        }
    }

    $exe = Install-SshPassPortable
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

    # Single-quoted so Windows PowerShell 5.1 never treats && as script syntax; remote bash runs this.
    $unpack = 'rm -rf /tmp/hephaestus-install && mkdir -p /tmp/hephaestus-install && tar xzf /tmp/hephaestus-install-bundle.tgz -C /tmp/hephaestus-install && rm -f /tmp/hephaestus-install-bundle.tgz'
    & $sshpassExe -e ssh -o StrictHostKeyChecking=accept-new "${Login}@${Server}" $unpack
    if ($LASTEXITCODE -ne 0) { throw "remote unpack failed with exit $LASTEXITCODE" }

    & $sshpassExe -e ssh -o StrictHostKeyChecking=accept-new "${Login}@${Server}" "bash $remoteDir/install.sh"
    if ($LASTEXITCODE -ne 0) { throw "remote install.sh failed with exit $LASTEXITCODE" }
}
finally {
    Remove-Item -LiteralPath $bundle -Force -ErrorAction SilentlyContinue
}
