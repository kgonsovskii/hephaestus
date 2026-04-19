param (
    [string]$serverName, [string]$packId = ""
)

if ($packId -eq "empty")
{
    $packId = ""
}

Write-Host "Packing server $serverName, pack $packId"

if ($serverName -eq "") {
   throw "No server for pack"
} 

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"
. ".\current.ps1" -serverName $serverName

if (-not (Test-Path $server.pack.packFolder)) {
    New-Item -ItemType Directory -Path $server.pack.packFolder | Out-Null
}


if ([string]::IsNullOrEmpty($packId) -eq $false) {
    $pack = $server.pack.items | Where-Object { $_.id -eq $packId }
    if (-not $pack) {
        throw "Item with id '$packId' not found in pack items."
    }
    if (-not (Test-Path $pack.packFolder)) {
        New-Item -ItemType Directory -Path $pack.packFolder | Out-Null
    }
    $url = $pack.originalUrl
    $id = $pack.id
    Write-Host "Packing individual $id -> $url"
    . ".\compile.ps1" -serverName $serverName -action "exe" -packId $packId
    exit
}   

# RUN
foreach ($pack in $server.pack.items)
{ 
    if (-not (Test-Path $pack.packFolder)) {
        New-Item -ItemType Directory -Path $pack.packFolder | Out-Null
    }
    $url = $pack.originalUrl
    $id = $pack.id
    Write-Host "Packing $id -> $url"
    . ".\compile.ps1" -serverName $serverName -action "exe" -packId $id
}


Write-Host "----------- THE END --------------"