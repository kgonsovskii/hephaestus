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

$cred = [pscredential]::new($Login, (ConvertTo-SecureString $Password -AsPlainText -Force))

Write-Host '=== WinRM: wait for host after reboot (3s interval, 3s connect timeout) ===' -ForegroundColor Cyan
$deadline = (Get-Date).AddMinutes(45)
$attempt = 0
while ((Get-Date) -lt $deadline) {
    $attempt++
    Start-Sleep -Seconds 3
    try {
        $so = New-PSSessionOption -SkipCACheck -SkipCNCheck -SkipRevocationCheck -OpenTimeout 3000
        $probe = New-PSSession -ConnectionUri "http://${Server}:5985/wsman" -Credential $cred -SessionOption $so -ErrorAction Stop
        Remove-PSSession -Session $probe -ErrorAction SilentlyContinue
        Write-Host "WinRM OK (attempt $attempt)" -ForegroundColor Green
        break
    } catch {
        Write-Host "WinRM attempt $attempt : $($_.Exception.Message)"
    }
}
if ((Get-Date) -ge $deadline) {
    throw 'WinRM did not become available within the deadline.'
}

Write-Host '=== sleep 5s after host is back ===' -ForegroundColor Cyan
Start-Sleep -Seconds 5

$remoteLogPath = [System.IO.Path]::GetFullPath((Join-Path $CloneParent 'log.txt'))
Write-Host "=== WinRM: poll remote log until _INSTALL_COMPLETE_ (2h max, read every 5s) ===" -ForegroundColor Cyan
Write-Host "Remote log: $remoteLogPath" -ForegroundColor DarkGray

$soPoll = New-PSSessionOption -SkipCACheck -SkipCNCheck -SkipRevocationCheck -OpenTimeout 3000
$pollSession = New-PSSession -ConnectionUri "http://${Server}:5985/wsman" -Credential $cred -SessionOption $soPoll
try {
    $pollDeadline = (Get-Date).AddHours(2)
    $lastLineCount = 0
    $sawComplete = $false
    while ((Get-Date) -lt $pollDeadline) {
        $lines = Invoke-Command -Session $pollSession -ScriptBlock {
            param($Path)
            if (-not (Test-Path -LiteralPath $Path)) {
                return
            }
            Get-Content -LiteralPath $Path -ErrorAction Stop
        } -ArgumentList $remoteLogPath

        if ($null -eq $lines) {
            $lines = @()
        } else {
            $lines = @($lines)
        }

        if ($lines.Count -gt $lastLineCount) {
            for ($i = $lastLineCount; $i -lt $lines.Count; $i++) {
                $lineNo = $i + 1
                Write-Host ('{0,6}| {1}' -f $lineNo, $lines[$i])
            }
            $lastLineCount = $lines.Count
        }

        $joined = [string]::Join([Environment]::NewLine, $lines)
        if ($joined -match '_INSTALL_COMPLETE_') {
            $sawComplete = $true
            break
        }

        Start-Sleep -Seconds 5
    }
    if (-not $sawComplete) {
        throw "Remote log did not contain _INSTALL_COMPLETE_ within 2 hours ($remoteLogPath)."
    }
} finally {
    Remove-PSSession -Session $pollSession -ErrorAction SilentlyContinue
}

Write-Host '=== install-remote2 finished ===' -ForegroundColor Green
