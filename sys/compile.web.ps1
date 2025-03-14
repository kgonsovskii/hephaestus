param (
    [string]$serverName, [string]$action = "apply"
)
. ".\lib.ps1"
if ($serverName -eq "") {
    $serverName = detectServer
} 

if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\current.ps1" -serverName $serverName


if (IsLocalServer -serverIp $server.serverIp)
{
    $session = Get-PSSession
    & ".\transfer.ps1"  -serverName $serverName -session $null
    & ".\dns.ps1"  -serverName $serverName
    & ".\iis.ps1"  -serverName $serverName
    & ".\ftp.ps1"  -serverName $serverName
}
else {
    $pass = $server.password
    if ([string]::IsNullOrEmpty($pass) -or $pass -eq "password")
    {
        $pass = [System.Environment]::GetEnvironmentVariable("SuperPassword_$serverName", [System.EnvironmentVariableTarget]::Machine)
    }
    $credentialObject = New-Object System.Management.Automation.PSCredential ($server.login, (ConvertTo-SecureString -String $pass -AsPlainText -Force))
    $session = New-PSSession -ComputerName $server.server -Credential $credentialObject

    
    & (Join-Path -Path $scriptDir -ChildPath "./transfer.ps1") -serverName $serverName -session $session

    if ($action -eq "apply")
    {
        Invoke-RemoteSysScript -Session $session -ArgumentList $serverName, "dns.ps1"

        Invoke-RemoteSysScript -Session $session -ArgumentList $serverName, "iis.ps1"

        Invoke-RemoteSysScript -Session $session -ArgumentList $serverName, "ftp.ps1"
    }
    
}


Write-Host "Compile Web complete"