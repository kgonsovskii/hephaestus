
$psVer = $PSVersionTable.PSVersion.Major
Write-Host "PowerShell v: $psVer"



#net 9
$sdkUrl = "https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.200/dotnet-sdk-9.0.200-win-x64.exe"
$targetDir = "C:\Temp"
if (-Not (Test-Path -Path $targetDir)) {
    New-Item -Path $targetDir -ItemType Directory
}
$sdkOutput = "$targetDir\dotnet-sdk-9.0.200-win-x64.exe"
Invoke-WebRequest -Uri $sdkUrl -OutFile $sdkOutput
Start-Process -FilePath $sdkOutput -ArgumentList '/quiet', '/norestart' -Wait


#iis hosting core
$url = "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/9.0.2/dotnet-hosting-9.0.2-win.exe"
$output = "$env:TEMP\dotnet-hosting-9.0.2-win.exe"
Invoke-WebRequest -Uri $url -OutFile $output
$installerPath = "$env:TEMP\dotnet-hosting-9.0.2-win.exe"
Start-Process -FilePath $installerPath -ArgumentList "/quiet /norestart" -Wait
Remove-Item $installerPath