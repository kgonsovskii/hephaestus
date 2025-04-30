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
        Invoke-RemoteCommand -ScriptBlock { Write-Host 'yes' }
    }
    catch 
    {
        Write-Host $_
        $cmd = @(
            "Enable-PSRemoting -Force"
            "Set-Service -Name WinRM -StartupType Automatic"
            "New-NetFirewallRule -DisplayName 'Allow WinRM' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5985"
        )
        foreach  ($c in $cmd)
        {
            UltraRemoteCmd -cmd $c -forever $false
            Start-Sleep -Seconds 1
        }
        Start-Sleep -Seconds 1
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

