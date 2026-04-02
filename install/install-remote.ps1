param(
    [Parameter(Position = 0)]
    [string] $Server = '78.140.243.76',

    [Parameter(Position = 1)]
    [string] $Login = 'Administrator',

    [Parameter(Position = 2)]
    [string] $Password = 'W0HmJkdBFyArO061',

    [string] $CloneUrl = 'https://github.com/kgonsovskii/hephaestus.git',

    [string] $CloneParent = 'C:\Delta'
)

$ErrorActionPreference = 'Stop'
$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
. (Join-Path $here 'install-remote-commons.ps1')

$rdpDir = [System.IO.Path]::GetFullPath((Join-Path $here '..\rdp'))
$exe = Join-Path $rdpDir 'RemoteEnabler.exe'
if (-not (Test-Path -LiteralPath $exe)) {
    throw "RemoteEnabler not found: $exe"
}

& $exe $Server $Login $Password

$cred = [pscredential]::new($Login, (ConvertTo-SecureString $Password -AsPlainText -Force))

try {
    $trusted = Get-Item WSMan:\localhost\Client\TrustedHosts -ErrorAction Stop
    $cur = $trusted.Value
    if ($cur -notmatch [regex]::Escape($Server)) {
        $newVal = if ([string]::IsNullOrWhiteSpace($cur)) { $Server } else { "$cur,$Server" }
        Set-Item WSMan:\localhost\Client\TrustedHosts -Value $newVal -Force
    }
} catch {
}

Invoke-RemotePreInstallReboot -ComputerName $Server -Credential $cred

$srcLocal = Join-Path $here 'install-local.ps1'
$srcLog = Join-Path $here 'install-local-log.ps1'
foreach ($p in @($srcLocal, $srcLog)) {
    if (-not (Test-Path -LiteralPath $p)) {
        throw "Missing $p"
    }
}

Write-Host "=== WinRM: copy 2 files -> $CloneParent , then run install-local.ps1 ===" -ForegroundColor Cyan
$session = New-RemotePwshSession -ComputerName $Server -Credential $cred
$bodyLocal = Get-Content -LiteralPath $srcLocal -Raw -ErrorAction Stop
$bodyLog = Get-Content -LiteralPath $srcLog -Raw -ErrorAction Stop
$remoteLocal = [System.IO.Path]::GetFullPath((Join-Path $CloneParent 'install-local.ps1'))
try {
    try {
        Invoke-Command -Session $session -ScriptBlock {
            param($Parent, $LocalText, $LogText)
            if (Test-Path -LiteralPath $Parent) {
                Remove-Item -LiteralPath $Parent -Recurse -Force
            }
            New-Item -ItemType Directory -Force -Path $Parent | Out-Null
            Set-Content -LiteralPath (Join-Path $Parent 'install-local.ps1') -Value $LocalText -Encoding utf8
            Set-Content -LiteralPath (Join-Path $Parent 'install-local-log.ps1') -Value $LogText -Encoding utf8
        } -ArgumentList $CloneParent, $bodyLocal, $bodyLog -ErrorAction Stop

        Invoke-Command -Session $session -ScriptBlock {
            param($ScriptPath, $cu, $cp)
            & $ScriptPath -CloneUrl $cu -CloneParent $cp
        } -ArgumentList $remoteLocal, $CloneUrl, $CloneParent -ErrorAction Stop
    } catch {
        Write-Host "install-local failed: $($_.Exception.Message)" -ForegroundColor Red
        throw
    }

    Write-Host '=== WinRM: Winlogon auto-login (before reboot) ===' -ForegroundColor Cyan
    Invoke-Command -Session $session -ScriptBlock {
        param($Username, $Pass)
        $regPath = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
        Set-ItemProperty -Path $regPath -Name 'AutoAdminLogon' -Value '1'
        Set-ItemProperty -Path $regPath -Name 'DefaultUserName' -Value $Username
        Set-ItemProperty -Path $regPath -Name 'DefaultPassword' -Value $Pass
        Set-ItemProperty -Path $regPath -Name 'DefaultDomainName' -Value $env:COMPUTERNAME
        Write-Host "Auto-login configured for user '$Username'."
    } -ArgumentList $Login, $Password

    Write-Host '=== WinRM: Restart-Computer -Force (remote) ===' -ForegroundColor Cyan
    try {
        Invoke-Command -Session $session -ScriptBlock { Restart-Computer -Force }
    } catch {
        Write-Host "remote reboot sent (session drop is normal): $($_.Exception.Message)" -ForegroundColor Yellow
    }
} finally {
    Remove-PSSession -Session $session -ErrorAction SilentlyContinue
}

Write-Host '=== sleep 10s after remote reboot (before install-remote2) ===' -ForegroundColor Cyan
Start-Sleep -Seconds 10

& (Join-Path $here 'install-remote2.ps1') -Server $Server -Login $Login -Password $Password -CloneParent $CloneParent

Write-Host '=== install-remote finished ===' -ForegroundColor Green
