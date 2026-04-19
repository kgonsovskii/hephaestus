param (
    [string]$serverName
)
if ($serverName -eq "") {
    $serverName = "185.247.141.125"
} 

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
    } else {
        $aRecords = Get-DnsServerResourceRecord -ZoneName $zoneName -RRType A -ComputerName $dnsServer
        foreach ($record in $aRecords) {
            Remove-DnsServerResourceRecord -ZoneName $zoneName -Name $record.HostName -RRType A -ComputerName $dnsServer -Force
        }
        dnscmd $dnsServer /ZoneDelete $zoneName /f 2>$null
    }
    dnscmd . /zoneadd $zoneName /primary 
	Start-Sleep -Milliseconds 10
    Add-DnsServerResourceRecordA -Name $recordName -ZoneName $zoneName -IPv4Address $ip -ComputerName $dnsServer -ErrorAction Stop
    Add-DnsServerResourceRecordA -Name "www" -ZoneName $zoneName -IPv4Address $ip -ComputerName $dnsServer -ErrorAction Stop
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


foreach ($domainIp in $server.domainIps)
{ 
    $ip = $domainIp.ip
    foreach ($domain in $domainIp.domains) 
    {
        AddOrUpdateDnsRecord $domain $ip
    }
}
