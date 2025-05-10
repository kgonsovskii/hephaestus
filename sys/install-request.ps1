param (
    [string]$serverName
)


$TaskName = "MyTask"

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"

if ($serverName -eq "") {
    $serverName = detectServer
} 

. ".\current.ps1" -serverName $serverName
. ".\install-lib.ps1" -serverName $serverName
. ".\install-pre.ps1" -serverName $serverName -reboot "false"


$password =(Get-Content 'C:\Windows\info.txt' -Raw).Trim()
$user="rdp"
$serverIp = $server.clone.cloneServerIp
$exePath = $server.clonerExe

Write-Host "Install-request $serverName, serverIp $serverIp"

$arguments = "-serverName $serverName"
$action = New-ScheduledTaskAction -Execute $exePath -Argument $arguments

$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddSeconds(2)

$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries


$existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue

if ($existingTask) {
    Write-Output "Task '$TaskName' exists. Stopping and deleting..."

    try {
        Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    } catch {
        Write-Warning "Failed to stop task '$TaskName'. It may not be running."
    }

    # Unregister (delete) the task
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Output "Task '$TaskName' deleted."
}

Register-ScheduledTask -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -User $user `
    -Password $password `
    -RunLevel Highest `
    -Force
