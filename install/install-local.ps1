param(
    [string] $CloneUrl = 'https://github.com/kgonsovskii/hephaestus.git',

    [string] $CloneParent = 'C:\Delta'
)
$ErrorActionPreference = 'Stop'
$ConfirmPreference = 'None'
$ProgressPreference = 'SilentlyContinue'

# First: remove any previous repo folder under CloneParent (hephaestus / Hephaestus)
foreach ($leaf in @('hephaestus', 'Hephaestus')) {
    $p = Join-Path $CloneParent $leaf
    if (Test-Path -LiteralPath $p) {
        Remove-Item -LiteralPath $p -Recurse -Force
    }
}
$dest = Join-Path $CloneParent 'hephaestus'
New-Item -ItemType Directory -Force -Path $CloneParent | Out-Null

[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072

function Refresh-PathEnv {
    $m = [System.Environment]::GetEnvironmentVariable('Path', 'Machine')
    $u = [System.Environment]::GetEnvironmentVariable('Path', 'User')
    $env:Path = "$m;$u"
}

function Invoke-External {
    param(
        [Parameter(Mandatory)]
        [string] $FilePath,
        [string[]] $Arguments = @()
    )
    $stdout = Join-Path ([System.IO.Path]::GetTempPath()) ('stdout-{0}.txt' -f [Guid]::NewGuid().ToString('N'))
    $stderr = Join-Path ([System.IO.Path]::GetTempPath()) ('stderr-{0}.txt' -f [Guid]::NewGuid().ToString('N'))
    try {
        $p = Start-Process -FilePath $FilePath -ArgumentList $Arguments -Wait -PassThru -NoNewWindow `
            -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        if (Test-Path -LiteralPath $stdout) {
            Get-Content -LiteralPath $stdout -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_ }
        }
        if (Test-Path -LiteralPath $stderr) {
            Get-Content -LiteralPath $stderr -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_ }
        }
        return [int]$p.ExitCode
    } finally {
        Remove-Item -LiteralPath $stdout, $stderr -Force -ErrorAction SilentlyContinue
    }
}

function Ensure-MicrosoftPowerShellArchive {
    try {
        Import-Module Microsoft.PowerShell.Archive -Force -ErrorAction Stop
        $null = Get-Command Expand-Archive -ErrorAction Stop
        return
    } catch {
    }

    Write-Output '=== Microsoft.PowerShell.Archive (nupkg, no PackageManagement) ==='
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $ver = '1.2.5'
    $nupkgUrl = "https://www.powershellgallery.com/api/v2/package/Microsoft.PowerShell.Archive/$ver"
    $tmpRoot = Join-Path $env:TEMP ('psarchive_' + [Guid]::NewGuid().ToString('N'))
    $nupkgPath = Join-Path $env:TEMP "Microsoft.PowerShell.Archive.$ver.nupkg"
    New-Item -ItemType Directory -Force -Path $tmpRoot | Out-Null
    try {
        (New-Object System.Net.WebClient).DownloadFile($nupkgUrl, $nupkgPath)
        [System.IO.Compression.ZipFile]::ExtractToDirectory($nupkgPath, $tmpRoot)
        $psd1 = Get-ChildItem -Path $tmpRoot -Recurse -Filter 'Microsoft.PowerShell.Archive.psd1' -ErrorAction Stop |
            Select-Object -First 1
        if ($null -eq $psd1) {
            throw 'Microsoft.PowerShell.Archive.psd1 not found in nupkg.'
        }
        $modRoot = $psd1.Directory.FullName
        $modulesBase = Join-Path $env:ProgramFiles 'WindowsPowerShell\Modules\Microsoft.PowerShell.Archive'
        $dest = Join-Path $modulesBase $ver
        if (Test-Path -LiteralPath $dest) {
            Remove-Item -LiteralPath $dest -Recurse -Force
        }
        New-Item -ItemType Directory -Force -Path $modulesBase | Out-Null
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Get-ChildItem -LiteralPath $modRoot | Copy-Item -Destination $dest -Recurse -Force
        Import-Module Microsoft.PowerShell.Archive -Force
        $null = Get-Command Expand-Archive -ErrorAction Stop
    } finally {
        Remove-Item -LiteralPath $tmpRoot -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $nupkgPath -Force -ErrorAction SilentlyContinue
    }
}

Ensure-MicrosoftPowerShellArchive

if (-not (Get-Command choco -ErrorAction SilentlyContinue)) {
    Write-Output '=== Chocolatey ==='
    Set-ExecutionPolicy Bypass -Scope Process -Force
    iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
} else {
    Write-Output 'Chocolatey already installed.'
}

Refresh-PathEnv

$env:GIT_TERMINAL_PROMPT = '0'
if ($CloneUrl -like 'git@*' -or $CloneUrl -like '*ssh://*') {
    $env:GIT_SSH_COMMAND = 'ssh -o BatchMode=yes -o StrictHostKeyChecking=accept-new'
}

Write-Output '=== choco install git ==='
$chocoExe = (Get-Command choco -ErrorAction Stop).Source
$code = Invoke-External -FilePath $chocoExe -Arguments @('install', 'git', '-y', '--no-progress', '--ignore-checksums')
Write-Output "choco exit code: $code"

Refresh-PathEnv

$gitExe = (Get-Command git -ErrorAction Stop).Source
Write-Output '=== git ==='
if (Test-Path -LiteralPath (Join-Path $dest '.git')) {
    Write-Output "git pull $dest"
    $g = Invoke-External -FilePath $gitExe -Arguments @('-C', $dest, 'pull')
} else {
    Write-Output "git clone $CloneUrl -> $dest"
    $g = Invoke-External -FilePath $gitExe -Arguments @('clone', $CloneUrl, $dest)
}
if ($g -ne 0) {
    throw "git exited with code $g"
}

$installPs1 = Join-Path $dest 'install\install.ps1'
if (-not (Test-Path -LiteralPath $installPs1)) {
    throw "Missing $installPs1"
}

$bootstrapLog = Join-Path $CloneParent 'install-local-log.ps1'
if (Test-Path -LiteralPath $bootstrapLog) {
    Copy-Item -LiteralPath $bootstrapLog -Destination (Join-Path $dest 'install\install-local-log.ps1') -Force
}

$logPath = Join-Path $CloneParent 'log.txt'
$logRunner = [System.IO.Path]::GetFullPath((Join-Path $dest 'install\install-local-log.ps1'))
$runEntryName = '_HephaestusBootInstall'

$runKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
$psExe = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$cmd = "`"$psExe`" -NoProfile -ExecutionPolicy Bypass -File `"$logRunner`""
Set-ItemProperty -LiteralPath $runKey -Name $runEntryName -Value $cmd

Write-Output "=== HKLM Run '$runEntryName' (next logon) -> $logRunner ; log -> $logPath ==="
Write-Output '=== install-local finished (reboot from install-remote) ==='
