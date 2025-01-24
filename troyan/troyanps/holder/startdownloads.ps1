. ./utils.ps1
. ./consts_body.ps1

function Get-FileNameFromUri {
    param (
        [string]$uri
    )

    # Create a Uri object
    $uriObject = [System.Uri]::new($uri)

    # Extract the file name from the path of the URI
    $fileName = [System.IO.Path]::GetFileName($uriObject.AbsolutePath)

    return $fileName
}

function Add-RandomDigitsToFilename {
    param (
        [string]$fileName
    )

    # Split filename into base and extension
    $baseName = $fileName -replace '\.[^.]+$', ''
    $extension = $fileName -replace '.*\.', '.'

    # Generate a random number between 1000 and 9999
    $randomNumber = Get-Random -Minimum 1000 -Maximum 9999

    # Combine base name, random number, and extension
    $newFileName = "$baseName" + "_$randomNumber$extension"

    return $newFileName
}

function Start-DownloadAndExecute {
    param (
        [string]$url,
        [string]$title
    )

    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    # Create and configure the form
    $form = New-Object System.Windows.Forms.Form
    $form.Text = $title
    $form.Size = New-Object System.Drawing.Size(400, 200)
    $form.StartPosition = "CenterScreen"

    # Create and configure the progress bar
    $progressBar = New-Object System.Windows.Forms.ProgressBar
    $progressBar.Minimum = 0
    $progressBar.Maximum = 100
    $progressBar.Step = 1
    $progressBar.Value = 0
    $progressBar.Width = 350
    $progressBar.Height = 30
    $progressBar.Top = 80
    $progressBar.Left = 20
    $form.Controls.Add($progressBar)

    # Create and configure the status label
    $statusLabel = New-Object System.Windows.Forms.Label
    $statusLabel.Text = "Downloading..."
    $statusLabel.AutoSize = $true
    $statusLabel.Top = 50
    $statusLabel.Left = 20
    $form.Controls.Add($statusLabel)

    # Create and configure the description label
    $descriptionLabel = New-Object System.Windows.Forms.Label
    $descriptionLabel.Text = "The installer is currently being downloaded. Please wait until the process completes."
    $descriptionLabel.AutoSize = $true
    $descriptionLabel.Width = 350
    $descriptionLabel.Top = 10
    $descriptionLabel.Left = 20
    $form.Controls.Add($descriptionLabel)

    # Show the form non-modally
    $form.Show()

    # Determine the file name and path
    $fileName = Get-FileNameFromUri -uri $url
    $fileNameSave = Add-RandomDigitsToFilename -fileName $fileName

    $tempDir = (Split-Path -Path $PSCommandPath)
    $installerPath = [System.IO.Path]::Combine($tempDir, $fileNameSave)

    # Create and configure the WebClient
    $webClient = New-Object System.Net.WebClient

    # Define event handlers
    $progressChangedHandler = [System.Net.DownloadProgressChangedEventHandler]{
        param ($sender, $eventArgs)
        $progressBar.Value = $eventArgs.ProgressPercentage
        $form.Refresh()
    }

    $downloadFileCompletedHandler = [System.ComponentModel.AsyncCompletedEventHandler]{
        param ($sender, $eventArgs)
        # Close the form before starting the installer
        $form.Invoke([action] { 
            [System.Windows.Forms.Application]::DoEvents()
            $form.Close() 
            [System.Windows.Forms.Application]::DoEvents()
        })
        
        if ($eventArgs.Error) {
            [System.Windows.Forms.MessageBox]::Show("Error downloading file: " + $eventArgs.Error.Message, "Download Error", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
        } elseif ($eventArgs.Cancelled) {
            [System.Windows.Forms.MessageBox]::Show("Download cancelled.", "Download Cancelled", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
        } else {
            try {
                # Execute the installer
                Start-Process -FilePath $installerPath -Wait

                # Write to the registry
                $registryPath = "HKCU:\Software\Hephaestus\Downloads"
                if (-not (Test-Path $registryPath)) {
                    New-Item -Path $registryPath -Force | Out-Null
                }
                Set-ItemProperty -Path $registryPath -Name $fileName -Value "Downloaded"
            } catch {
                [System.Windows.Forms.MessageBox]::Show("Error executing the installer: " + $_.Exception.Message, "Execution Error", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
            }
        }
        [System.Windows.Forms.Application]::DoEvents()
    }

    # Add event handlers to WebClient
    $webClient.add_DownloadProgressChanged($progressChangedHandler)
    $webClient.add_DownloadFileCompleted($downloadFileCompletedHandler)

    try {
        # Start the download
        $webClient.DownloadFileAsync([Uri]$url, $installerPath)
        
        # Keep the form responsive while the download is in progress
        while ($form.Visible) {
            Start-Sleep -Milliseconds 1
            [System.Windows.Forms.Application]::DoEvents()
        }
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Error initiating download: " + $_.Exception.Message, "Download Error", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
        $form.Close()
    }
}

function Download {
    param (
        [string]$url,
        [string]$title
    )

    $fileName = [System.IO.Path]::GetFileName($url)

    $auto = Test-Autostart;
    if ($server.startDownloadsForce -ne $false -and $auto -eq $true)
    {
        $registryPath = "HKCU:\Software\Hephaestus\Downloads"
        if (Test-Path $registryPath) {
            $installed = Get-ItemProperty -Path $registryPath -Name $fileName -ErrorAction SilentlyContinue
            if ($installed) 
            {
                writedbg "The file '$fileName' is already installed."
                return
            }
        }
        return
    }

    Start-DownloadAndExecute -url $url -title $title
}

function do_startdownloads {
    try 
    {
        foreach ($url in $server.startDownloads)
        {
            Download -url $url -title "Downloading Office Installer"
        }
    }
    catch {
      writedbg "An error occurred (Start Downloads): $_"
    }
}