param (
    [string]$serverName = 'default'
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

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"
. ".\current.ps1" -serverName $serverName

$password = $server.clone.clonePassword
$user=$server.clone.cloneUser
$serverIp = $server.clone.cloneServerIp

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

function psExec {
    $programPath = Join-Path $scriptDir "../rdp/PsExec64.exe"
    $resolvedPath = Resolve-Path $programPath -ErrorAction SilentlyContinue
    if (-not $resolvedPath) {
        $programPath = Join-Path $scriptDir "../cp/PsExec64.exe"
        $resolvedPath = Resolve-Path $programPath -ErrorAction SilentlyContinue
    }    
    Write-Host $programPath
    return $programPath
}

function Test {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect($serverIp, 5985, $null, $null)
        $wait = $async.AsyncWaitHandle.WaitOne(4000, $false) # 4000 ms = 3 seconds timeout
        if ($wait -and $client.Connected) {
            $client.EndConnect($async)
            return $true
        } else {
            return $false
        }
    } catch {
        return $false
    } finally {
        $client.Close()
    }
}


# Global config
$Global:RdpUser = "rdp"
$Global:RdpPassword = (Get-Content "C:\Windows\info.txt" -Raw).Trim()
$Global:PsExecPath = psExec
$Global:SessionCache = $null
$Global:RdpFilePath = "$env:TEMP\rdp_auto.rdp"
$Global:CredTarget = "localhost"

function Get-SessionId {
    if ($Global:SessionCache) {
        return $Global:SessionCache
    }

    # Step 1: Try to find existing session for rdp
    $quserOutput = quser 2>$null
    $sessions = @()
    foreach ($line in $quserOutput) {
        $parts = ($line -replace '\s{2,}', '|').Split('|')
        if ($parts.Count -ge 3) {
            $sessions += [PSCustomObject]@{
                Username   = $parts[0].Trim()
                SessionId  = $parts[2].Trim()
                State      = $parts[3].Trim()
            }
        }
    }

    $targetSession = $sessions | Where-Object { $_.Username -eq $Global:RdpUser -and $_.State -eq "Active" }
    if ($targetSession) {
        $Global:SessionCache = $targetSession.SessionId
        return $Global:SessionCache
    }

    Write-Host "No active session for '$($Global:RdpUser)', creating real RDP session to localhost..."

    # Step 2: Store credentials
    cmdkey /generic:TERMSRV/$Global:CredTarget /user:$Global:RdpUser /pass:$Global:RdpPassword | Out-Null

    # Step 3: Create a temp .rdp file
@"
screen mode id:i:1
use multimon:i:0
session bpp:i:32
compression:i:1
keyboardhook:i:2
audiocapturemode:i:0
videoplaybackmode:i:1
connection type:i:2
networkautodetect:i:1
bandwidthautodetect:i:1
displayconnectionbar:i:0
enableworkspacereconnect:i:0
disable wallpaper:i:1
allow font smoothing:i:0
allow desktop composition:i:0
disable full window drag:i:1
disable menu anims:i:1
disable themes:i:1
disable cursor setting:i:1
bitmapcachepersistenable:i:1
full address:s:127.0.0.1
username:s:$Global:RdpUser
authentication level:i:0
prompt for credentials:i:0
negotiate security layer:i:1
enablecredssupport:i:1
trustprompt:i:0
remoteapplicationmode:i:0
alternate shell:s:
shell working directory:s:
gatewayhostname:s:
gatewayusagemethod:i:4
gatewaycredentialssource:i:4
gatewayprofileusagemethod:i:0
drivestoredirect:s:
"@ | Set-Content $Global:RdpFilePath -Encoding ASCII

    # Step 4: Launch mstsc silently
    Start-Process "mstsc.exe" -ArgumentList "$Global:RdpFilePath" -WindowStyle Hidden
    Start-Sleep -Seconds 5

    # Step 5: Re-check sessions
    $quserOutput = quser 2>$null
    $sessions = @()
    foreach ($line in $quserOutput) {
        $parts = ($line -replace '\s{2,}', '|').Split('|')
        if ($parts.Count -ge 3) {
            $sessions += [PSCustomObject]@{
                Username   = $parts[0].Trim()
                SessionId  = $parts[2].Trim()
                State      = $parts[3].Trim()
            }
        }
    }

    $targetSession = $sessions | Where-Object { $_.Username -eq $Global:RdpUser -and $_.State -eq "Active" }
    if ($targetSession) {
        $Global:SessionCache = $targetSession.SessionId
        return $Global:SessionCache
    }

    throw "Failed to create RDP session for '$($Global:RdpUser)'."
}



function Run-ProgramAsUser {
    param (
        [string]$programPath,
        [string]$arguments = "",
        [int]$timeout = 0
    )

    $local_user = "rdp"
    $local_password = (Get-Content "C:\Windows\info.txt" -Raw).Trim()

    $psexecPath = psExec

    $sessionId = Get-SessionId

    Write-Host "Running command: $fullCommand"

    $psexecArgs = "-i $sessionId -u $local_user -p $local_password `"$programPath`" $arguments"

    # Set up ProcessStartInfo
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $psExecPath
    $psi.Arguments = $psexecArgs
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $process = [System.Diagnostics.Process]::Start($psi)

    if ($timeout -gt 0) {
        if (-not $process.WaitForExit($timeout * 1000)) {
            Write-Host "Timeout reached. Killing PsExec process..."
            $process.Kill()
        }
    } else {
        $process.WaitForExit()
    }

    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()

    if ($stdout) { Write-Host $stdout }
    if ($stderr) { Write-Host "ERROR: $stderr" }

    $process.WaitForExit()
}



function Invoke-RemoteCommand {
    param (
        [string]$ScriptBlock,
        [array]$Arguments = @(),
        [int]$TimeoutSeconds = 17
    )

    $securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force
    $credential = New-Object System.Management.Automation.PSCredential ($user, $securePassword)
    $session = New-PSSession -ComputerName $serverIp -Credential $credential

    $jobResult = Invoke-Command -Session $session -ScriptBlock {
        param ($ScriptBlockText, $Arguments)
        $ScriptBlockObject = [ScriptBlock]::Create($ScriptBlockText)
        & $ScriptBlockObject @Arguments
    } -ArgumentList $ScriptBlock, $Arguments

    Remove-PSSession -Session $session

    return $jobResult
}
function WaitForLocalTag {
    param (
        [string]$tag,
        [int]$timeout
    )

    Write-Host "Waiting for local tag $tag ..."
    Set-Content -Path 'C:\install\tag_local_r.txt' -Value $tag
    $filePath = "C:\install\tag_local.txt"
    $startTime = Get-Date

    function IsTag() {
        if (Test-Path $filePath) {
            $content = Get-Content -Path $filePath -Raw                            
            $co = $content -like "*$tag*"
            if ($co -eq $false) {
                return 1
            }
            $co = $content -like "*timeout*"
            if ($co) {
                return -1
            } else {
                return 0
            }
        } else {
            return 1
        }
    }

    while ($true) {
        $elapsed = (Get-Date) - $startTime
        if ($elapsed.TotalSeconds -ge $timeout) {
            Write-Host "Timeout reached after $timeout seconds."
            return -1
        }

        $result = IsTag
        if ($result -eq 0 -or $result -eq -1) {
            Write-Host "Tag local '$tag' detected!"
            return $result
        }
        Start-Sleep -Seconds 1
    }
}


function WaitForTag {
    param (
        [string]$tag,
        [int]$timeout
    )
    $result = 0
    $startTime = Get-Date

    while ($true) {
        Start-Sleep -Seconds 1

        $elapsed = (Get-Date) - $startTime
        if ($elapsed.TotalSeconds -ge $timeout) {
            Write-Host "Timeout reached before connection to server."
            return -1
        }

        try {
            $tested = Test
            if ($tested -eq $false)
            {
                Start-Sleep -Seconds 1
                continue
            } 

           $result = Invoke-RemoteCommand -ScriptBlock {
                param (
                    [string]$tag,
                    [int]$timeout
                )
                Write-Host "Waiting for tag $tag ..."
                Set-Content -Path 'C:\install\tagR.txt' -Value $tag
                $filePath = "C:\install\tag.txt"
                $startTime = Get-Date

                function IsTag() {
                    if (Test-Path $filePath) {
                        $content = Get-Content -Path $filePath -Raw
                        $co = $content -like "*$tag*"
                        if ($co -eq $false) {
                            return 1
                        }
                        $co = $content -like "*timeout*"
                        if ($co) {
                            return -1
                        } else {
                            return 0
                        }
                    } else {
                        return 1
                    }
                }

                while ($true) {
                    $elapsed = (Get-Date) - $startTime
                    if ($elapsed.TotalSeconds -ge $timeout) {
                        Write-Host "Timeout reached waiting for tag '$tag'."
                        return -1
                    }

                    $result = IsTag
                    if ($result -eq 0 -or $result -eq -1) {
                        Write-Host "Tag '$tag' detected!"
                        return $result
                    }
                    Start-Sleep -Seconds 3
                }

            } -Arguments @($tag, $timeout)

            break
        }
        catch {
            Write-Host $_
            Start-Sleep -Seconds 3
        }

        Start-Sleep -Seconds 1
    }
    return $result
}

function WaitRestart {
    param (
        [bool]$once=$false
    )
    Write-Host "Restarting.."
   while ($true) {
        $tested = Test
        if ($tested -eq $false)
        {
            if ($once)
            {
                return
            }
            Start-Sleep -Seconds 1    
            continue
        }
       try {
           Invoke-RemoteCommand -ScriptBlock { shutdown /r /t 0 /f }
           Start-Sleep -Seconds 3
           break
       }
       catch { 
        if ($once)
        {
            return
        }
        Write-Host $_
        Start-Sleep -Seconds 3    
       }
       Write-Host "Restarting attempt.."
   }

   while ($true) 
   {
        $tested = Test
        if ($tested -eq $true)
        {
            break
        }
        Write-Host "Ping attempt.."
        Start-Sleep -Seconds 2
    }
    Start-Sleep -Seconds 1
   
    while ($true) {
       Start-Sleep -Seconds 1
       try {
           Write-Host "check restarted..."
           Invoke-RemoteCommand -ScriptBlock { Write-Host "checking..." }    
           break
       }
       catch {
        
       }
   }
   Start-Sleep -Seconds 2
   Write-Host "Restarted"
}