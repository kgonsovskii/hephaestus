param (
    [string]$serverName
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptRoot = $PSScriptRoot
$includedScriptPath = Resolve-Path -Path (Join-Path -Path $scriptRoot -ChildPath "remote.ps1")
. $includedScriptPath  -serverName $serverName

Import-Module DnsServer

Set-DnsServerRecursion -Enable $true
$forwarderIP = "8.8.8.8"
Set-DnsServerForwarder -IPAddress $forwarderIP -Enable $true
Write-Host "Configured forwarder to use $forwarderIP"

function AddOrUpdateDnsRecord {
    param (
        [string]$zoneName,
        [string]$ip
    )

    $dnsServer = "localhost" 

    $recordName="@"

    $zoneExists = Get-DnsServerZone -Name $zoneName -ComputerName $dnsServer -ErrorAction SilentlyContinue
    if ($null -eq $zoneExists) {
        dnscmd . /zoneadd $zoneName /primary 
    } else {
        dnscmd $dnsServer /ZoneDelete $zoneName /f 2>$null
    }
    dnscmd . /zoneadd $zoneName /primary 
	Start-Sleep -Milliseconds 100
    $aRecords = Get-DnsServerResourceRecord -ZoneName $zoneName -RRType A -ComputerName $dnsServer
    foreach ($record in $aRecords) {
        Remove-DnsServerResourceRecord -ZoneName $zoneName -Name $record.Name -RRType A -ComputerName $dnsServer -Force
    }
    Add-DnsServerResourceRecordA -Name $recordName -ZoneName $zoneName -IPv4Address $ip -ComputerName $dnsServer -ErrorAction Stop
}


try 
{
    $dnsFolderPath = "$env:SystemRoot\System32\Dns"
    $Acl = Get-ACL $dnsFolderPath
    $AccessRule= New-Object System.Security.AccessControl.FileSystemAccessRule("everyone","FullControl","ContainerInherit,Objectinherit","none","Allow")
    $Acl.AddAccessRule($AccessRule)
    Set-Acl $dnsFolderPath $Acl
}
catch {

}



for ($i = 0; $i -lt $server.domains.Length; $i++) {
    $domain = $server.domains[$i]
    $ip = $server.interfaces[$i]
    AddOrUpdateDnsRecord $domain $ip
}