###head
param(
    [Parameter(Mandatory=$False, Position=0, ValueFromPipeline=$true)]
    [System.String]
    $Task
)
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

function Main 
{
    $scriptPath = Get-ScriptPath

    if ($Task) {
        & $Task
    } else {               

        $taskFunctions = @(
            ###doo
        )

        foreach ($task in $taskFunctions) {
            Invoke-Script $scriptPath $task
        }
    }
}

Main