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

    # Always use the current directory as base if no path is included
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
        Invoke-RemoteCommand -ScriptBlock "Write-Host 'yes'"
    }
    catch 
    {
        UltraRemoteCmd -cmd "Start-Sleep -Seconds 1" -forever $false
        Start-Sleep -Seconds 1
        $cmd = @(
            "Enable-PSRemoting -Force"
            "Set-Service -Name WinRM -StartupType Automatic"
            "New-NetFirewallRule -DisplayName 'Allow WinRM' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5985"
            "Start-Service -Name WinRM"
        )
        foreach  ($c in $cmd)
        {
            UltraRemoteCmd -cmd $c -forever $false
            Start-Sleep -Seconds 1
        }
    }
    Write-Host "Enable remote2 compelete"
    Start-Sleep -Seconds 1
}

################

AddTrusted -hostname $serverIp

WaitRestart -once $true

Enable-Remote2

CopyItems -FileMask "install*.*"

WaitRestart  -once $false