Install-Module -Name ps2exe  -Scope AllUsers
Install-Module -Name PSPKI -Scope AllUsers
Install-WindowsFeature -Name Web-Server -IncludeManagementTools

Install-WindowsFeature FS-SMB1
Install-WindowsFeature FS-SMB2
Set-SmbServerConfiguration -EnableSMB1Protocol $true
Set-SmbServerConfiguration -EnableSMB2Protocol $true
New-NetFirewallRule -DisplayName "Allow SMB1 and SMB2" -Direction Inbound -Protocol TCP -LocalPort 445,139 -Action Allow -Profile Any
