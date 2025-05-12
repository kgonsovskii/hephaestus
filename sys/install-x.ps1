# 🛠 Registry fix: Allow interactive tasks (force RPC to work)
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name "AllowRemoteRPC" -Value 1


# Disable UAC for Admins (Optional, lowers security)
Write-Host "Disabling UAC enforcement (EnableLUA = 0)..."
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" `
    -Name "EnableLUA" -Value 0 -Force

Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" `
    -Name "ConsentPromptBehaviorAdmin" -Value 0 -Force

Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" `
    -Name "PromptOnSecureDesktop" -Value 0 -Force

Write-Host "UAC registry settings applied. A reboot is required to fully disable UAC."


####
$regPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Reliability"
if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }
Set-ItemProperty -Path $regPath -Name ShutdownReasonOn -Value 0 -Type DWord -Force

Enable-PSRemoting -Force
Set-Service -Name WinRM -StartupType Automatic
Start-Service -Name WinRM

$psVer = $PSVersionTable.PSVersion.Major
Write-Host "PowerShell v: $psVer"

try {
    Set-ExecutionPolicy Unrestricted -Scope LocalMachine -Force
    Set-ExecutionPolicy Unrestricted -Scope CurrentUser -Force
}
catch {

}



#firewall
function Update-FirewallRule {
    param (
        [string]$Name,
        [string]$DisplayName,
        [string]$Description,
        [int]$LocalPort,
        [string]$Protocol,
        [string]$Profile = 'Any',
        [string]$RemoteAddress = 'Any',
        [string]$Program = 'Any'
    )
    try {
        $existingRule = Get-NetFirewallRule -Name $Name -ErrorAction Stop
        Set-NetFirewallRule -Name $Name -Profile $Profile -RemoteAddress $RemoteAddress -Program $Program
        Enable-NetFirewallRule -Name $Name
        Write-Output "Rule '$Name' updated."
    }
    catch {
        New-NetFirewallRule -Name $Name -DisplayName $DisplayName -Description $Description -Protocol $Protocol -LocalPort $LocalPort -Action Allow -Profile $Profile -RemoteAddress $RemoteAddress -Program $Program
        Write-Output "Rule '$Name' created."
    }
}
Update-FirewallRule -Name "WinRM-HTTP-In-TCP" -DisplayName "WinRM (HTTP-In)" -Description "Inbound rule for WinRM (HTTP-In)" -Protocol TCP -LocalPort 5985
Update-FirewallRule -Name "WinRM-HTTPS-In-TCP" -DisplayName "WinRM (HTTPS-In)" -Description "Inbound rule for WinRM (HTTPS-In)" -Protocol TCP -LocalPort 5986
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name "LocalAccountTokenFilterPolicy" -Value 1 -Type DWORD -Force


try
{
set-item -force WSMan:\localhost\Service\AllowUnencrypted $true
winrm set winrm/config/client '@{AllowUnencrypted="true"}'
Set-Item -Path WSMan:\localhost\Service\Auth\Basic -Value $true
Set-Item -Path WSMan:\localhost\Service\AllowUnencrypted -Value $true
if (-not (Get-NetFirewallRule -Name "WinRM-HTTP-In-TCP" -ErrorAction SilentlyContinue)) { New-NetFirewallRule -Name "WinRM-HTTP-In-TCP" -DisplayName "WinRM (HTTP-In)" -Description "Inbound rule for WinRM (HTTP-In)" -Protocol TCP -LocalPort 5985 -Action Allow } else { Enable-NetFirewallRule -Name "WinRM-HTTP-In-TCP" }
if (-not (Get-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" -ErrorAction SilentlyContinue)) { New-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" -DisplayName "WinRM (HTTPS-In)" -Description "Inbound rule for WinRM (HTTPS-In)" -Protocol TCP -LocalPort 5986 -Action Allow } else { Enable-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" }

}
catch{
    Write-Host $_
}


##base folders
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

    function Create-UserIfNeeded {
        param (
            [string]$Username,
            [string]$Pass
        )
    
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
    
        # Enable auto-login
        $regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
        Set-ItemProperty -Path $regPath -Name "AutoAdminLogon" -Value "1"
        Set-ItemProperty -Path $regPath -Name "DefaultUserName" -Value $Username
        Set-ItemProperty -Path $regPath -Name "DefaultPassword" -Value $Pass
        Set-ItemProperty -Path $regPath -Name "DefaultDomainName" -Value $env:COMPUTERNAME
    
        Write-Host "Auto-login configured for user '$Username'."
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
Setup-UserAndFolder




