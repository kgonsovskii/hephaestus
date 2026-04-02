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

function New-RemotePwshSession {
    param(
        [string] $ComputerName,
        [pscredential] $Credential
    )
    $so = New-PSSessionOption -SkipCACheck -SkipCNCheck -SkipRevocationCheck -OpenTimeout 3000
    $uri = "http://${ComputerName}:5985/wsman"
    New-PSSession -ConnectionUri $uri -Credential $Credential -SessionOption $so
}

$localScript = Join-Path $here 'install-local.ps1'
if (-not (Test-Path -LiteralPath $localScript)) {
    throw "install-local.ps1 not found: $localScript"
}

Write-Host '=== WinRM: copy + run install-local.ps1 ===' -ForegroundColor Cyan
$session = New-RemotePwshSession -ComputerName $Server -Credential $cred
try {
    try {
        Invoke-Command -Session $session -FilePath $localScript -ArgumentList $CloneUrl, $CloneParent -ErrorAction Stop
    } catch {
        Write-Host "install-local failed: $($_.Exception.Message)" -ForegroundColor Red
        throw
    }

    $logScriptSrc = Join-Path $here 'install-local-log.ps1'
    $logScriptRemote = [System.IO.Path]::GetFullPath((Join-Path $CloneParent 'hephaestus\install\install-local-log.ps1'))
    Write-Host '=== WinRM: copy install-local-log.ps1 to remote ===' -ForegroundColor Cyan
    $logBody = Get-Content -LiteralPath $logScriptSrc -Raw -ErrorAction Stop
    Invoke-Command -Session $session -ScriptBlock {
        param($Body, $RemotePath)
        $dir = Split-Path -Parent $RemotePath
        if (-not (Test-Path -LiteralPath $dir)) {
            New-Item -ItemType Directory -Force -Path $dir | Out-Null
        }
        Set-Content -LiteralPath $RemotePath -Value $Body -Encoding Unicode
    } -ArgumentList $logBody, $logScriptRemote

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

& (Join-Path $here 'install-remote2.ps1') -Server $Server -Login $Login -Password $Password

Write-Host '=== install-remote finished ===' -ForegroundColor Green
