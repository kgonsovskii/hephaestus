param (
    [string]$serverName, [object]$session
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\current.ps1" -serverName $serverName
. ".\lib.ps1"


Add-Type -AssemblyName "System.IO.Compression.FileSystem"
Clear-Folder -FolderPath "C:\_publish2\"
Copy-Folder -SourcePath $server.certDir -DestinationPath "C:\_publish2\local\cert"

# no copy
# Copy-Folder -SourcePath $server.userDataDir -DestinationPath "C:\_publish2\local\data\$serverName"
Compress-FolderToZip -SourceFolder "C:\_publish2\local" -targetZipFile "C:\_publish2\local.zip"

if ($session -ne $null)
{
    Invoke-RemoteSysScript -Session $session -ArgumentList $serverName, "transfer2.ps1"

    Copy-Item -Path "C:\_publish2\local.zip" -Destination "C:\_publish2\local2.zip" -ToSession $session -Force 

    Invoke-RemoteSysScript -Session $session -ArgumentList $serverName, "transfer3.ps1"
}
 else 
{
    & ".\transfer2.ps1"  -serverName $serverName

    Copy-Item -Path "C:\_publish2\local.zip" -Destination "C:\_publish2\local2.zip"

    & ".\transfer3.ps1"  -serverName $serverName
}