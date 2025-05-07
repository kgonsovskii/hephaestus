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
    Set-Content -Path 'C:\tag_local_r.txt' -Value $tag
    $filePath = "C:\tag_local.txt"
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
                Set-Content -Path 'C:\tagR.txt' -Value $tag
                $filePath = "C:\tag.txt"
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