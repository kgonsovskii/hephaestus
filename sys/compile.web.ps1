param (
    [string]$serverName, [string]$action = "apply"
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\current.ps1" -serverName $serverName
. ".\lib.ps1"

if ([string]::IsNullOrEmpty($server.password)) {
    $session = Get-PSSession
}
else {
    $credentialObject = New-Object System.Management.Automation.PSCredential ($server.login, (ConvertTo-SecureString -String $server.password -AsPlainText -Force))
    $session = New-PSSession -ComputerName $server.server -Credential $credentialObject
    
}

& (Join-Path -Path $scriptDir -ChildPath "./transfer.ps1") -serverName $serverName -session $session

if ($action -eq "apply")
{
    Invoke-RemoteSysScript -Session $session -ArgumentList $serverName, "dns.ps1"

    Invoke-RemoteSysScript -Session $session -ArgumentList $serverName, "iis.ps1"

    Invoke-RemoteSysScript -Session $session -ArgumentList $serverName, "ftp.ps1"
}
Write-Host "Compile Web complete"