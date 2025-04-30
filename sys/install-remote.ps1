param (
    [string]$serverName
)

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"


if ($serverName -eq "") {
    $serverName = detectServer
} 

. ".\current.ps1" -serverName $serverName
. ".\install-lib.ps1" -serverName $serverName

$password = $server.clone.clonePassword
$user=$server.clone.cloneUser
$serverIp = $server.clone.cloneServerIp

function Invoke-RemoteFile {
    param (
        [string]$FilePath          # Local PowerShell script path (.ps1)
    )
   WaitRestart
   Write-Host "Invoke-RemoteFile $filePath .."
    try {
        $securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force
        $credential = New-Object System.Management.Automation.PSCredential ($user, $securePassword)

        $RemoteFileName = [System.IO.Path]::GetFileName($FilePath)
        $remoteScriptPath = "C:\$RemoteFileName"

        $session = New-PSSession -ComputerName $serverIp -Credential $credential

        Copy-Item -Path $FilePath -Destination $remoteScriptPath -ToSession $session -Force

        Invoke-Command -Session $session -ScriptBlock {
            param ($remotePath)

            & "powershell.exe" -ExecutionPolicy Bypass -File $remotePath
        } -ArgumentList $remoteScriptPath

        Remove-PSSession -Session $session
    } catch {
        throw "Error executing remote PowerShell script: $_"
    }
    Start-Sleep -Seconds 1
    Write-Host "Invoke-RemoteFile Complete $filePath .."
}

function UltraRemoteCmdFile {
    param (
        [string]$cmd,
        [int]$timeout = 60,
        [bool]$forever
    )

    Write-Host "UltraRemoteCmd $cmd ..."
    $programPath = sharpRdp
    if (-not (Test-Path $programPath -PathType Leaf)) {
        throw "File not found: $programPath"
    }

    $tag = Get-Date -Format "yyyyMMdd-HHmmssfff"

    $scriptLines = @()
    if (-not [string]::IsNullOrEmpty($cmd)) {
        $scriptLines += $cmd
    }

    if ($forever -eq $true) {
        $scriptLines += "Set-Content -Path 'C:\tag1.txt' -Value '$tag'"
    }

    $scriptContent = [string]::Join([Environment]::NewLine, $scriptLines)
    $tempScriptPath = "C:\temp.ps1"
    Set-Content -Path $tempScriptPath -Value $scriptContent -Encoding UTF8

    $execCmd = "powershell.exe -ExecutionPolicy Bypass -File `"$tempScriptPath`""

    & $programPath --server=$serverIp --username=$user --password=$password --command=$execCmd

    Write-Host "UltraRemoteCmd complete $cmd."

    if ($forever -eq $true) {
        WaitForTag -tag $tag
    }
}

function Ultra-RemoteFile {
    param (
        [string]$FilePath
    )
   WaitRestart
   Write-Host "Ultra-RemoteFile $filePath .."
   UltraRemoteCmdFile -cmd  "powershell.exe -ExecutionPolicy Bypass -File 'C:\$FilePath'" -timeout 800 -forever $true
}


Invoke-RemoteFile -FilePath "install0.ps1" 
Ultra-RemoteFile -FilePath "installSql.ps1"
Invoke-RemoteFile -FilePath "installSqlTools.ps1"
WaitSql
Ultra-RemoteFile -FilePath "installWeb.ps1"
Invoke-RemoteFile -FilePath "installWeb2.ps1"
Invoke-RemoteFile -FilePath "installTrigger.ps1"


WaitRestart
. ".\publish.ps1" -serverIp $serverIp -user $user -password $password -direct $true

Write-Host "----------- THE END --------------"