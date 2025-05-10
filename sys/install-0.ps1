# Stop IIS service if it exists
if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
    Stop-Service -Name W3SVC
} else {
    Write-Host "Service W3SVC does not exist."
}

$regPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Reliability"
if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }
Set-ItemProperty -Path $regPath -Name ShutdownReasonOn -Value 0 -Type DWord -Force

Enable-PSRemoting -Force
Set-Service -Name WinRM -StartupType Automatic
Start-Service -Name WinRM

$psVer = $PSVersionTable.PSVersion.Major
Write-Host "PowerShell v: $psVer"

try {
    Set-ExecutionPolicy Unrestricted -Scope LocalMachine -Force
    Set-ExecutionPolicy Unrestricted -Scope CurrentUser -Force
}
catch {

}



function Update-FirewallRule {
    param (
        [string]$Name,
        [string]$DisplayName,
        [string]$Description,
        [int]$LocalPort,
        [string]$Protocol,
        [string]$Profile = 'Any',
        [string]$RemoteAddress = 'Any',
        [string]$Program = 'Any'
    )
    try {
        $existingRule = Get-NetFirewallRule -Name $Name -ErrorAction Stop
        Set-NetFirewallRule -Name $Name -Profile $Profile -RemoteAddress $RemoteAddress -Program $Program
        Enable-NetFirewallRule -Name $Name
        Write-Output "Rule '$Name' updated."
    }
    catch {
        New-NetFirewallRule -Name $Name -DisplayName $DisplayName -Description $Description -Protocol $Protocol -LocalPort $LocalPort -Action Allow -Profile $Profile -RemoteAddress $RemoteAddress -Program $Program
        Write-Output "Rule '$Name' created."
    }
}
Update-FirewallRule -Name "WinRM-HTTP-In-TCP" -DisplayName "WinRM (HTTP-In)" -Description "Inbound rule for WinRM (HTTP-In)" -Protocol TCP -LocalPort 5985
Update-FirewallRule -Name "WinRM-HTTPS-In-TCP" -DisplayName "WinRM (HTTPS-In)" -Description "Inbound rule for WinRM (HTTPS-In)" -Protocol TCP -LocalPort 5986
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name "LocalAccountTokenFilterPolicy" -Value 1 -Type DWORD -Force


try
{
set-item -force WSMan:\localhost\Service\AllowUnencrypted $true
winrm set winrm/config/client '@{AllowUnencrypted="true"}'
Set-Item -Path WSMan:\localhost\Service\Auth\Basic -Value $true
Set-Item -Path WSMan:\localhost\Service\AllowUnencrypted -Value $true
if (-not (Get-NetFirewallRule -Name "WinRM-HTTP-In-TCP" -ErrorAction SilentlyContinue)) { New-NetFirewallRule -Name "WinRM-HTTP-In-TCP" -DisplayName "WinRM (HTTP-In)" -Description "Inbound rule for WinRM (HTTP-In)" -Protocol TCP -LocalPort 5985 -Action Allow } else { Enable-NetFirewallRule -Name "WinRM-HTTP-In-TCP" }
if (-not (Get-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" -ErrorAction SilentlyContinue)) { New-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" -DisplayName "WinRM (HTTPS-In)" -Description "Inbound rule for WinRM (HTTPS-In)" -Protocol TCP -LocalPort 5986 -Action Allow } else { Enable-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" }

}
catch{
    Write-Host $_
}


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
