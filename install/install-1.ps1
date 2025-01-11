$name=$env:COMPUTERNAME

# 🛠 Registry fix: Allow interactive tasks (force RPC to work)
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server" -Name "AllowRemoteRPC" -Value 1


####
$regPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Reliability"
if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }
Set-ItemProperty -Path $regPath -Name ShutdownReasonOn -Value 0 -Type DWord -Force

#firewall (before WinRM: avoids Set-WSManQuickConfig failing when a NIC is "Public")
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

# SkipNetworkProfileCheck: required on client SKUs when any active network is "Public" (WSMan fault 2150859113)
Enable-PSRemoting -Force -SkipNetworkProfileCheck
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

# Optional WinRM hardening; WSMan drive can fault on some profiles—do not abort the script
Start-Sleep -Seconds 2
foreach ($wsmanStep in @(
    { Set-Item -LiteralPath WSMan:\localhost\Service\AllowUnencrypted -Value $true -Force },
    { & winrm set winrm/config/client '@{AllowUnencrypted="true"}' | Out-Null },
    { Set-Item -LiteralPath WSMan:\localhost\Service\Auth\Basic -Value $true -Force },
    { if (-not (Get-NetFirewallRule -Name "WinRM-HTTP-In-TCP" -ErrorAction SilentlyContinue)) { New-NetFirewallRule -Name "WinRM-HTTP-In-TCP" -DisplayName "WinRM (HTTP-In)" -Description "Inbound rule for WinRM (HTTP-In)" -Protocol TCP -LocalPort 5985 -Action Allow } else { Enable-NetFirewallRule -Name "WinRM-HTTP-In-TCP" } },
    { if (-not (Get-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" -ErrorAction SilentlyContinue)) { New-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" -DisplayName "WinRM (HTTPS-In)" -Description "Inbound rule for WinRM (HTTPS-In)" -Protocol TCP -LocalPort 5986 -Action Allow } else { Enable-NetFirewallRule -Name "WinRM-HTTPS-In-TCP" } }
)) {
    try { & $wsmanStep } catch { }
}
