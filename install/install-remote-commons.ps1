function New-RemotePwshSession {
    param(
        [string] $ComputerName,
        [pscredential] $Credential
    )
    $so = New-PSSessionOption -SkipCACheck -SkipCNCheck -SkipRevocationCheck -OpenTimeout 3000
    $uri = "http://${ComputerName}:5985/wsman"
    New-PSSession -ConnectionUri $uri -Credential $Credential -SessionOption $so
}

function Wait-RemoteWinRmAvailable {
    param(
        [string] $ComputerName,
        [pscredential] $Credential,
        [int] $IntervalSec = 3,
        [int] $TimeoutMin = 45
    )
    $deadline = (Get-Date).AddMinutes($TimeoutMin)
    $attempt = 0
    Write-Host "=== WinRM: wait for $ComputerName (${IntervalSec}s interval, ${TimeoutMin}m max) ===" -ForegroundColor Cyan
    while ((Get-Date) -lt $deadline) {
        $attempt++
        Start-Sleep -Seconds $IntervalSec
        try {
            $so = New-PSSessionOption -SkipCACheck -SkipCNCheck -SkipRevocationCheck -OpenTimeout 3000
            $probe = New-PSSession -ConnectionUri "http://${ComputerName}:5985/wsman" -Credential $Credential -SessionOption $so -ErrorAction Stop
            Remove-PSSession -Session $probe -ErrorAction SilentlyContinue
            Write-Host "WinRM OK (attempt $attempt)" -ForegroundColor Green
            return
        } catch {
            Write-Host "WinRM attempt $attempt : $($_.Exception.Message)"
        }
    }
    throw "WinRM did not become available within $TimeoutMin minutes ($ComputerName)."
}

function Invoke-RemotePreInstallReboot {
    param(
        [string] $ComputerName,
        [pscredential] $Credential
    )
    Write-Host '=== WinRM: reboot remote before installation, then wait until available ===' -ForegroundColor Cyan
    $preSession = New-RemotePwshSession -ComputerName $ComputerName -Credential $Credential
    try {
        try {
            Invoke-Command -Session $preSession -ScriptBlock { Restart-Computer -Force }
        } catch {
            Write-Host "pre-install reboot sent (session drop is normal): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    } finally {
        Remove-PSSession -Session $preSession -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 5
    Wait-RemoteWinRmAvailable -ComputerName $ComputerName -Credential $Credential
    Start-Sleep -Seconds 5
}
