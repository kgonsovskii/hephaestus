<#
.SYNOPSIS
  SSH to a Linux host: install git, clone Hephaestus to the login user's $HOME/hephaestus, run install/install.sh.

.DESCRIPTION
  No local files are copied. Resolves sshpass via Chocolatey (optional) or portable GitHub build.
  If PowerShell buffers remote SSH output, use install\install-remote.bat (C# console via dotnet run).

.PARAMETER Server
  SSH host (default when any switch is used: 216.203.21.239). Omit all three to read install-remote-creds.txt.

.PARAMETER Login
  SSH user (default when any switch is used: root).

.PARAMETER Password
  SSH password (default when any switch is used: legacy script default). Omit all three to read install-remote-creds.txt.

.EXAMPLE
  powershell -File .\install\install-remote.ps1
  (requires install\install-remote-creds.txt: three lines — host, login, password)
.EXAMPLE
  powershell -File .\install\install-remote.ps1 -Server 10.0.0.5 -Login deploy -Password 'secret'
#>
[CmdletBinding()]
param(
    [string] $Server,
    [string] $Login,
    [string] $Password
)

$credsPath = Join-Path $PSScriptRoot "install-remote-creds.txt"
if ($PSBoundParameters.Count -eq 0) {
    if (-not (Test-Path -LiteralPath $credsPath)) {
        throw "No -Server/-Login/-Password and missing file: $credsPath (three lines: host, login, password)."
    }
    $triple = Read-InstallRemoteCredsFile -Path $credsPath
    $Server = $triple.Server
    $Login = $triple.Login
    $Password = $triple.Password
}
else {
    if (-not $PSBoundParameters.ContainsKey("Server")) { $Server = "216.203.21.239" }
    if (-not $PSBoundParameters.ContainsKey("Login")) { $Login = "root" }
    if (-not $PSBoundParameters.ContainsKey("Password")) { $Password = "1!Ogviobhuetly" }
}

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-InstallRemoteCredsFile {
    param([Parameter(Mandatory = $true)][string] $Path)
    $lines = Get-Content -LiteralPath $Path -Encoding utf8
    $taken = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $lines) {
        $t = $line.Trim()
        if ($t.Length -eq 0) { continue }
        if ($t.StartsWith("#")) { continue }
        [void]$taken.Add($t)
        if ($taken.Count -ge 3) { break }
    }
    if ($taken.Count -lt 3) {
        throw "${Path}: need three non-empty, non-comment lines (SSH host, login, password); got $($taken.Count)."
    }
    return [pscustomobject]@{ Server = $taken[0]; Login = $taken[1]; Password = $taken[2] }
}

$sshCommonOpts = @(
    "-o", "StrictHostKeyChecking=accept-new",
    "-o", "ConnectTimeout=30",
    "-o", "ServerAliveInterval=15",
    "-o", "ServerAliveCountMax=4"
)

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

function Invoke-SshPassWithConsoleOutput {
    param(
        [Parameter(Mandatory = $true)][string] $SshPassPath,
        [Parameter(Mandatory = $true)][string[]] $Arguments
    )
    # PowerShell often pipes native exe stdout → OpenSSH block-buffers. Do not redirect: inherit console.
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $SshPassPath
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $false
    $psi.RedirectStandardError = $false
    $psi.RedirectStandardInput = $false
    $psi.CreateNoWindow = $false
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        foreach ($a in $Arguments) { [void]$psi.ArgumentList.Add($a) }
    }
    else {
        # Windows PowerShell 5 / .NET Framework: no ArgumentList — best-effort quoting for ssh
        $psi.Arguments = ($Arguments | ForEach-Object {
                $x = $_
                if ($x -match '[\s"]') { '"' + ($x.Replace('"', '\"')) + '"' } else { $x }
            }) -join ' '
    }
    $p = [System.Diagnostics.Process]::new()
    $p.StartInfo = $psi
    [void]$p.Start()
    $p.WaitForExit()
    return $p.ExitCode
}

$sshpassExe = Ensure-SshPass
$env:SSHPASS = $Password

Write-Host "Remote install -> ${Login}@${Server}"
Write-Host "[1/1] SSH: install git, clone repo to `$HOME/hephaestus (remote user), run install.sh"

$remoteTxt = Join-Path $PSScriptRoot "install-remote.txt"
if (-not (Test-Path -LiteralPath $remoteTxt)) {
    throw "Missing remote script file: $remoteTxt (keep in sync with InstallRemote and install-remote.sh)"
}
$remoteCmd = [System.IO.File]::ReadAllText($remoteTxt, [System.Text.UTF8Encoding]::new($false))
$remoteCmd = $remoteCmd -replace "`r`n", "`n" -replace "`r", "`n"

# One remote argv (base64 pipe) avoids stdin issues; -tt allocates a PTY so apt/git use line-oriented output.
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($remoteCmd))
$remoteShell = "echo $b64 | base64 -d | bash"
$sshArgs = @("-e", "ssh", "-tt") + $sshCommonOpts + @("${Login}@${Server}", $remoteShell)
$exitCode = Invoke-SshPassWithConsoleOutput -SshPassPath $sshpassExe -Arguments $sshArgs
if ($exitCode -ne 0) {
    throw "Remote install failed with exit $exitCode"
}
Write-Host "Done."
