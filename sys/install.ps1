param (
    [string]$serverName
)

function Set-KeyboardLayouts {
    $langlist = New-WinUserLanguageList en-US
    $langlist[0].InputMethodTips.Clear()
    $langlist[0].InputMethodTips.Add('0409:00000409')
    $langlist.Add((New-WinUserLanguageList ru-RU)[0])
    $langlist[1].InputMethodTips.Clear()
    $langlist[1].InputMethodTips.Add('0419:00000419')
    Set-WinUserLanguageList $langlist -Force
    Set-WinUILanguageOverride -Language en-US
}
Set-KeyboardLayouts
Start-Sleep -Seconds 1

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"


if ($serverName -eq "") {
    $serverName = detectServer
} 


. ".\current.ps1" -serverName $serverName

$password = $server.clone.clonePassword
$user=$server.clone.cloneUser
$serverIp = $server.clone.cloneServerIp

function AddTrusted {
    param ($hostname)

    $currentTrustedHosts = (Get-Item WSMan:\localhost\Client\TrustedHosts).Value
    if ([string]::IsNullOrEmpty($currentTrustedHosts)) {
        $newTrustedHosts = $hostname
    } else {
        if ($currentTrustedHosts -notmatch [regex]::Escape($hostname)) {
            $newTrustedHosts = "$currentTrustedHosts,$hostname"
        } else {
            $newTrustedHosts = $currentTrustedHosts
        }
    }
    if ($currentTrustedHosts -ne $newTrustedHosts) {
        Set-Item WSMan:\localhost\Client\TrustedHosts -Value $newTrustedHosts -Force
    }
    Get-Item WSMan:\localhost\Client\TrustedHosts
    Set-Item WSMan:\localhost\Client\AllowUnencrypted -Value $true
}

function sharpRdp {
    $programPath = Join-Path $scriptDir "../rdp/SharpRdp.exe"
    $resolvedPath = Resolve-Path $programPath -ErrorAction SilentlyContinue
    if (-not $resolvedPath) {
        $programPath = Join-Path $scriptDir "../cp/SharpRdp.exe"
        $resolvedPath = Resolve-Path $programPath -ErrorAction SilentlyContinue
    }    
    Write-Host $programPath
    return $programPath
}

function UltraRemoteCmd {
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
    if ($forever -eq $true -and [string]::IsNullOrEmpty($cmd) -eq $false)
    {
        $cmd =  $cmd + "; Set-Content -Path 'C:\tag1.txt' -Value '$tag'" + "; "
    }
    & $programPath --server=$serverIp --username=$user --password=$password --command=$cmd
   Write-Host "UltraRemoteCmd complete $cmd."
    if ($forever -eq $true)
    {
        WaitForTag -tag $tag
    }
}

function WaitForTag {
    param (
        [string]$tag
    )
   while ($true) {
       Start-Sleep -Seconds 1
       try {
           Invoke-RemoteCommand -ScriptBlock {
                param (
                    [string]$tag
                )
                Set-Content -Path 'C:\tagR.txt' -Value $tag
                $filePath = "C:\tag1.txt"
                function IsTag() {
                    if (Test-Path $filePath) {
                        $content = Get-Content -Path $filePath -Raw
                        $co = $content  -like "*$tag*"
                        return $co
                    } else {
                        return $false
                    }
                }
                while ($true) {
                    if (IsTag) {
                        Write-Host "Tag '$expectedTag' detected!"
                        break
                    }
                    Start-Sleep -Seconds 1
                }
        
            } -Arguments @($tag)   
           break
       }
       catch {
          Write-Host $_
          Start-Sleep -Seconds 1
       }
       Start-Sleep -Seconds 1
   }
   Start-Sleep -Seconds 1
   Write-Host "Tag found"
}

function WaitRestart {
    Write-Host "Restarting.."
   while ($true) {
       try {
           Invoke-RemoteCommand -ScriptBlock { shutdown /r /t 0 /f }
           Start-Sleep -Seconds 3
           break
       }
       catch {
           Start-Sleep -Seconds 3
       }
       Write-Host "Restarting attempt.."
   }
   
   while ($true) {
       Start-Sleep -Seconds 1
       try {
           Write-Host "testing.."
           Invoke-RemoteCommand -ScriptBlock { Write-Host "test..." }    
           break
       }
       catch {
        
       }
   }
   Start-Sleep -Seconds 5
   Write-Host "Restarted"
}



function CopyFile {
    param (
        [string]$FilePath
    )

    $spass = (ConvertTo-SecureString -String $password -AsPlainText -Force)
    $credentialObject = New-Object System.Management.Automation.PSCredential ($user, $spass)

    $session = New-PSSession -ComputerName $serverIp -Credential $credentialObject

    $fileName = [System.IO.Path]::GetFileName($FilePath)
    $remoteScriptPath = "C:\$fileName"
    Copy-Item -Path $FilePath -Destination $remoteScriptPath -ToSession $session -Force
    Start-Sleep -Seconds 1
}

function Invoke-RemoteCommand {
    param (
        [string]$ScriptBlock,
        [array]$Arguments = @(),
        [int]$TimeoutSeconds = 20
    )

    $job = Start-Job -ScriptBlock {
        param ($serverIp, $user, $password, $ScriptBlock, $Arguments)

        $securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force
        $credential = New-Object System.Management.Automation.PSCredential ($user, $securePassword)
        $session = New-PSSession -ComputerName $serverIp -Credential $credential

        Invoke-Command -Session $session -ScriptBlock ([scriptblock]::Create($ScriptBlock)) -ArgumentList $Arguments -Verbose

        Remove-PSSession $session
    } -ArgumentList $serverIp, $user, $password, $ScriptBlock, $Arguments

    $startTime = Get-Date

    while ($true) {
        Receive-Job -Job $job -Keep | Write-Host
        if ($job.State -ne 'Running') { break }
        if ((Get-Date) -gt $startTime.AddSeconds($TimeoutSeconds)) {
            Write-Warning "Timeout reached after $TimeoutSeconds seconds. Stopping job."
            Stop-Job -Job $job -Force
            Remove-Job -Job $job
            throw "Remote command timed out after $TimeoutSeconds seconds."
        }
        Start-Sleep -Milliseconds 500
    }

    $result = Receive-Job -Job $job
    Remove-Job -Job $job
    return $result
}

function Invoke-RemoteFile {
    param (
        [string]$FilePath          # Local PowerShell script path (.ps1)
    )
    WaitRestart
   Write-Host "Invoke-RemoteFile $filePath .."
    try {
        $securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force
        $credential = New-Object System.Management.Automation.PSCredential ($user, $securePassword)

        # Extract filename and determine remote execution path
        $RemoteFileName = [System.IO.Path]::GetFileName($FilePath)
        $remoteScriptPath = "C:\$RemoteFileName"

        # Start a remote session
        $session = New-PSSession -ComputerName $serverIp -Credential $credential

        # Copy the script to the remote server
        Copy-Item -Path $FilePath -Destination $remoteScriptPath -ToSession $session -Force

        # Execute the PowerShell script remotely
        Invoke-Command -Session $session -ScriptBlock {
            param ($remotePath)

            # Run PowerShell script on the remote server
            & "powershell.exe" -ExecutionPolicy Bypass -File $remotePath
        } -ArgumentList $remoteScriptPath

        # Clean up session
        Remove-PSSession -Session $session
    } catch {
        throw "Error executing remote PowerShell script: $_"
    }
    Start-Sleep -Seconds 1
    Write-Host "Invoke-RemoteFile Complete $filePath .."
}

function Enable-Remote2 {
    try 
    {
        Invoke-RemoteCommand -ScriptBlock "Write-Host 'yes'"
    }
    catch 
    {
        UltraRemoteCmd -cmd "Start-Sleep -Seconds 20" -forever $false
        Start-Sleep -Seconds 60
        $cmd = @(
            "Enable-PSRemoting -Force"
            "Set-Service -Name WinRM -StartupType Automatic"
            "New-NetFirewallRule -DisplayName 'Allow WinRM' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5985"
            "Start-Service -Name WinRM"
        )
        $str = $cmd -join "; "
        UltraRemoteCmd -cmd $str -forever $true
    }
    Start-Sleep -Seconds 1
    Write-Host "Enable remote2 compelete"
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
            Start-Sleep -Seconds 3
            break
        }
        catch {
            Write-Host $_
            Start-Sleep -Seconds 3
        }
    }
    Write-Host "SQL SETUPED"
    Start-Sleep -Seconds 1
}

################

AddTrusted -hostname $serverIp
Enable-Remote2

Invoke-RemoteFile -FilePath "install0.ps1"

WaitRestart
CopyFile -FilePath "installSql.ps1"
UltraRemoteCmd -cmd  "powershell.exe -ExecutionPolicy Bypass -File 'C:\installSql.ps1'" -timeout 800 -forever $true
WaitSql

CopyFile -FilePath "install.sql"
Invoke-RemoteFile -FilePath "installSqlTools.ps1"

Invoke-RemoteFile -FilePath "installWeb.ps1"
UltraRemoteCmd -cmd  "powershell.exe -ExecutionPolicy Bypass -File 'C:\installWeb.ps1'" -timeout 800 -forever $true

Invoke-RemoteFile -FilePath "installTrigger.ps1"

WaitRestart
. ".\publish.ps1" -serverIp $serverIp -user $user -password $password -direct $true

Write-Host "----------- THE END --------------"