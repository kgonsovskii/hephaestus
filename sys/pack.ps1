param (
    [string]$serverName, [string]$packId = ""
)

if ($serverName -eq "") {
    $serverName = detectServer
} 

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"
. ".\current.ps1" -serverName $serverName


if ([string]::IsNullOrEmpty($packId) -eq $false) {
    $pack = $server.pack.items | Where-Object { $_.index -eq $packId }
    if (-not $pack) {
        throw "Item with index '$packId' not found in pack items."
    }
    $url = $pack.originalUrl
    $index = $pack.Index
    Write-Host "Packing individual $index -> $url"
    . ".\compile.ps1" -serverName $serverName -action "exe" -packId $packId
    exit
}   

# RUN
foreach ($pack in $server.pack.items)
{ 
    $url = $pack.originalUrl
    $index = $pack.Index
    Write-Host "Packing $index -> $url"
    . ".\compile.ps1" -serverName $serverName -action "exe" -packId $index
}


Write-Host "----------- THE END --------------"