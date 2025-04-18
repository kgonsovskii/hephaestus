Write-Host $PSVersionTable.PSVersion
if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
  Stop-Service -Name W3SVC
} else {
  Write-Host "Service W3SVC does not exist."
}

try {
  Uninstall-WindowsFeature -Name Windows-Defender

Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force


Set-PSRepository -Name 'PSGallery' -InstallationPolicy Trusted
Install-Module -Name ps2exe  -Scope AllUsers
Install-Module -Name PSPKI -Scope AllUsers
Install-Module PSPKI
Import-Module PSPKI

Install-WindowsFeature FS-SMB1
Set-SmbServerConfiguration -EnableSMB2Protocol $true  -Force
New-NetFirewallRule -DisplayName "Allow SMB1 and SMB2" -Direction Inbound -Protocol TCP -LocalPort 445,139 -Action Allow -Profile Any

Install-WindowsFeature -Name DNS -IncludeManagementTools
Install-WindowsFeature -Name Web-Server, Web-Ftp-Server, Web-FTP-Ext, Web-Windows-Auth -IncludeManagementTools
Install-WindowsFeature web-scripting-tools

}
catch {
  <#Do this if a terminating exception happens#>
}

choco install dotnet-9.0-aspnetcore-hosting --yes --ignore-checksums --no-progress

Write-Host "Installing required IIS components"

$features = @(
  "IIS-WebServerRole",
  "IIS-WebServer",
  "IIS-CommonHttpFeatures",
  "IIS-StaticContent",
  "IIS-DefaultDocument",
  "IIS-HttpErrors",
  "IIS-ApplicationDevelopment",
  "IIS-ISAPIExtensions",
  "IIS-ISAPIFilter",
  "IIS-ManagementConsole",
  "IIS-RequestFiltering",
  "IIS-WindowsAuthentication",
  "IIS-LoggingLibraries",
  "NetFx4Extended-ASPNET45"
)
foreach ($feature in $features)
{
  Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart -ErrorAction SilentlyContinue
}

function Install-UrlRewrite {
    choco install urlrewrite --yes --ignore-checksums --no-progress
}
Install-UrlRewrite

if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
  Start-Service -Name W3SVC
} else {
  Write-Host "Service W3SVC does not exist."
}