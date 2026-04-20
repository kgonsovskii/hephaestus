###head
###head

. ./utils.ps1

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
    if ($globalDebug)
    {
        Start-Process powershell.exe -WindowStyle Normal -WorkingDirectory $taskDir -ArgumentList "-ExecutionPolicy Bypass -file ""$scriptPath"" -Task $taskName"
    }
    else
    {
        Start-Process powershell.exe -WindowStyle Hidden -Verb RunAs -WorkingDirectory $taskDir -ArgumentList "-ExecutionPolicy Bypass -File `"$scriptPath`" -Task $taskName"
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
    $showPath = GetLocalScriptPath -taskName "program"
    writedbg "program curScript: $showPath"

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