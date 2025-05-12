# Stop IIS service if it exists
if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
    Stop-Service -Name W3SVC
} else {
    Write-Host "Service W3SVC does not exist."
}

. ".\install-x.ps1"


function Install-Chocolatey {
    # Check if Chocolatey is installed
    $chocoInstalled = Get-Command choco -ErrorAction SilentlyContinue
  
    if (-not $chocoInstalled) {
        Write-Host "Chocolatey is not installed. Installing Chocolatey..."
  
        # Install Chocolatey
        Set-ExecutionPolicy Bypass -Scope Process -Force; 
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;
        Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
  
        # Wait for Chocolatey installation to complete
        Write-Host "Chocolatey installation is complete."
    } else {
        Write-Host "Chocolatey is already installed."
    }
}
Install-Chocolatey

choco install dotnet-9.0-sdk --yes --ignore-checksums --no-progress
choco install far --yes --ignore-checksums --no-progress
choco install git --yes --ignore-checksums --no-progress
