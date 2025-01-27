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
    Start-Process powershell.exe -WindowStyle Hidden -ArgumentList "-file ""$scriptPath"" -Task $taskName"
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

function Main 
{
    $scriptPath = Get-ScriptPath
    writedbg "program curScript: $scriptPath"

    if ($global:Task) {
        writedbg "Task - $task"
        & $global:Task
    } else {               

        $taskFunctions = @(
            ###doo
        )

        writedbg "Main - "
        foreach ($task in $taskFunctions) 
        {
            writedbg "Main - $task"
            Invoke-Script $scriptPath $task
        }
    }
}

Main