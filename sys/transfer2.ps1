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

if (-not (Test-Path "C:\_publish2\"))
{
    New-Item -Path "C:\_publish2" -ItemType Directory -Force -ErrorAction SilentlyContinue
}
if (Test-Path "C:\_publish2\local2.zip")
{
    Remove-Item -Path "C:\_publish2\local2.zip"
    Remove-Item -Path "C:\_publish2\extracted" -Force -Recurse
}