
. ".\1ver.ps1"

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

$srv = detectServer
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "$srv, 127.0.0.1" -Force
Restart-Service WinRM



Set-MpPreference -DisableRealtimeMonitoring $true
Uninstall-WindowsFeature -Name Windows-Defender



Set-PSRepository -Name 'PSGallery' -InstallationPolicy Trusted



Install-Module -Name ps2exe  -Scope AllUsers
Install-Module -Name PSPKI -Scope AllUsers
Install-Module PSPKI
Import-Module WebAdministration
Import-Module PSPKI
Import-Module ServerManager

Install-WindowsFeature FS-SMB1
Set-SmbServerConfiguration -EnableSMB2Protocol $true  -Force
New-NetFirewallRule -DisplayName "Allow SMB1 and SMB2" -Direction Inbound -Protocol TCP -LocalPort 445,139 -Action Allow -Profile Any

Install-WindowsFeature -Name DNS -IncludeManagementTools
Install-WindowsFeature -Name Web-Server, Web-Ftp-Server, Web-FTP-Ext, Web-Windows-Auth -IncludeManagementTools
Install-WindowsFeature web-scripting-tools

$downloadUrl = "https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi"
$msiPath = "$env:TEMP\rewrite_amd64_en-US.msi"
Invoke-WebRequest -Uri $downloadUrl -OutFile $msiPath
Start-Process -FilePath msiexec.exe -ArgumentList "/i `"$msiPath`" /quiet" -Wait
Remove-Item -Path $msiPath -Force

IISReset
Write-Host "Installatin 2 complete"
