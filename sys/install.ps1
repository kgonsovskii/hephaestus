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
        [string]$cmd
    )
    $programPath = sharpRdp

    if (-not (Test-Path $programPath -PathType Leaf)) {
        throw "File not found: $programPath"
    }
    
    # Start the remote command as a background job
    $job = Start-Job -ScriptBlock {
        param($programPath, $serverIp, $user, $password, $cmd)
        & $programPath --server=$serverIp --username=$user --password=$password --command=$cmd
    } -ArgumentList $programPath, $serverIp, $user, $password, $cmd

    # Wait for up to 5 minutes
    $job | Wait-Job -Timeout 60

    # If the job is still running, kill it
    if ($job.State -eq "Running") {
        Stop-Job $job
        Write-Host "Command timed out and was stopped."
    }

    # Cleanup the job
    Remove-Job $job

    # Wait before the next attempt
    Start-Sleep -Seconds 5
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
        [int]$TimeoutSeconds = 20  # Default timeout (60 seconds)
    )

    try {
        $securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force
        $credential = New-Object System.Management.Automation.PSCredential ($user, $securePassword)

        $runspace = [runspacefactory]::CreateRunspace()
        $runspace.Open()
        $powershell = [powershell]::Create().AddScript({
            param ($ServerIp, $Credential, $ScriptBlock, $Arguments)
            
            $session = New-PSSession -ComputerName $serverIp -Credential $Credential
            
            try {
                $result = Invoke-Command -Session $session -ScriptBlock ([scriptblock]::Create($ScriptBlock)) -ArgumentList $Arguments
            } finally {
                Remove-PSSession -Session $session
            }

            return $result
        }).AddArgument($ServerIp).AddArgument($credential).AddArgument($ScriptBlock).AddArgument($Arguments)

        $powershell.Runspace = $runspace
        $handle = $powershell.BeginInvoke()

        if ($handle.AsyncWaitHandle.WaitOne($TimeoutSeconds * 1000)) {
            $result = $powershell.EndInvoke($handle)
        } else {
            $powershell.Stop()
            throw "The remote command timed out after $TimeoutSeconds seconds."
        }
    } catch {
        throw "Error executing remote command: $_"
    } finally {
        $powershell.Dispose()
        $runspace.Close()
        $runspace.Dispose()
    }

    Start-Sleep -Seconds 1
    return $result
}

function Invoke-RemoteFile {
    param (
        [string]$FilePath          # Local PowerShell script path (.ps1)
    )
    WaitRestart

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
}

function Enable-Remote2 {
    try 
    {
        Invoke-RemoteCommand -ScriptBlock "Write-Host 'yes'"
    }
    catch 
    {
        $cmd = @(
            "Enable-PSRemoting -Force"
            "Set-Service -Name WinRM -StartupType Automatic"
            "Start-Service -Name WinRM"
            "New-NetFirewallRule -DisplayName 'Allow WinRM' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5985"
        )
        $str = $cmd -join "; "
        UltraRemoteCmd -cmd $str
        Start-Sleep -Seconds 35
    }
    Start-Sleep -Seconds 1

}

function WaitRestart {
    Invoke-RemoteCommand -ScriptBlock { Restart-Computer -Force }
    Start-Sleep -Seconds 3
    
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
    Start-Sleep -Seconds 1
    Write-Host "Restarted"
}

################

AddTrusted -hostname $serverIp
Enable-Remote2

Invoke-RemoteFile -FilePath "install0.ps1"
Invoke-RemoteFile -FilePath "install1.ps1"
CopyFile -FilePath "install.sql"
Invoke-RemoteFile -FilePath "install2.ps1"
Invoke-RemoteFile -FilePath "install3.ps1"

WaitRestart
. ".\publish.ps1" -serverIp $serverIp -user $user -password $password -direct $true

Write-Host "----------- THE END --------------"