Set-PSRepository -Name 'PSGallery' -InstallationPolicy Trusted


$psVer = $PSVersionTable.PSVersion.Major
Write-Host "PowerShell v: $psVer"


# dotnet 7 install
$sdkUrl = "https://download.visualstudio.microsoft.com/download/pr/6f7abf5c-3f6d-43cc-8f3c-700c27d4976b/b7a3b806505c95c7095ca1e8c057e987/dotnet-sdk-7.0.410-win-x64.exe"
$targetDir = "C:\Temp"
if (-Not (Test-Path -Path $targetDir)) {
    New-Item -Path $targetDir -ItemType Directory
}
$sdkOutput = "$targetDir\dotnet-sdk-7.0.100-win-x64.exe"
function Download-File {
    param (
        [string]$url,
        [string]$outputPath
    )

    Write-Host "Downloading from $url to $outputPath"
    try {
        Invoke-WebRequest -Uri $url -OutFile $outputPath -UseBasicParsing -ErrorAction Stop
        Write-Host "Downloaded $outputPath successfully."
    } catch {
        Write-Host "Failed to download $url. Error: $_"
        exit 1
    }
}
Download-File -url $sdkUrl -outputPath $sdkOutput
Write-Host "Installing .NET SDK..."
try {
    Start-Process -FilePath $sdkOutput -ArgumentList "/quiet" -Wait
    Write-Host ".NET SDK installed successfully."
} catch {
    Write-Host "Failed to install .NET SDK. Error: $_"
    exit 1
}
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::Machine)
Write-Host "Verifying .NET installation..."
try {
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        dotnet --list-sdks
        dotnet --list-runtimes
    } else {
        Write-Host "dotnet command is not recognized. Please ensure .NET is installed correctly and the PATH is updated."
    }
} catch {
    Write-Host "Error while verifying .NET installation. Error: $_"
}



if ($psVer -eq 7)
{
    Enable-WindowsOptionalFeature -Online -FeatureName IIS-WindowsAuthentication -NoRestart
    $feats = @("IIS-WebServerRole","IIS-WebServer","IIS-CommonHttpFeatures","IIS-HttpErrors","IIS-Security","IIS-RequestFiltering","IIS-WebServerManagementTools","IIS-DigestAuthentication","IIS-StaticContent","IIS-DefaultDocument","IIS-DirectoryBrowsing","IIS-WebDAV","IIS-BasicAuthentication","IIS-ManagementConsole");
    foreach ($feat in $feats) 
    {
        Enable-WindowsOptionalFeature -Online -FeatureName $feat -NoRestart
    };
} else 
{
    Install-WindowsFeature -Name DNS -IncludeManagementTools
    Install-WindowsFeature -Name Web-Server, Web-Ftp-Server, Web-FTP-Ext, Web-Windows-Auth -IncludeManagementTools
    Install-WindowsFeature web-scripting-tools

    Install-Module PSPKI
    Import-Module WebAdministration
    Import-Module PSPKI
    Import-Module ServerManager
}

$downloadUrl = "https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi"
$msiPath = "$env:TEMP\rewrite_amd64_en-US.msi"
Invoke-WebRequest -Uri $downloadUrl -OutFile $msiPath
Start-Process -FilePath msiexec.exe -ArgumentList "/i `"$msiPath`" /quiet" -Wait
Remove-Item -Path $msiPath -Force



#iis hosting core
$url = "https://dotnetcli.azureedge.net/dotnet/aspnetcore/Runtime/7.0.20/dotnet-hosting-7.0.20-win.exe"
$output = "$env:TEMP\dotnet-hosting-7.0.20-win.exe"
Invoke-WebRequest -Uri $url -OutFile $output
$installerPath = "$env:TEMP\dotnet-hosting-7.0.20-win.exe"
Start-Process -FilePath $installerPath -ArgumentList "/quiet /norestart" -Wait
Remove-Item $installerPath


IISReset
Write-Host "Installatin 2 complete"