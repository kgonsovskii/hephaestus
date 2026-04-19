param (
    [string]$serverName
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "current.ps1 -serverName argument is null"
}
$psVer = $PSVersionTable.PSVersion.Major
Write-Host "PowerShell v: $psVer"

$dataDir="C:\data\$serverName"
$appPoolName = "AdsPool"
$serverPath = Resolve-Path -Path (Join-Path -Path $dataDir -ChildPath "server.json")
$certDir = Resolve-Path -Path ("C:\inetpub\wwwroot\cert")
$server = Get-Content -Path $serverPath -Raw | ConvertFrom-Json
$certPassword = ConvertTo-SecureString -String "123" -Force -AsPlainText
$friendlyName="IIS Root Authority"
$certLocation="cert:\LocalMachine\Root"
if ([string]::IsNullOrEmpty($server.password) -or $server.password -eq "password") {
    $hh = $server.server
    $server.password= [System.Environment]::GetEnvironmentVariable("SuperPassword_$hh", [System.EnvironmentVariableTarget]::Machine)
    if ([string]::IsNullOrEmpty($server.password) -or $server.password -eq "password") {
        $server.password = [System.Environment]::GetEnvironmentVariable('SuperPassword', [System.EnvironmentVariableTarget]::Machine)
    }
}

function pfxFile {
    param (
        [string]$domain
    )
    return (Join-Path -Path $certDir -ChildPath "$domain.pfx")
}

function certFile {
    param (
        [string]$domain
    )
    return (Join-Path -Path $certDir -ChildPath "$domain.cer")
}