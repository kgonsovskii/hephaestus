param (
    [string]$serverName
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptRoot = $PSScriptRoot
$includedScriptPath = Resolve-Path -Path (Join-Path -Path $scriptRoot -ChildPath "remote.ps1")
. $includedScriptPath  -serverName $serverName
$includedScriptPath = Resolve-Path -Path (Join-Path -Path $scriptRoot -ChildPath "lib.ps1")
. $includedScriptPath -serverName $serverName -usePath $usePath

Import-Module WebAdministration
Import-Module PSPKI

Create-FtpDevs

Create-FtpSite -ftpUrl $server.ftp -ftpPath $server.publishedAdsDir -ftpSiteName "_ftp" -ApplicationPool $appPoolName

Write-Host "ftp complete: ${server.ftp}, ${server.publishedAdsDir}"