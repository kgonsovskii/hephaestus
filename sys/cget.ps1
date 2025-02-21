param (
    [string]$serverName, [string]$action = "apply", [string]$kill="kill", [string]$refiner
)

if ($serverName -eq "") {
    $serverName = "38.180.228.45"
    $action = "apply"
} 

if ([string]::IsNullOrEmpty($serverName))
{
    throw "compile.ps1 -serverName argument is null"
}

if (-not (Test-Path "C:\data"))
{
    New-Item -Path "C:\data" -ItemType Directory -Force
}

if (-not (Test-Path "C:/data/$serverName"))
{
    New-Item -Path "C:/data/$serverName" -ItemType Directory -Force
}

$url = "http://$serverName/data/server.json"
$folder ="C:/data/$serverName/server.json"

Invoke-WebRequest -Uri $Url -OutFile $folder