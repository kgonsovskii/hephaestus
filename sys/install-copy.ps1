param (
    [string]$serverName,  [string]$user="",  [string]$password="", [string]$direct=""
)

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"

if ($serverName -eq "") {
    $serverName = detectServer
} 

if ($direct -ne "true")
{
    . ".\current.ps1" -serverName $serverName
}
. ".\install-lib.ps1" -serverName $serverName -user $user -password $password -direct $direct


if ($direct -eq "true")
{
    $serverIp = $serverName
}
else
{
    $password = $server.clone.clonePassword
    $user=$server.clone.cloneUser
    $serverIp = $server.clone.cloneServerIp
}
Write-Host "Install-Copy $serverName, serverIp $serverIp"

function CopyItems {
    param (
        [string]$FileMask
    )

    $spass = (ConvertTo-SecureString -String $password -AsPlainText -Force)
    $credentialObject = New-Object System.Management.Automation.PSCredential ($user, $spass)

    $session = New-PSSession -ComputerName $serverIp -Credential $credentialObject

    $currentDir = $scriptDir
    $fullPath = Join-Path -Path $currentDir -ChildPath $FileMask

    $files = Get-ChildItem -Path $fullPath

    Invoke-Command -Session $session -ScriptBlock {
        param($remoteDir)
        if (-Not (Test-Path -Path $remoteDir)) {
            New-Item -Path $remoteDir -ItemType Directory | Out-Null
        }
    } -ArgumentList "C:\Install"

    foreach ($file in $files) {
        $remotePath = "C:\Install\$($file.Name)"
        Copy-Item -Path $file.FullName -Destination $remotePath -ToSession $session -Force
    }
}


CopyItems -FileMask "install*.*"
