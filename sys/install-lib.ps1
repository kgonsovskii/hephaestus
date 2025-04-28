param (
    [string]$serverName
)

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
    & $programPath --server=$serverIp --username=$user --password=$password --command=$cmd\ 
    Write-Host "UltraRemoteCmd complete $cmd."
    if ($forever -eq $true)
    {
        WaitForTag -tag $tag
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
                Write-Host "Waiting for tag $tag ..."
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
                        Write-Host "Tag '$tag' detected!"
                        break
                    }
                    Start-Sleep -Seconds 3
                }
        
            } -Arguments @($tag)   
           break
       }
       catch {
          Write-Host $_
          Start-Sleep -Seconds 3
       }
       Start-Sleep -Seconds 1
   }
   Start-Sleep -Seconds 1
   Write-Host "Tag found"
}

function WaitRestart {
    param (
        [bool]$once=$false
    )
    Write-Host "Restarting.."
   while ($true) {
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
        if (Test-Connection -ComputerName $serverIp -Count 1 -Quiet) {
            break
        }
        Write-Host "Ping attempt.."
        Start-Sleep -Seconds 3
    }
    Start-Sleep -Seconds 3
   
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