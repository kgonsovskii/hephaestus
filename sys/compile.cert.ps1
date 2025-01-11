param (
    [string]$serverName
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. ".\current.ps1" -serverName $serverName

Import-Module WebAdministration
Import-Module PSPKI


function CreateCertificate {
    param (
        [string]$domain
    )

    $friendlyNameX = "$domain $friendlyName"
    $expiryDate = (Get-Date).AddYears(25)
    
    $path = certFile($domain)
    $pathPfx = pfxFile($domain)

    if (-not (Test-Path $path)) {
        Write-Host "Certificate creating... $path"
  
        $cert = New-SelfSignedCertificate -DnsName $domain -CertStoreLocation "cert:\LocalMachine\My" -KeySpec KeyExchange -NotAfter $expiryDate -Subject "CN=$domain" -KeyExportPolicy Exportable -FriendlyName $friendlyNameX
    
        Move-Item -Path "Cert:\LocalMachine\My\$($cert.Thumbprint)" -Destination "Cert:\LocalMachine\Root" -Force:$force
    
        Write-Host $pathPfx
        Export-PfxCertificate -Cert $cert -FilePath $pathPfx -NoClobber -Force -Password $certPassword
    
        Export-Certificate -Cert $cert -FilePath $path -Force
    } else {
        Write-Host "Certificate exists. $pathPfx"
        $certificatePassword = ConvertTo-SecureString -String "123" -Force -AsPlainText
        $certificate = Import-PfxCertificate -FilePath $pathPfx -CertStoreLocation Cert:\LocalMachine\My -Password $certificatePassword -Exportable
        $certificate = Import-PfxCertificate -FilePath $pathPfx -CertStoreLocation Cert:\LocalMachine\Root -Password $certificatePassword -Exportable
        $certificate | Out-Null
    }
    if (-not (Test-Path $server.sourceCertDir))
    {
        New-Item -Path $server.sourceCertDir -ItemType Directory -Force
    }
    $pathSrc = (Join-Path -Path $server.sourceCertDir -ChildPath "$domain.cer")
    $pathPfxSrc = (Join-Path -Path $server.sourceCertDir -ChildPath "$domain.pfx")
    Copy-FileIfDifferentLocation -SourceFilePath $path -DestinationFilePath $pathSrc
    Copy-FileIfDifferentLocation -SourceFilePath $pathPfx -DestinationFilePath $pathPfxSrc
}

function Copy-FileIfDifferentLocation {
    param (
        [string]$SourceFilePath,
        [string]$DestinationFilePath
    )

    # Check if the source file exists
    if (-not (Test-Path -Path $SourceFilePath)) {
        Write-Error "Source file '$SourceFilePath' does not exist."
        return
    }

    # Check if the source and destination paths are the same
    if ($SourceFilePath -ieq $DestinationFilePath) {
        Write-Output "Source and destination paths are the same. No copy needed."
        return
    }

    # Perform the copy operation
    Copy-Item -Path $SourceFilePath -Destination $DestinationFilePath -Force
    Write-Output "Copied file from '$SourceFilePath' to '$DestinationFilePath'."
}

foreach ($domain in $server.domains) {
    CreateCertificate($domain)
}

Write-Host "Compile cert сomplete"