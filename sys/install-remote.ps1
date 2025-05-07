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

if ([string]::IsNullOrEmpty($serverIp))
{
    throw "No Server Ip defined"
}

Set-KeyboardLayouts
Start-Sleep -Seconds 1

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


function Ultra-RemoteFile {
    param (
        [string]$FilePath,  [int]$timeout = 1300
    )
   Write-Host "Ultra-RemoteFile $filePath .."
   WaitRestart

   $programPath = sharpRdp
   if (-not (Test-Path $programPath -PathType Leaf)) {
       throw "File not found: $programPath"
   }

   $tag = Get-Date -Format "yyyyMMdd-HHmmssfff"

   $scriptLines = @(Get-Content -Path $FilePath)
   $scriptLines = @(
       "if (Test-Path 'C:\tag.txt') { Remove-Item 'C:\tag.txt' }"
       $scriptLines
       "Set-Content -Path 'C:\tag.txt' -Value '$tag'"
   )

   $scriptContent = [string]::Join([Environment]::NewLine, $scriptLines)
   $tempScriptPath = "C:\t.ps1"
   Set-Content -Path $tempScriptPath -Value $scriptContent -Encoding UTF8

   $securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force
   $credential = New-Object System.Management.Automation.PSCredential ($user, $securePassword)
   $session = New-PSSession -ComputerName $serverIp -Credential $credential
   Copy-Item -Path $tempScriptPath -Destination $tempScriptPath -ToSession $session -Force
   Remove-PSSession -Session $session

   $cmd = "& '$tempScriptPath'"

   & $programPath --server=$serverIp --username=$user --password=$password --command=$cmd --tag=$tag --timeout=$timeout

   $result = WaitForLocalTag -tag $tag -timeout $timeout   
   if ($result -eq -1)
   {
       Write-Host "Ultra-RemoteFile TimeOut local $FilePath ..."
       Start-Sleep 1
       Ultra-RemoteFile -FilePath $FilePath -timeout $timeout
       return
   }  

   Write-Host "Ultra-RemoteFile complete $FilePath. waiting for tag..."
   $result = WaitForTag -tag $tag -timeout $timeout
   if ($result -eq -1)
   {
       Write-Host "Ultra-RemoteFile TimeOut remote $FilePath ..."
       Start-Sleep 1
       Ultra-RemoteFile -FilePath $FilePath -timeout $timeout
       return
   } 
   Write-Host "Ultra-RemoteFile complete $FilePath. and Tag"
}

function WaitSql {
    Write-Host "Waiting for sql"
    while ($true) {
        try {
            Invoke-RemoteCommand -ScriptBlock { 

                [string]$ServerInstance = "localhost\SQLEXPRESS"
                [string]$SqlCmdPath = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
                [int]$IntervalSeconds = 5
                while ($true) {
                    $result = & "$SqlCmdPath" -S $ServerInstance -Q "SELECT 1" -b -h -1 2>$null
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "SQL Server '$ServerInstance' is responsive."
                        break
                    } else {
                        Write-Host "Waiting for SQL Server '$ServerInstance'... retrying in $IntervalSeconds seconds."
                        Start-Sleep -Seconds $IntervalSeconds
                    }
                }
            }
            Start-Sleep -Seconds 1
            break
        }
        catch {
            Write-Host $_
            Start-Sleep -Seconds 1
        }
    }
    Write-Host "SQL SETUPED"
    Start-Sleep -Seconds 1
}

Invoke-RemoteFile -FilePath "install0.ps1" 

Ultra-RemoteFile -FilePath "installNop.ps1" -timeout 90
Ultra-RemoteFile -FilePath "installSql.ps1"
Invoke-RemoteFile -FilePath "installSqlTools.ps1"
WaitSql

Invoke-RemoteFile -FilePath "installWeb.ps1"
Invoke-RemoteFile -FilePath "installWeb2.ps1"
Invoke-RemoteFile -FilePath "installTrigger.ps1"

WaitRestart

. ".\publish.ps1" -serverIp $serverIp -user $user -password $password -direct $true
WaitRestart
Write-Host "----------- THE END --------------"