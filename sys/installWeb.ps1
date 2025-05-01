# Stop IIS service if it exists
if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
    Stop-Service -Name W3SVC
} else {
    Write-Host "Service W3SVC does not exist."
}

try {
    # Uninstall Windows Defender (note: not always possible via this cmdlet on client systems)
    Uninstall-WindowsFeature -Name Windows-Defender -ErrorAction SilentlyContinue

    # Install NuGet for module installation
    Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force

    # Trust PSGallery and install modules
    Set-PSRepository -Name 'PSGallery' -InstallationPolicy Trusted
    Install-Module -Name ps2exe -Scope AllUsers -Force
    Install-Module -Name PSPKI -Scope AllUsers -Force
    Import-Module PSPKI

    # Enable SMB1 and configure firewall rule
    Install-WindowsFeature FS-SMB1
    Set-SmbServerConfiguration -EnableSMB2Protocol $true -Force
    New-NetFirewallRule -DisplayName "Allow SMB1 and SMB2" -Direction Inbound -Protocol TCP -LocalPort 445,139 -Action Allow -Profile Any

    # Install DNS and Web server components
    Install-WindowsFeature -Name DNS -IncludeManagementTools
    Install-WindowsFeature -Name Web-Server, Web-Ftp-Server, Web-FTP-Ext, Web-Windows-Auth -IncludeManagementTools
    Install-WindowsFeature -Name Web-Scripting-Tools

} catch {
    Write-Host "An error occurred during feature installation: $_"
}

Write-Host "Installing required IIS components..."

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

foreach ($feature in $features) {
    Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart -ErrorAction SilentlyContinue
}

function Download-File {
  param (
      [Parameter(Mandatory)]
      [string]$Uri,

      [Parameter(Mandatory)]
      [string]$OutFile
  )

  $client = New-Object System.Net.WebClient
  $client.DownloadFile($Uri, $OutFile)
}

# Start W3SVC again if it exists
if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
    Start-Service -Name W3SVC
} else {
    Write-Host "Service W3SVC does not exist."
}
