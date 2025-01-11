param (
    [string]$serverName
)

# Ensure script runs from its directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir

# Load supporting scripts
. ".\lib.ps1"

if ([string]::IsNullOrWhiteSpace($serverName)) {
    $serverName = detectServer
}

. ".\current.ps1" -serverName $serverName
. ".\install-lib.ps1" -serverName $serverName
. ".\install-pre.ps1" -serverName $serverName -reboot "false"

# Read credentials and paths
$password = (Get-Content 'C:\Windows\info.txt' -Raw).Trim()
$user = "rdp"
$serverIp = $server.clone.cloneServerIp
$exePath = $server.clonerExe
$fullPath = Resolve-Path -Path (Join-Path -Path $scriptDir -ChildPath "../Cloner/bin/Debug/net9.0/Cloner.exe") -ErrorAction SilentlyContinue
if ($fullPath -and (Test-Path $fullPath)) {
    $exePath = $fullPath.Path
}

$lofgile=$server.userCloneLog
if (Test-Path $lofgile) {
    Remove-Item $lofgile -Force
}

function Log {
    param (

        [string]$LineText
    )
    Write-Host $LineText
    Add-Content -Path $lofgile -Value $LineText
}

$arguments = "$serverName"
$taskName = "MyTask"

Log "Preparing to install: $exePath on $serverName ($serverIp)"

# ---------------------------------------
# Remove existing task if present
# ---------------------------------------
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    Log "Existing task '$taskName' found. Removing..."
    try {
        Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    } catch {}
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    Log "Task removed."
}

# ---------------------------------------
# Schedule task using schtasks.exe (more reliable for elevation)
# ---------------------------------------

# Schedule time (1 min from now)
$startTime = (Get-Date).AddMinutes(1).ToString("HH:mm")

# Full schtasks command (as a string)
$taskCmd = @"
schtasks.exe /Create /IT /TN "$taskName" /TR "$exePath $arguments" /SC ONCE /ST $startTime /RL HIGHEST /F /RU "$user" /RP "$password"
"@

Log "Creating task using schtasks.exe..."
cmd.exe /c $taskCmd