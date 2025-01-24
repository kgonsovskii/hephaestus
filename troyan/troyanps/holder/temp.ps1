param(
    [Parameter(Mandatory=$False, Position=0, ValueFromPipeline=$true)]
    [System.String]
    $Task
)

function Get-ScriptPath {
    $scriptPaths = @(
        #$MyInvocation.MyCommand.Definition,
        $PSCommandPath,
        $MyInvocation.MyCommand.Path
    )
    
    foreach ($path in $scriptPaths) {
        try {
            if (Test-Path $path) {
                return $path
            }
        }
        catch {
            <#Do this if a terminating exception happens#>
        }
    }
}

function Helper-Function {
    param ([string]$TaskName)
    Write-Host "$TaskName : Helper-Function executed." -ForegroundColor Magenta
}

function Task-One {
    Helper-Function -TaskName "Task-One"
    Write-Host "Task-One started." -ForegroundColor Green
    Start-Sleep -Seconds 3
    Write-Host "Task-One completed." -ForegroundColor Green
    Start-Sleep -Seconds 5
}

function Task-Two {
    Helper-Function -TaskName "Task-Two"
    Write-Host "Task-Two started." -ForegroundColor Cyan
    Start-Sleep -Seconds 5
    Write-Host "Task-Two completed." -ForegroundColor Cyan
    Start-Sleep -Seconds 5
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
    Start-Process powershell.exe -ArgumentList "-file ""$scriptPath"" -Task $taskName"
}

function Main 
{
    $scriptPath = Get-ScriptPath

    if ($Task) {
        Write-Host "Executing Task $Task..." -ForegroundColor Yellow
        & $Task
    } else {
        Write-Host "Executing both tasks in parallel..." -ForegroundColor Yellow
                
        $taskFunctions = Get-Command -Type Function | Where-Object { $_.Name -like 'Task-*' }

        foreach ($task in $taskFunctions) {
            Write-Host "Starting task: $($task.Name)" -ForegroundColor Green
            Invoke-Script $scriptPath $task.Name
        }

        Write-Host "Tasks are executing in parallel. Main function returning immediately." -ForegroundColor Green
    }
}

Start-Sleep -Seconds 1
Main