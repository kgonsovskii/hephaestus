param (
    [string]$serverName, [string]$reboot="true"
)

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"

if ($serverName -eq "") {
    throw "No server"
} 

. ".\current.ps1" -serverName $serverName
. ".\install-lib.ps1" -serverName $serverName

$password = $server.clone.clonePassword
$user=$server.clone.cloneUser
$serverIp = $server.clone.cloneServerIp

Write-Host "Install-Pre $serverName, serverIp $serverIp, rebooting $reboot"


if ([string]::IsNullOrEmpty($serverIp))
{
    throw "No Server Ip defined"
}

function Setup-UserAndFolder {
    $Username = "rdp"
    $InstallPath = "C:\install"
    $InfoFile = "C:\Windows\info.txt"

    function Get-OrCreatePassword {
        if (Test-Path $InfoFile) {
            return (Get-Content $InfoFile -Raw).Trim()
        } else {
            $Chars = @{
                Upper  = [char[]](65..90)          # A-Z
                Lower  = [char[]](97..122)         # a-z
                Digit  = [char[]](48..57)          # 0-9
                Symbol = [char[]]'!'               # only !
            }
            
            $All = $Chars.Upper + $Chars.Lower + $Chars.Digit + $Chars.Symbol
            
            $PasswordArray = @(
                Get-Random -InputObject $Chars.Upper
                Get-Random -InputObject $Chars.Lower
                Get-Random -InputObject $Chars.Digit
                Get-Random -InputObject $Chars.Symbol
            ) + (Get-Random -InputObject $All -Count 6)
            
            $Password = -join ($PasswordArray | Sort-Object { Get-Random })

            if (-not (Test-Path $InstallPath)) {
                New-Item -ItemType Directory -Path $InstallPath | Out-Null
            }

            Set-Content -Path $InfoFile -Value $Password
            return $Password.Trim()
        }
    }

    function Create-UserIfNeeded($Username, $Pass) {
        $SecurePass = ConvertTo-SecureString $Pass -AsPlainText -Force
    
        if (Get-LocalUser -Name $Username -ErrorAction SilentlyContinue) {
            Write-Host "User '$Username' already exists."
            $cred = New-Object System.Management.Automation.PSCredential($Username, $SecurePass)
            try {
                Start-Process -FilePath "cmd.exe" -Credential $cred -ArgumentList "/c exit" -NoNewWindow -Wait -ErrorAction Stop
                Write-Host "Password is correct. No change needed."
            } catch {
                try {
                    Set-LocalUser -Name $Username -Password $SecurePass
                    Write-Host "Password for '$Username' has been updated."
                } catch {
                    Write-Host "Failed to update password for '$Username': $_"
                }
            }
        } else {
            New-LocalUser -Name $Username -Password $SecurePass -FullName $Username -Description "RDP user" -PasswordNeverExpires
            Add-LocalGroupMember -Group "Administrators" -Member $Username
            Write-Host "User '$Username' created and added to 'Administrators' group."
        }
    }

    function Ensure-FolderPermissions {
        if (-Not (Test-Path $InstallPath)) {
            New-Item -ItemType Directory -Path $InstallPath | Out-Null
            Write-Host "Folder '$InstallPath' created."
        } else {
            Write-Host "Folder '$InstallPath' already exists."
        }

        $Acl = Get-Acl $InstallPath
        $AccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            "Everyone", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
        )
        $Acl.SetAccessRule($AccessRule)
        Set-Acl -Path $InstallPath -AclObject $Acl
        Write-Host "Permissions for '$InstallPath' set to FullControl for Everyone."
    }

    # Main execution
    $Password = Get-OrCreatePassword
    Create-UserIfNeeded -Username $Username -Pass $Password
    Ensure-FolderPermissions
    $InstallPath = "C:\data"
    Ensure-FolderPermissions
    $InstallPath = "C:\inetpub\wwwroot"
    Ensure-FolderPermissions
    $InstallPath = "C:\soft\hephaestus"
    Ensure-FolderPermissions
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

Setup-UserAndFolder
AddTrusted -hostname $serverIp

if ($reboot -eq "true")
{
    WaitRestart -once $true
}