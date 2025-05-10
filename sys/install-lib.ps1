param (
    [string]$serverName = 'default'
)

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