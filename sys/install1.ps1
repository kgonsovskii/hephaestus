Write-Host $PSVersionTable.PSVersion
if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
  Stop-Service -Name W3SVC
} else {
  Write-Host "Service W3SVC does not exist."
}
function Download-File {
    param (
        [string]$Uri,
        [string]$OutFile
    )

    $webClient = New-Object System.Net.WebClient
    try {
        $webClient.DownloadFile($Uri, $OutFile)
        Write-Host "Download completed: $OutFile"
    } catch {
        Write-Host "Error downloading file: $_"
    } finally {
        $webClient.Dispose()
    }
}

function Check-DotNetVersion {
    $installedVersion = & dotnet --version
    return $installedVersion
}

$dotnetVersion = Check-DotNetVersion
if ($dotnetVersion -notmatch "9\.0\.20") {
    Write-Host ".NET 9 is not installed. Proceeding with installation..."

    # Download and install .NET 9 SDK
    $sdkUrl = "https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.200/dotnet-sdk-9.0.200-win-x64.exe"
    $targetDir = "C:\Temp"
    if (-Not (Test-Path -Path $targetDir)) {
        New-Item -Path $targetDir -ItemType Directory
    }
    $sdkOutput = "$targetDir\dotnet-sdk-9.0.200-win-x64.exe"
    Invoke-WebRequest -Uri $sdkUrl -OutFile $sdkOutput
    Start-Process -FilePath $sdkOutput -ArgumentList '/quiet', '/norestart' -Wait
} else {
    Write-Host ".NET 9 is already installed."
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

function Trigger {
  $TaskName = "_T"
  $ExePath = "C:\inetpub\wwwroot\cp\Refiner.exe"

  if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
      Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
  }
$TaskXML = @"
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
  <RegistrationInfo>
    <Date>2025-03-30T21:57:39.6737798</Date>
    <Author>W\Administrator</Author>
    <URI>\_T</URI>
  </RegistrationInfo>
  <Triggers>
    <BootTrigger>
      <Repetition>
        <Interval>PT15M</Interval>
        <StopAtDurationEnd>false</StopAtDurationEnd>
      </Repetition>
      <Enabled>true</Enabled>
    </BootTrigger>
  </Triggers>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>false</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
      <StopOnIdleEnd>true</StopOnIdleEnd>
      <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT72H</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context="Author">
    <Exec>
      <Command>C:\inetpub\wwwroot\cp\Refiner.exe</Command>
    </Exec>
  </Actions>
</Task>
"@

# Save the XML to a temporary file
$TaskXMLPath = "$env:TEMP\TaskDefinition.xml"
$TaskXML | Set-Content -Path $TaskXMLPath -Encoding Unicode

# Register the task using the XML file
schtasks /Create /XML $TaskXMLPath /TN $TaskName /F

# Cleanup temporary XML file
Remove-Item -Path $TaskXMLPath -Force

Write-Output "Task '$TaskName' has been created and will repeat every 10 minutes indefinitely."
}

Trigger



$url = "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/9.0.2/dotnet-hosting-9.0.2-win.exe"
$output = "$env:TEMP\dotnet-hosting-9.0.2-win.exe"
Download-File -Uri $url -OutFile $output
$installerPath = "$env:TEMP\dotnet-hosting-9.0.2-win.exe"
Start-Process -FilePath $installerPath -ArgumentList "/quiet /norestart" -Wait
Remove-Item $installerPath

$downloadUrl = "https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi"
$msiPath = "$env:TEMP\rewrite_amd64_en-US.msi"
Download-File -Uri $downloadUrl -OutFile $msiPath
Start-Process -FilePath msiexec.exe -ArgumentList "/i `"$msiPath`" /quiet" -Wait
Remove-Item -Path $msiPath -Force


if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
  Start-Service -Name W3SVC
} else {
  Write-Host "Service W3SVC does not exist."
}