$server = '93.183.75.106'
$user = 'Administrator'
$password = '9GP7WLEhLg'



############################################################################
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\install-x.ps1"

function Get-ClonerPath {
    if ($PSCommandPath) {
        $scriptDir = Split-Path -Parent $PSCommandPath
    }
    else {
        $scriptDir = (Get-Location).Path
    }

    $paths = @(
        (Join-Path -Path $scriptDir -ChildPath "..\cloner\bin\debug\net9.0\cloner.exe"),
        (Join-Path -Path $scriptDir -ChildPath "..\cp\cloner.exe")
    )

    foreach ($path in $paths) {
        if (Test-Path $path) {
            # Resolve the full absolute path and return it as a string
            return (Resolve-Path $path).Path
        }
    }

    throw "cloner.exe not found in expected locations."
}

function Invoke-Cloner {
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$ExePath,

        [Parameter(ValueFromRemainingArguments = $true, Position = 1)]
        [string[]]$Args
    )

    if (-not (Test-Path $ExePath)) {
        throw "Executable not found: $ExePath"
    }

    $argString = $Args -join ' '
    $logFile = "_log1.txt"

    # Start the process and redirect both stdout and stderr to tee-object
    & $ExePath @Args 2>&1 | Tee-Object -FilePath "_log1.txt"
}

$executor = Get-ClonerPath

taskkill.exe /im "Cloner.exe" /f
taskkill.exe /im "Refiner.exe" /f
taskkill.exe /im "Packer.exe" /f
taskkill.exe /im "SharpRdp.exe" /f

Write-Output "Executing $executor $server $user $password"
$result = Invoke-Cloner $executor $server $user $password
Start-Sleep -Seconds 1000
Write-Output "Process exited with code $($result.ExitCode)"