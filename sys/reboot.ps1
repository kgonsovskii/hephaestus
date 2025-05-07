param (
    [string]$serverName
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\current.ps1" -serverName $serverName
. ".\lib.ps1"

$ipv4Addresses = Get-NetIPAddress -AddressFamily IPv4 | Select-Object -ExpandProperty IPAddress

if ($server.serverIp -in $ipv4Addresses)
{
    & ".\rebootPC.ps1"
}
else 
{
    $pass = $server.password
    if ([string]::IsNullOrEmpty($passs) -or $pass -eq "password")
    {
        $pass = [System.Environment]::GetEnvironmentVariable("SuperPassword_$serverName", [System.EnvironmentVariableTarget]::Machine)
    }
    $credentialObject = New-Object System.Management.Automation.PSCredential ($server.login, (ConvertTo-SecureString -String $pass -AsPlainText -Force))
    $session = New-PSSession -ComputerName $server.serverIp -Credential $credentialObject
    Invoke-RemoteSysScript -Session $session -ArgumentList $serverName, "rebootPC.ps1"
}

& ".\rebootPC.ps1"

