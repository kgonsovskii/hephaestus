param(
    [Parameter(Position = 0)]
    [string] $Server = '78.140.243.76',

    [Parameter(Position = 1)]
    [string] $Login = 'Administrator',

    [Parameter(Position = 2)]
    [string] $Password = 'W0HmJkdBFyArO061',

    [string] $CloneParent = 'C:\Delta'
)

$ErrorActionPreference = 'Stop'
$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
. (Join-Path $here 'install-remote-commons.ps1')

$cred = [pscredential]::new($Login, (ConvertTo-SecureString $Password -AsPlainText -Force))

Wait-RemoteWinRmAvailable -ComputerName $Server -Credential $cred

Write-Host '=== sleep 5s after host is back ===' -ForegroundColor Cyan
Start-Sleep -Seconds 5

$remoteLogPath = [System.IO.Path]::GetFullPath((Join-Path $CloneParent 'log.txt'))
Write-Host "=== WinRM: poll remote log until _INSTALL_COMPLETE_ (2h max, read every 5s) ===" -ForegroundColor Cyan
Write-Host "Remote log: $remoteLogPath" -ForegroundColor DarkGray

$pollDeadline = (Get-Date).AddHours(2)
$lastLineCount = 0
$sawComplete = $false
$quietStart = Get-Date
while ((Get-Date) -lt $pollDeadline) {
    $pollSession = $null
    $lines = @()
    try {
        $pollSession = New-RemotePwshSession -ComputerName $Server -Credential $cred
        $r = Invoke-Command -Session $pollSession -ScriptBlock {
            param($Path)
            if (-not (Test-Path -LiteralPath $Path)) {
                return
            }
            Get-Content -LiteralPath $Path -ErrorAction Stop
        } -ArgumentList $remoteLogPath
        if ($null -ne $r) {
            $lines = @($r)
        }

        if ($lines.Count -gt $lastLineCount) {
            for ($i = $lastLineCount; $i -lt $lines.Count; $i++) {
                $lineNo = $i + 1
                Write-Host ('{0,6}| {1}' -f $lineNo, $lines[$i])
            }
            $lastLineCount = $lines.Count
            $quietStart = Get-Date
        } else {
            $quietFor = (Get-Date) - $quietStart
            Write-Host ("no events for {0:n0} s (no new log lines)" -f $quietFor.TotalSeconds) -ForegroundColor DarkGray
        }

        $joined = [string]::Join([Environment]::NewLine, $lines)
        if ($joined -match '_INSTALL_COMPLETE_') {
            $sawComplete = $true
        }
    } catch {
        Write-Host "poll iteration: $($_.Exception.Message)" -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    } finally {
        if ($null -ne $pollSession) {
            Remove-PSSession -Session $pollSession -ErrorAction SilentlyContinue
        }
        if (-not $sawComplete) {
            Start-Sleep -Seconds 5
        }
    }
    if ($sawComplete) {
        break
    }
}
if (-not $sawComplete) {
    throw "Remote log did not contain _INSTALL_COMPLETE_ within 2 hours ($remoteLogPath)."
}

Write-Host '=== install-remote2 finished ===' -ForegroundColor Green
