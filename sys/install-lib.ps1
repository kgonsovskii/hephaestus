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
        [int]$TimeoutSeconds = 20
    )
    $waitScriptPath = Join-Path -Path $scriptDir -ChildPath "wait.ps1"
    $tempScriptPath = "C:\temp.ps1"
    $outFile = "C:\out.log"
    $errFile = "C:\err.log"

    if (-not (Test-Path $waitScriptPath)) {
        throw "Required script 'wait.ps1' not found in current directory."
    }

    # Create local temp.ps1 file from ScriptBlock
    Set-Content -Path $tempScriptPath -Value $ScriptBlock -Encoding UTF8

    $startArgs = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", "`"$waitScriptPath`"",
        "-serverIp", "`"$serverIp`"",
        "-user", "`"$user`"",
        "-password", "`"$password`"",
        "-tempScriptPath", "`"$tempScriptPath`"",
        "-remoteScriptPath", "`"C:\temp.ps1`"",
        "-arguments", "`"$($Arguments -join ',')`""
    ) -join ' '

    try {
        $process = Start-Process -FilePath "powershell.exe" `
                                 -ArgumentList $startArgs `
                                 -RedirectStandardOutput $outFile `
                                 -RedirectStandardError $errFile `
                                 -PassThru -NoNewWindow

        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            try { $process | Stop-Process -Force } catch {}
            throw "Invoke-Remote timeOut: $ScriptBlock"
        }

        Start-Sleep -Milliseconds 200

        if (Test-Path $outFile) {
            [System.IO.File]::ReadAllLines($outFile) | ForEach-Object { Write-Host $_ }
        }

        if (Test-Path $errFile) {
            [System.IO.File]::ReadAllLines($errFile) | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        }
    }
    finally {
        Start-Sleep -Milliseconds 100
        if (Test-Path $tempScriptPath) { Remove-Item $tempScriptPath -Force -ErrorAction SilentlyContinue }
        if (Test-Path $outFile) { Remove-Item $outFile -Force -ErrorAction SilentlyContinue }
        if (Test-Path $errFile) { Remove-Item $errFile -Force -ErrorAction SilentlyContinue }
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
        [bool]$once
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
        Start-Sleep -Seconds 3
       }
       Write-Host "Restarting attempt.."
   }
   
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