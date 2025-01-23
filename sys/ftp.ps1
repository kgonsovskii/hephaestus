param (
    [string]$serverName
)

if ($serverName -eq "") {
    $serverName = "127.0.0.1"
} 

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

Write-Host "ftp complete: ${server.ftp}, ${server.publishedAdsDir}"

for ($i = 0; $i -lt $server.domainIps.Length; $i++) {
    $domainIp = $server.domainIps[$i]
    $ftp = $domainIp.ftp
    $path = $domainIp.ads
    $name = $domainIp.name
    foreach ($domain in $domainIp.domains) 
    {    
        Create-FtpSite -ftpUrl $ftp -ftpPath $path -ftpSiteName "_ftp_$name" -ApplicationPool $appPoolName

    }
}