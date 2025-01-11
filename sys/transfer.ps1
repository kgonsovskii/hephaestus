param (
    [string]$serverName, [object]$session
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\current.ps1" -serverName $serverName
. ".\lib.ps1"


Add-Type -AssemblyName "System.IO.Compression.FileSystem"
Clear-Folder -FolderPath "C:\_publish2\"
Copy-Folder -SourcePath $server.certDir -DestinationPath "C:\_publish2\local\cert"
Copy-Folder -SourcePath $server.userDataDir -DestinationPath "C:\_publish2\local\data\$serverName"
Compress-FolderToZip -SourceFolder "C:\_publish2\local" -targetZipFile "C:\_publish2\local.zip"

Invoke-Command -Session $session -ScriptBlock {
    if (-not (Test-Path "C:\_publish2\"))
    {
        New-Item -Path "C:\_publish2" -ItemType Directory -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path "C:\_publish2\local2.zip")
    {
        Remove-Item -Path "C:\_publish2\local2.zip"
        Remove-Item -Path "C:\_publish2\extracted" -Force -Recurse
    }
}
Copy-Item -Path "C:\_publish2\local.zip" -Destination "C:\_publish2\local2.zip" -ToSession $session -Force 
Invoke-Command -Session $session -ScriptBlock {
    param ([string]$serverName)
    
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
                            Write-Output "Extracting file: $($entry.FullName) to $entryDestinationPath"
                            $entryStream = $entry.Open()
                            $fileStream = [System.IO.File]::Create($entryDestinationPath)
            
                            try {
                                $entryStream.CopyTo($fileStream)
                                $fileStream.Close()  # Close the file stream explicitly
                                Write-Output "File extracted: $entryDestinationPath"
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

    
    Write-Host "remotes- $serverName"
    try {
        Add-Type -AssemblyName "System.IO.Compression.FileSystem"
    }
    catch {
    }

    Clear-Folder "C:\_publish2\extracted"
    Extract-ZipFile -zipFilePath "C:\_publish2\local2.zip" -destinationPath "C:\_publish2\extracted"

    Copy-Folder -SourcePath "C:\_publish2\extracted\local\cert" -DestinationPath "C:\inetpub\wwwroot\cert" -Clear $false

    Copy-Folder -SourcePath "C:\_publish2\extracted\local\data\$serverName" -DestinationPath "C:\data\$serverName" -Clear $false

} -ArgumentList $serverName

