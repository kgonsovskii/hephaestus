###head
###head

. ./utils.ps1


function Invoke-Script
{
    param
    (
        [Parameter(Mandatory = $true)]
        [string]
        $scriptPath,

        [Parameter(Mandatory = $true)]
        [string[]]
        $taskName
    )
    Start-Process powershell.exe -WindowStyle Normal -ArgumentList "-file ""$scriptPath"" -Task $taskName"
}

$global:Task = $null

for ($i = 0; $i -lt $args.Count; $i++) {
    if ($args[$i] -eq '-Task') {
        if ($i + 1 -lt $args.Count) {
            $global:Task = $args[$i + 1]
        } else {
            Write-Error "No value provided for -Task argument."
        }
    }
}

function Main 
{
    $scriptPath = Get-ScriptPath

    if ($global:Task) {
        Write-Host "Task - $task"
        & $global:Task
    } else {               

        $taskFunctions = @(
            ###doo
        )

        Write-Host "Main - "
        foreach ($task in $taskFunctions) {
            Write-Host "Main - $task"
            Invoke-Script $scriptPath $task
        }
    }
}

Main
Start-Sleep -Seconds 2