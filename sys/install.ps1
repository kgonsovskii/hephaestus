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




function UltraRemoteCmd {
    param (
        [string]$cmd,
        [int]$timeout = 60
    )
    Write-Host "UltraRemoteCmd $cmd ..."
    $programPath = sharpRdp
    if (-not (Test-Path $programPath -PathType Leaf)) {
        throw "File not found: $programPath"
    }
    $tag = Get-Date -Format "yyyyMMdd-HHmmssfff"
    & $programPath --server=$serverIp --username=$user --password=$password --command=$cmd --tag=$tag --timeout=$timeout
    Write-Host "UltraRemoteCmd complete $cmd. Awaiting tag..."
    $result = WaitForLocalTag -tag $tag -timeout $timeout
    if ($result -eq -1)
    {
        Write-Host "UltraRemoteCmd TimeOut $cmd ..."
        Start-Sleep 1
        UltraRemoteCmd -cmd $cmd -timeout $timeout
    }    
    Write-Host "UltraRemoteCmd complete $cmd. And Tag."
}

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

    foreach ($file in $files) {
        $remotePath = "C:\$($file.Name)"
        Copy-Item -Path $file.FullName -Destination $remotePath -ToSession $session -Force
    }

    Start-Sleep -Seconds 1

    Remove-PSSession $session
}

function Enable-Remote2 {
    try 
    {
        Invoke-RemoteCommand -ScriptBlock { Write-Host 'yes' }
    }
    catch 
    {
        Write-Host $_
        $cmd = @(
            "Enable-PSRemoting -Force"
            "Enable-PSRemoting -Force"
            "Set-Service -Name WinRM -StartupType Automatic"
            "New-NetFirewallRule -DisplayName 'Allow WinRM' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5985"
            "Start-Service -Name WinRM"
        )
        foreach  ($c in $cmd)
        {
            Start-Sleep -Seconds 1
            UltraRemoteCmd -cmd $c
            Start-Sleep -Seconds 3
        }
        Start-Sleep -Seconds 3
        WaitRestart
    }
    Write-Host "Enable remote2 compelete"
    Start-Sleep -Seconds 1
}

################

AddTrusted -hostname $serverIp

WaitRestart -once $true

Enable-Remote2

CopyItems -FileMask "install*.*"

. ".\install-remote.ps1" -serverName $serverName