
$psVer = $PSVersionTable.PSVersion.Major
Write-Host "PowerShell v: $psVer"


# POWERSHELL OLD
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
Stop-Service WinRM
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name "LocalAccountTokenFilterPolicy" -Value 1 -Type DWORD -Force
Start-Service WinRM
try
{
Enable-PSRemoting -Force
}
catch{}

try
{
set-item -force WSMan:\localhost\Service\AllowUnencrypted $true
winrm set winrm/config/client '@{AllowUnencrypted="true"}'
Set-Item -Path WSMan:\localhost\Service\Auth\Basic -Value $true
Set-Item -Path WSMan:\localhost\Service\AllowUnencrypted -Value $true
if (-not (Get-NetFirewallRule -Name "WinRM-HTTP-In-TCP" -ErrorAction SilentlyContinue)) { New-NetFirewallRule -Name "WinRM-HTTP-In-TCP" -DisplayName "WinRM (HTTP-In)" -Description "Inbound rule for WinRM (HTTP-In)" -Protocol TCP -LocalPort 5985 -Action Allow } else { Enable-NetFirewallRule -Name "WinRM-HTTP-In-TCP" }
if (-not (Get-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" -ErrorAction SilentlyContinue)) { New-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" -DisplayName "WinRM (HTTPS-In)" -Description "Inbound rule for WinRM (HTTPS-In)" -Protocol TCP -LocalPort 5986 -Action Allow } else { Enable-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" }
Stop-Service WinRM
Start-Service WinRM
Get-Service WinRM
}
catch{
    Write-Host $_
}



#POWESHELL 7
# $version = "7.4.3"
# $url = "https://github.com/PowerShell/PowerShell/releases/download/v$version/PowerShell-$version-win-x64.msi"
# $outputDir = "C:\Temp"
# $outputFile = "$outputDir\PowerShell-$version-win-x64.msi"
# if (!(Test-Path -Path $outputDir -PathType Container)) {
#     New-Item -Path $outputDir -ItemType Directory | Out-Null
# }
# Invoke-WebRequest -Uri $url -OutFile $outputFile
# Start-Process msiexec.exe -ArgumentList "/i $outputFile /quiet /norestart" -Wait

# # Enable PowerShell remoting
# $pwshPath = "C:\Program Files\PowerShell\7\pwsh.exe"
# & $pwshPath -Command "Enable-PSRemoting -Force"


# #SSH
# try {
#     $existingRule = Get-NetFirewallRule -Name 'sshd' -ErrorAction SilentlyContinue
#     if ($existingRule) {
#         Set-NetFirewallRule -Name 'sshd' -RemoteAddress Any -Profile Any -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22 -ErrorAction Stop
#         Write-Output "Updated firewall rule 'sshd' to allow SSH (port 22) for any profile, any IP, and any program."
#     } else {
#         New-NetFirewallRule -Name 'sshd' -DisplayName 'OpenSSH Server (sshd)' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22 -RemoteAddress Any -Profile Any -ErrorAction Stop
#         Write-Output "Created firewall rule 'sshd' to allow SSH (port 22) for any profile, any IP, and any program."
#     }
# } catch {
#     Write-Error "Failed to create/update firewall rule for SSH:`n$_"
# }
# Add-WindowsCapability -Online -Name OpenSSH.Server
# Add-WindowsCapability -Online -Name OpenSSH.Client
# Set-Service -Name sshd -StartupType 'Automatic'
# Start-Service sshd


#PowerShell 5.1
$wmfVersion = "5.1"
$wmfUrl = "https://go.microsoft.com/fwlink/?LinkId=817261" # Updated link for WMF 5.1
$outputDir = "C:\Temp"
$outputFile = "$outputDir\WindowsTH-KB3191564-x64.msu"

if (!(Test-Path -Path $outputDir -PathType Container)) {
    New-Item -Path $outputDir -ItemType Directory | Out-Null
}

#PowerShell 5.1 DEFS
Invoke-WebRequest -Uri $wmfUrl -OutFile $outputFile
Start-Process wusa.exe -ArgumentList "$outputFile /quiet /norestart" -Wait
$PSVersion = $PSVersionTable.PSVersion
if ($PSVersion.Major -eq 5 -and $PSVersion.Minor -eq 1) {
    Write-Output "PowerShell 5.1 has been successfully installed."
} else {
    Write-Output "PowerShell 5.1 installation failed."
}
# Ensure PowerShell 5.1 is installed
$PSVersionTable.PSVersion
# Delete existing WSMan configuration for PowerShell if it exists
$existingConfig = Get-PSSessionConfiguration -Name "Microsoft.PowerShell"
if ($existingConfig) {
    Unregister-PSSessionConfiguration -Name "Microsoft.PowerShell" -Force
}
# Register new WSMan configuration for PowerShell 5.1
Register-PSSessionConfiguration -Name "Microsoft.PowerShell" -Force -ShowSecurityDescriptorUI
# Restart WinRM service
Restart-Service WinRM
# Enable PowerShell remoting
Enable-PSRemoting -Force
# Verify WSMan configuration
Get-PSSessionConfiguration


Set-Item WSMan:\localhost\Client\TrustedHosts -Value "185.247.141.76, 213.226.112.110, 109.248.201.219"


# Write-Host "Installatin 1 complete"

