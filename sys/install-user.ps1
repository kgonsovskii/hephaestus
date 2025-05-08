function Setup-UserAndFolder {
    $Username = "rdp"
    $InstallPath = "C:\install"
    $InfoFile = "C:\Windows\info.txt"

    function Get-OrCreatePassword {
        if (Test-Path $InfoFile) {
            return Get-Content $InfoFile -Raw
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
            return $Password
        }
    }

    function Create-UserIfNeeded($Pass) {
        if (Get-LocalUser -Name $Username -ErrorAction SilentlyContinue) {
            Write-Host "User '$Username' already exists."
        } else {
            $SecurePass = ConvertTo-SecureString $Pass -AsPlainText -Force
            New-LocalUser -Name $Username -Password $SecurePass -FullName $Username -Description "RDP user" -PasswordNeverExpires
            Add-LocalGroupMember -Group "Administrators" -Member $Username
            Write-Host "User '$Username' created and added to 'Users' group."
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
    Create-UserIfNeeded -Pass $Password
    Ensure-FolderPermissions
}

# Call the main function
Setup-UserAndFolder