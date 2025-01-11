param (
    [string]$serverName
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptRoot = $PSScriptRoot
$includedScriptPath = Resolve-Path -Path (Join-Path -Path $scriptRoot -ChildPath "remote.ps1")
. $includedScriptPath  -serverName $serverName

Import-Module WebAdministration
Import-Module PSPKI

$portHttp = 80
$portHttps = 443

$ConfirmPreference = 'None'

# CLEAR SITES
function Remove-AllIISWebsites {
    if ($psVer -eq 7)
    {
        Reset-IISServerManager -Confirm:$false
        $manager = Get-IISServerManager
        for ($i = 0; $i -lt $manager.Sites.Count; $i++) {
            $site = $manager.Sites[$i]
            $siteName = $site.Name
            if ($siteName -eq "cp")
            {
                continue;
            }
            Write-Output "Removing site: $siteName"
            $manager.Sites.Remove($site)
        }
        $manager.CommitChanges()
    }
    else 
    {
        $websites = Get-Website
        foreach ($website in $websites) {
            $siteName = $website.Name
            if ($siteName -eq "cp")
            {
                continue;
            }
            Write-Host "Removing website $siteName..."
            Stop-WebSite -Name $siteName -ErrorAction SilentlyContinue
            Get-WebBinding -Name $siteName | ForEach-Object {
                Remove-WebBinding -Name $siteName -BindingInformation $_.BindingInformation
            }
            Remove-Website -Name $siteName
        }
    }
    Write-Host "All websites and bindings have been removed."
}
Remove-AllIISWebsites

 #remove and create pool
function Remove-Pool(){
    
     if (Test-Path "IIS:\AppPools\$appPoolName") {
        Stop-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue
        Remove-Item "IIS:\AppPools\$appPoolName" -Recurse
        Write-Output "Existing identity for '$appPoolName' removed."
    }

    New-Item "IIS:\AppPools\$appPoolName"
    Get-Item "IIS:\AppPools\$appPoolName"
    Set-ItemProperty IIS:\AppPools\$appPoolName -Name processModel.identityType -Value "ApplicationPoolIdentity"
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedPipelineMode" -Value "Integrated"
    Set-WebConfigurationProperty -Filter '/system.webServer/httpErrors' -Name errorMode -Value Detailed

    $acl = Get-Acl $server.publishedAdsDir
    $permission = "IIS AppPool\$appPoolName", "Read,Write", "ContainerInherit, ObjectInherit", "None", "Allow"
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
    $acl.SetAccessRule($accessRule)
    Set-Acl $server.publishedAdsDir $acl

    Start-WebAppPool -Name $appPoolName
}
Remove-Pool


# REMOVE SERTS
function Remove-CertificatesV5 {
    # List of certificate stores to search
    $stores = @(
        "CurrentUser\My",
        "LocalMachine\My",
        "CurrentUser\Root",
        "LocalMachine\Root",
        "CurrentUser\CA",
        "LocalMachine\CA",
        "CurrentUser\AuthRoot",
        "LocalMachine\AuthRoot"
    )
    foreach ($storeLocation in $stores) {
        try {
            $certs = Get-ChildItem -Path "cert:\$storeLocation" | Where-Object { $_.FriendlyName -like "*$friendlyName*" }
            $certs | %{Remove-Item -path $_.PSPath -recurse -Force}
        }
        catch {
        
        }
    }
}

function Remove-CertificatesV7 {
    param (
        [string[]]$storeLocations = @("LocalMachine", "CurrentUser"),
        [string[]]$storeNames = @("Root", "My")  # You can add more store names as needed
    )
    Add-Type -AssemblyName "System.Security.Cryptography.X509Certificates"
    foreach ($storeLocation in $storeLocations) {
        foreach ($storeName in $storeNames) {
            try {
                # Open the certificate store
                $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($storeName, $storeLocation)
                $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
                $certificates = $store.Certificates
                for ($i = 0; $i -lt $certificates.Count; $i++) {
                    $cert = $certificates[$i]
                    if ($cert.FriendlyName -like "*$friendlyName*")
                    {
                        Write-Output "Removing certificate: $($cert.Subject) from $storeName store in $storeLocation location"
                        $store.Remove($cert)
                    }
                }
                $store.Close()
                Write-Output "All certificates removed from $storeName store in $storeLocation location successfully."
            }
            catch {
                Write-Error "Failed to remove certificates from $storeName store in $storeLocation location: $_"
            }
        }
    }
}
if ($psVer -eq 7)
{
    Remove-CertificatesV7
}
else {
    Remove-CertificatesV5
}


function HexToBytes($hex) {
    $bytes = for($i = 0; $i -lt $hex.Length; $i += 2) {
        [convert]::ToByte($hex.SubString($i, 2), 16)
    }

    return $bytes
}

function CreateWebsite {
    param (
        [string]$domain,
        [string]$ip
    )
    
    $hostHeader = $domain
    $siteName = $domain

    Write-Output "Start website $domain"
    
    Remove-Website -Name $siteName  -ErrorAction SilentlyContinue

    $path = $server.publishedAdsDir
    $pathPfx = pfxFile($domain)
    $certRoot = Import-PfxCertificate -FilePath $pathPfx -CertStoreLocation Cert:\LocalMachine\Root -Password $certPassword -Exportable
    $certRootMy = Import-PfxCertificate -FilePath $pathPfx -CertStoreLocation Cert:\LocalMachine\My -Password $certPassword -Exportable

    #$certUseRoot = Import-PfxCertificate -FilePath $pathPfx -CertStoreLocation Cert:\CurrentUser\Root -Password $certPassword -Exportable
    #$certUserMy = Import-PfxCertificate -FilePath $pathPfx -CertStoreLocation Cert:\CurrentUser\My -Password $certPassword -Exportable

    if ($psVer -eq 7)
    {
        $manager = Get-IISServerManager
        $site = $manager.Sites.Add($siteName, $path, 80)
        $site.ServerAutoStart = $true;
        $ipport="*:${portHttps}:$hostHeader"
        $thumbprintBytes = HexToBytes $sslCert.Thumbprint
        $site.Bindings.Add($ipport, $thumbprintBytes, $certRoot.StoreName, 1) | Out-Null
        $manager.CommitChanges()
        #TODO: POOL
    }
    else {
        New-Website -Name $siteName -HostHeader $hostHeader -PhysicalPath $path -Port $portHttp -IPAddress $ip -ApplicationPool $appPoolName
        $httpsBinding = Get-WebBinding -Port $portHttps -Name $siteName -HostHeader $hostHeader -Protocol "https" -ErrorAction SilentlyContinue
        if ($httpsBinding) {
            Remove-WebBinding -Name $siteName -Protocol "https" -Port $portHttps --HostHeader $hostHeader
        }
        New-WebBinding -Name $siteName -IPAddress $ip -Port $portHttp -HostHeader "" -Protocol "http" 
        New-WebBinding -Name $siteName -IPAddress $ip -Port $portHttps -HostHeader $hostHeader -Protocol "https" 
        $httpsBinding = Get-WebBinding -Port $portHttps -Name $siteName -HostHeader $hostHeader -Protocol "https"    
        $httpsBinding.AddSslCertificate($certRootMy.Thumbprint, "My")
    }

    Write-Output "Finish website $domain"
}


# RUN
for ($i = 0; $i -lt $server.domains.Length; $i++) {
    $domain = $server.domains[$i]
    $ip = $server.interfaces[$i]
    CreateWebsite -domain $domain $ip
}

Write-Host "Done IIS"