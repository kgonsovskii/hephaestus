param(
    [Parameter(Position = 0)]
    [string] $Server = '78.140.243.76',

    [Parameter(Position = 1)]
    [string] $Login = 'Administrator',

    [Parameter(Position = 2)]
    [string] $Password = 'W0HmJkdBFyArO061'
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

Write-Host '=== sleep 10s after host is back ===' -ForegroundColor Cyan
Start-Sleep -Seconds 5

Write-Host '=== install-remote2 finished ===' -ForegroundColor Green
